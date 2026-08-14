using System;
using System.Text.Json;
using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Attributes;
using ACS.Communication.Mqtt.Model;
using ACS.Core.History;
using ACS.Core.Logging;
using ACS.Core.Message;
using ACS.Core.Resource;
using ACS.Core.Resource.Model;
using ACS.Core.Transfer;
using ACS.Core.Transfer.Model;

namespace ACS.Elsa.Workflows.Trans
{
    /// <summary>
    /// RAIL-VEHICLEABNORMAL 워크플로우.
    ///
    /// EI 프로세스에서 AMR status 의 abnormal 블록을 감지하면 type 무관하게 메시지를 보낸다.
    /// Trans 프로세스의 ESListener 가 수신하여 이 워크플로우를 실행하고, type 별 분기로 처리한다.
    ///
    /// 처리 대상:
    ///   - OPERATOR_ABORT (code=200): 운영자 강제 중단.
    ///     진행 중인 TC 에 대해 JOBREPORT(COMPLETE) 를 HS 로 보고하여 MES 로 전파한 뒤,
    ///     TC 를 히스토리 이관·NA_T_TRANSPORTCMD 에서 삭제하고,
    ///     NA_R_VEHICLE 의 TransportCommandId/Path/AcsDestNodeId 초기화 +
    ///     TransferState=NOTASSIGNED, ProcessingState=IDLE 로 전이한다.
    ///   - 그 외 type: 현재는 Warn 로그만. 향후 분기 추가 위치.
    ///
    /// 멱등성: vehicle.TransportCommandId 가 비어 있으면 정리 대상이 없는 상태로 보고
    /// 로그/DB 변경 없이 조기 반환한다. AMR 이 abnormal 을 반복 송신해도 노이즈/중복 변경이 없도록 함.
    /// </summary>
    public class RailVehicleAbnormalWorkflow : WorkflowBase
    {
        protected override void Build(IWorkflowBuilder builder)
        {
            builder.DefinitionId = "RAIL-VEHICLEABNORMAL";
            builder.Name = "RAIL-VEHICLEABNORMAL";
            builder.Description = "AMR Abnormal 메시지 수신 시 type 별 분기 처리 (OPERATOR_ABORT → TC 삭제 + Vehicle 초기화)";

            builder.Root = new Sequence
            {
                Activities =
                {
                    new RailVehicleAbnormalActivity(),
                }
            };
        }
    }

    /// <summary>
    /// RAIL-VEHICLEABNORMAL 처리 Activity.
    /// JSON 을 역직렬화한 뒤 type 별로 분기. OPERATOR_ABORT 만 도메인 상태를 갱신하며,
    /// 그 외는 Warn 로그로 남기고 종료.
    /// </summary>
    [Activity("ACS.Trans", "Rail Vehicle Abnormal",
        "AMR Abnormal JSON 으로 type 별 분기 처리 (OPERATOR_ABORT 처리 포함)")]
    public class RailVehicleAbnormalActivity : CodeActivity
    {
        private const string MsgName = "RAIL-VEHICLEABNORMAL";
        private static readonly Logger logger = Logger.GetLogger(typeof(RailVehicleAbnormalActivity));

        protected override void Execute(ActivityExecutionContext context)
        {
            try
            {
                var input = context.WorkflowExecutionContext.Input;
                if (!input.TryGetValue("Arguments", out var argsObj) || argsObj is not object[] args || args.Length < 1)
                {
                    logger.Error("RailVehicleAbnormalActivity: Arguments가 없거나 형식이 올바르지 않습니다.");
                    return;
                }

                var json = args[0] as string;
                if (string.IsNullOrEmpty(json))
                {
                    logger.Error("RailVehicleAbnormalActivity: JSON 메시지가 null입니다.");
                    return;
                }

                var abnormalMessage = JsonSerializer.Deserialize<RailVehicleAbnormalMessage>(json);
                if (abnormalMessage?.Data == null)
                {
                    logger.Error("RailVehicleAbnormalActivity: JSON 역직렬화 실패.");
                    return;
                }

                var data = abnormalMessage.Data;

                var accessor = context.GetService<Bridge.AutofacContainerAccessor>();
                if (accessor == null)
                {
                    logger.Error("RailVehicleAbnormalActivity: AutofacContainerAccessor를 찾을 수 없습니다.");
                    return;
                }

                var resourceManager = accessor.Resolve<IResourceManagerEx>();
                if (resourceManager == null)
                {
                    logger.Error("RailVehicleAbnormalActivity: IResourceManagerEx를 찾을 수 없습니다.");
                    return;
                }

                VehicleEx vehicle = resourceManager.GetVehicle(data.VehicleId);
                if (vehicle == null)
                {
                    logger.Warn($"RailVehicleAbnormalActivity: Vehicle을 찾을 수 없습니다. vehicleId={data.VehicleId}");
                    return;
                }

                // type 또는 code 어느 쪽이라도 OPERATOR_ABORT 매칭이면 abort 처리.
                bool isOperatorAbort = string.Equals(data.Type, RailVehicleAbnormalData.TYPE_OPERATOR_ABORT, StringComparison.OrdinalIgnoreCase)
                                    || string.Equals(data.Code, RailVehicleAbnormalData.CODE_OPERATOR_ABORT, StringComparison.OrdinalIgnoreCase);

                if (!isOperatorAbort)
                {
                    logger.Warn($"RailVehicleAbnormalActivity: 미처리 type=\"{data.Type}\", code=\"{data.Code}\", vehicleId={data.VehicleId}, node={data.Node}");
                    return;
                }

                HandleOperatorAbort(accessor, resourceManager, vehicle, data);
            }
            catch (Exception e)
            {
                logger.Error("RailVehicleAbnormalActivity 오류", e);
            }
        }

        // OPERATOR_ABORT 처리:
        //   1) 멱등성: vehicle.TransportCommandId 가 공백이면 silent skip.
        //   2) TC 조회
        //   3) JOBREPORT(COMPLETE) → HS → MES (TC 데이터가 유효한 동안 먼저 송신)
        //   4) TC 히스토리 이관 → NA_T_TRANSPORTCMD 삭제
        //   5) Vehicle.TransportCommandId/Path/AcsDestNodeId = ""
        //   6) Vehicle.TransferState = NOTASSIGNED, ProcessingState = IDLE
        private static void HandleOperatorAbort(Bridge.AutofacContainerAccessor accessor,
            IResourceManagerEx resourceManager, VehicleEx vehicle, RailVehicleAbnormalData data)
        {
            // 1) 멱등성 가드
            if (string.IsNullOrEmpty(vehicle.TransportCommandId))
            {
                return;
            }

            logger.Info($"RailVehicleAbnormalActivity: OPERATOR_ABORT 수신 vehicleId={vehicle.VehicleId}, " +
                        $"transportCommandId={vehicle.TransportCommandId}, node={data.Node}");

            var transferManager = accessor.Resolve<ITransferManagerEx>();
            var historyManager = accessor.Resolve<IHistoryManagerEx>();
            var messageManager = accessor.Resolve<IMessageManagerEx>();

            if (transferManager == null)
            {
                logger.Error($"RailVehicleAbnormalActivity: ITransferManagerEx 미등록 - TC 정리 불가 vehicleId={vehicle.VehicleId}");
                return;
            }

            // 2) TC 조회
            TransportCommandEx tc = transferManager.GetTransportCommand(vehicle.TransportCommandId);
            if (tc != null)
            {
                // 3) JOBREPORT(COMPLETE) → HS → MES 송신.
                // errCode/errMsg 에 OPERATOR_ABORT 정보를 실어 MES 가 정상 종료 vs abort-driven COMPLETE 를 구분 가능.
                // 삭제 이후엔 tc.JobType / tc.Description 이 무효해질 수 있으므로 history/delete 보다 반드시 먼저.
                if (messageManager != null)
                {
                    messageManager.SendJobReportToHost(
                        reportType: "COMPLETE",
                        jobId: tc.JobId,
                        amrId: vehicle.VehicleId,
                        actionType: tc.JobType ?? "",
                        materialType: tc.Description ?? "",
                        errCode: RailVehicleAbnormalData.CODE_OPERATOR_ABORT,
                        errMsg: RailVehicleAbnormalData.TYPE_OPERATOR_ABORT);
                    logger.Info($"RailVehicleAbnormalActivity: JOBREPORT(COMPLETE, ErrorCode={RailVehicleAbnormalData.CODE_OPERATOR_ABORT}, ErrorMsg={RailVehicleAbnormalData.TYPE_OPERATOR_ABORT}) 전송 tc={tc.JobId}, vehicleId={vehicle.VehicleId}");
                }
                else
                {
                    logger.Warn($"RailVehicleAbnormalActivity: IMessageManagerEx 미등록 - JOBREPORT 송신 생략 tc={tc.JobId}");
                }

                // 4) TC 히스토리 이관 + 삭제
                if (historyManager != null)
                {
                    historyManager.CreateTransportCommandHistory(tc, "", MsgName);
                }
                else
                {
                    logger.Warn($"RailVehicleAbnormalActivity: IHistoryManagerEx 미등록 - 히스토리 생성 생략 tc={tc.JobId}");
                }
                int deleted = transferManager.DeleteTransportCommand(tc);
                logger.Info($"RailVehicleAbnormalActivity: TC 삭제 vehicleId={vehicle.VehicleId}, tcId={tc.JobId}, deleted={deleted}");
            }
            else
            {
                logger.Warn($"RailVehicleAbnormalActivity: Vehicle.TransportCommandId={vehicle.TransportCommandId} 에 해당하는 TC 가 없음 vehicleId={vehicle.VehicleId} - JOBREPORT/히스토리/삭제 생략");
            }

            // 3) Vehicle 할당 정보 초기화
            resourceManager.UpdateVehicleTransportCommandId(vehicle, "", MsgName);
            vehicle.TransportCommandId = "";

            resourceManager.UpdateVehicle(vehicle, "Path", "");
            vehicle.Path = "";

            resourceManager.UpdateVehicleAcsDestNodeId(vehicle, "", MsgName);
            vehicle.AcsDestNodeId = "";

            // 4) 상태 전이 + 슬롯 동반 초기화 (EXCHANGE 잔류 점유 방지)
            resourceManager.UpdateVehicleTransferState(vehicle, VehicleEx.TRANSFERSTATE_NOTASSIGNED, MsgName);
            resourceManager.UpdateVehicleProcessingState(vehicle, VehicleEx.PROCESSINGSTATE_IDLE, MsgName);
            accessor.ResolveOptional<ACS.Core.Resource.ISlotManagerEx>()?.ReleaseAllByVehicleId(vehicle.VehicleId);

            logger.Info($"RailVehicleAbnormalActivity: OPERATOR_ABORT 처리 완료 vehicleId={vehicle.VehicleId}, " +
                        $"TransferState={VehicleEx.TRANSFERSTATE_NOTASSIGNED}, ProcessingState={VehicleEx.PROCESSINGSTATE_IDLE}");
        }
    }
}
