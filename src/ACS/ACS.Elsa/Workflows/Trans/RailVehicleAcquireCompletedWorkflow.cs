using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.Json;
using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Attributes;
using ACS.Core.Alarm;
using ACS.Core.Alarm.Model;
using ACS.Core.History;
using ACS.Core.History.Model;
using ACS.Core.Logging;
using ACS.Core.Message;
using ACS.Core.Resource;
using ACS.Core.Resource.Model;
using ACS.Core.Transfer;
using ACS.Core.Transfer.Model;
using ACS.Elsa.Activities;
using ACS.Elsa.Bridge;

namespace ACS.Elsa.Workflows.Trans
{
    /// <summary>
    /// RAIL-VEHICLEACQUIRECOMPLETED 워크플로우.
    ///
    /// EI(AMR 컨트롤러)가 AMR의 Source 포트 UNLOAD 작업 완료를 Trans에 보고할 때 수신.
    /// resultCode=OK이면 TC를 조회해 13단계 상태 전이를 순차 수행하고
    /// 마지막으로 EI로 RAIL-CARRIERTRANSFER(jobType=LOAD) 를 전송한다.
    ///
    /// Arguments: [string jsonMessage]
    /// </summary>
    public class RailVehicleAcquireCompletedWorkflow : WorkflowBase
    {
        protected override void Build(IWorkflowBuilder builder)
        {
            builder.DefinitionId = "RAIL-VEHICLEACQUIRECOMPLETED";
            builder.Name = "RAIL-VEHICLEACQUIRECOMPLETED";
            builder.Description = "AMR UNLOAD 완료 보고 수신 → 상태 전이 13단계 → Dest LOAD RAIL-CARRIERTRANSFER 전송";

            builder.Root = new Sequence
            {
                Activities =
                {
                    new HandleVehicleAcquireCompletedActivity()
                }
            };
        }
    }

    /// <summary>
    /// RAIL-VEHICLEACQUIRECOMPLETED JSON 수신 후 13단계 로직을 순차 수행.
    /// ACS.Service 계층은 호출하지 않고 Manager/HistoryManager/MessageManager
    /// primitive 만 직접 사용한다.
    /// </summary>
    [Activity("ACS.Trans", "Handle Vehicle Acquire Completed",
        "AMR UNLOAD 완료 수신 → 13단계 상태 전이 → RAIL-CARRIERTRANSFER(LOAD) 전송")]
    public class HandleVehicleAcquireCompletedActivity : CodeActivity
    {
        private static readonly Logger logger = Logger.GetLogger(typeof(HandleVehicleAcquireCompletedActivity));

        private const string MsgName = "RAIL-VEHICLEACQUIRECOMPLETED";

        protected override void Execute(ActivityExecutionContext context)
        {
            try
            {
                var input = context.WorkflowExecutionContext.Input;
                if (!input.TryGetValue("Arguments", out var argsObj) || argsObj is not object[] args || args.Length < 1)
                {
                    logger.Error("HandleVehicleAcquireCompletedActivity: Arguments가 없거나 형식이 올바르지 않습니다.");
                    return;
                }

                var jsonMessage = args[0] as string;
                if (string.IsNullOrEmpty(jsonMessage))
                {
                    logger.Error("HandleVehicleAcquireCompletedActivity: JSON 메시지가 null입니다.");
                    return;
                }

                string commandId = null;
                string vehicleId = null;
                string resultCode = null;
                string errorCode = null;
                string errorMessage = null;

                using (var doc = JsonDocument.Parse(jsonMessage))
                {
                    if (doc.RootElement.TryGetProperty("data", out var dataEl))
                    {
                        if (dataEl.TryGetProperty("commandId", out var cid))
                            commandId = cid.GetString();
                        if (dataEl.TryGetProperty("vehicleId", out var vid))
                            vehicleId = vid.GetString();
                        if (dataEl.TryGetProperty("resultCode", out var rc))
                            resultCode = rc.GetString();
                        if (dataEl.TryGetProperty("errorCode", out var ec))
                            errorCode = ec.ValueKind == JsonValueKind.String ? ec.GetString() : ec.ToString();
                        if (dataEl.TryGetProperty("errorMessage", out var em))
                            errorMessage = em.GetString();
                    }
                }

                logger.Info($"HandleVehicleAcquireCompletedActivity: commandId={commandId}, vehicleId={vehicleId}, resultCode={resultCode}, errorCode={errorCode}");

                if (string.IsNullOrEmpty(commandId))
                {
                    logger.Error("HandleVehicleAcquireCompletedActivity: commandId가 없습니다.");
                    return;
                }

                if (!"OK".Equals(resultCode, StringComparison.OrdinalIgnoreCase))
                {
                    logger.Warn($"HandleVehicleAcquireCompletedActivity: UNLOAD 실패 - commandId={commandId}, resultCode={resultCode}, errorCode={errorCode}, errorMessage={errorMessage}. 후속 처리 생략.");
                    return;
                }

                var accessor = context.GetService<AutofacContainerAccessor>();
                var resourceManager = accessor?.Resolve<IResourceManagerEx>();
                var transferManager = accessor?.Resolve<ITransferManagerEx>();
                var alarmManager = accessor?.Resolve<IAlarmManagerEx>();
                var historyManager = accessor?.Resolve<IHistoryManagerEx>();
                var messageManager = accessor?.Resolve<IMessageManagerEx>();

                if (resourceManager == null || transferManager == null || alarmManager == null
                    || historyManager == null || messageManager == null)
                {
                    logger.Error("HandleVehicleAcquireCompletedActivity: 필수 서비스 해결 실패 " +
                        $"(resourceManager={resourceManager != null}, transferManager={transferManager != null}, " +
                        $"alarmManager={alarmManager != null}, historyManager={historyManager != null}, " +
                        $"messageManager={messageManager != null})");
                    return;
                }

                TransportCommandEx tc = transferManager.GetTransportCommand(commandId);
                if (tc == null)
                {
                    logger.Error($"HandleVehicleAcquireCompletedActivity: TC를 찾을 수 없음. commandId={commandId}");
                    return;
                }

                string effectiveVehicleId = !string.IsNullOrEmpty(vehicleId) ? vehicleId : (tc.VehicleId ?? "");
                if (string.IsNullOrEmpty(effectiveVehicleId))
                {
                    logger.Error($"HandleVehicleAcquireCompletedActivity: VehicleId 없음. commandId={commandId}");
                    return;
                }

                // Step 1
                VehicleEx vehicle = CheckVehicle(resourceManager, effectiveVehicleId);
                if (vehicle == null) return;

                // Step 2
                UpdateTransportCommandVehicleEvent(transferManager, tc);

                // Step 3
                ChangeVehicleConnectStateToConnect(resourceManager, vehicle);

                // Step 4
                UpdateVehicleEventTime(resourceManager, vehicle);

                // Step 5 → Step 7/8 (알람이 있을 때만)
                // Step 6(Host 알람 클리어 보고)는 현 운영상 불필요하여 제거됨.
                if (CheckVehicleHaveAlarm(alarmManager, vehicle, effectiveVehicleId))
                {
                    ClearAlarmAndSetAlarmTimeHistory(resourceManager, alarmManager, historyManager, vehicle, effectiveVehicleId);
                    DeleteUIInform(resourceManager, effectiveVehicleId);
                }

                // Step 9
                UpdateVehicleAcsDestNodeIdEmpty(resourceManager, vehicle);

                // Step 10
                ChangeVehicleTransferStateToAcquireComplete(resourceManager, vehicle);

                // Step 11
                ChangeTransportCommandStateToTransferingDest(transferManager, tc);

                // Step 11.5
                UpdateVehicleStateTransferingDest(resourceManager, vehicle);

                // Step 12
                UpdateVehicleAcsDestNodeToDest(resourceManager, vehicle, tc);

                // Step 13
                SendTransportCommandDest(messageManager, resourceManager, tc, effectiveVehicleId);
            }
            catch (Exception ex)
            {
                logger.Error($"HandleVehicleAcquireCompletedActivity 오류: {ex.Message}", ex);
            }
        }

        // ─────────────────────────────────────────────────────────────
        // Step 1: NA_R_VEHICLE 존재 확인
        // ─────────────────────────────────────────────────────────────
        private static VehicleEx CheckVehicle(IResourceManagerEx rm, string vehicleId)
        {
            VehicleEx v = rm.GetVehicle(vehicleId);
            if (v == null)
                logger.Error($"[Step1] CheckVehicle: Vehicle 없음 vehicleId={vehicleId}");
            else
                logger.Info($"[Step1] CheckVehicle OK vehicleId={vehicleId}");
            return v;
        }

        // ─────────────────────────────────────────────────────────────
        // Step 2: NA_T_TRANSPORTCMD.VehicleEvent = RAIL-VEHICLEACQUIRECOMPLETED
        // ─────────────────────────────────────────────────────────────
        private static void UpdateTransportCommandVehicleEvent(ITransferManagerEx tm, TransportCommandEx tc)
        {
            tc.VehicleEvent = MsgName;
            var set = new Dictionary<string, object> { ["VehicleEvent"] = MsgName };
            tm.UpdateTransportCommand(tc, set);
            logger.Info($"[Step2] UpdateTransportCommandVehicleEvent tc={tc.JobId}, vehicleEvent={MsgName}");
        }

        // ─────────────────────────────────────────────────────────────
        // Step 3: Vehicle.ConnectionState = CONNECT
        // ─────────────────────────────────────────────────────────────
        private static void ChangeVehicleConnectStateToConnect(IResourceManagerEx rm, VehicleEx v)
        {
            rm.UpdateVehicleConnectionState(v, VehicleEx.CONNECTIONSTATE_CONNECT, MsgName);
            logger.Info($"[Step3] ChangeVehicleConnectStateToConnect vehicleId={v.VehicleId}");
        }

        // ─────────────────────────────────────────────────────────────
        // Step 4: Vehicle.EventTime = now
        // ─────────────────────────────────────────────────────────────
        private static void UpdateVehicleEventTime(IResourceManagerEx rm, VehicleEx v)
        {
            rm.UpdateVehicleEventTime(v);
            logger.Info($"[Step4] UpdateVehicleEventTime vehicleId={v.VehicleId}");
        }

        // ─────────────────────────────────────────────────────────────
        // Step 5: Vehicle 알람 존재 여부
        // ─────────────────────────────────────────────────────────────
        private static bool CheckVehicleHaveAlarm(IAlarmManagerEx am, VehicleEx v, string vehicleId)
        {
            AlarmEx a = am.GetAlarmByVehicleId(vehicleId);
            bool hasAlarm = a != null
                || (!string.IsNullOrEmpty(v.AlarmState)
                    && VehicleEx.ALARMSTATE_ALARM.Equals(v.AlarmState, StringComparison.OrdinalIgnoreCase));
            logger.Info($"[Step5] CheckVehicleHaveAlarm vehicleId={vehicleId}, hasAlarm={hasAlarm}");
            return hasAlarm;
        }

        // ─────────────────────────────────────────────────────────────
        // Step 7: 알람을 히스토리로 복사 + 삭제, AlarmState=NOALARM
        // ─────────────────────────────────────────────────────────────
        private static void ClearAlarmAndSetAlarmTimeHistory(IResourceManagerEx rm, IAlarmManagerEx am,
            IHistoryManagerEx hm, VehicleEx v, string vehicleId)
        {
            IList alarms = am.GetAlarmsByVehicleId(vehicleId);
            bool needUpdateAlarmState = true;
            int cleared = 0;

            // IHistoryManagerExs 가 등록되지 않았을 수 있어 cast-or-skip 패턴 사용
            IHistoryManagerExs hmExs = hm as IHistoryManagerExs;

            if (alarms != null)
            {
                foreach (var obj in alarms)
                {
                    if (obj is not AlarmEx a) continue;

                    if (AlarmExs.ALARMCODE_LOWBATERRY.Equals(a.AlarmId))
                    {
                        needUpdateAlarmState = false;
                        continue;
                    }

                    if (hmExs != null)
                    {
                        var h = new AlarmTimeHistoryEx
                        {
                            AlarmId = a.AlarmId,
                            AlarmCode = a.AlarmCode,
                            AlarmText = a.AlarmText,
                            CreateTime = a.CreateTime,
                            ClearTime = DateTime.Now,
                            Time = DateTime.Now,
                            TransportCommandId = a.TransportCommandId,
                            VehicleId = a.VehicleId,
                            NearAgv = a.NearAgv,
                            IsCross = a.IsCross,
                            BayId = v.BayId
                        };
                        hmExs.CreateAlarmTimeHistory(h);
                    }
                    else
                    {
                        logger.Warn("[Step7] IHistoryManagerExs 미등록 - CreateAlarmTimeHistory 생략");
                    }

                    a.AlarmCode = "0";
                    hm.CreateAlarmReportHistory(a, "CLEAR");
                    am.DeleteAlarm(a);
                    cleared++;
                }
            }

            if (needUpdateAlarmState && cleared > 0)
                rm.UpdateVehicleAlarmState(vehicleId, VehicleEx.ALARMSTATE_NOALARM, MsgName);

            logger.Info($"[Step7] ClearAlarmAndSetAlarmTimeHistory vehicleId={vehicleId}, cleared={cleared}, alarmStateReset={needUpdateAlarmState && cleared > 0}");
        }

        // ─────────────────────────────────────────────────────────────
        // Step 8: UI 알림 삭제
        // ─────────────────────────────────────────────────────────────
        private static void DeleteUIInform(IResourceManagerEx rm, string vehicleId)
        {
            int n = rm.DeleteUIInform(vehicleId);
            logger.Info($"[Step8] DeleteUIInform vehicleId={vehicleId}, deleted={n}");
        }

        // ─────────────────────────────────────────────────────────────
        // Step 9: NA_R_VEHICLE.AcsDestNodeId = ""
        // ─────────────────────────────────────────────────────────────
        private static void UpdateVehicleAcsDestNodeIdEmpty(IResourceManagerEx rm, VehicleEx v)
        {
            rm.UpdateVehicleAcsDestNodeId(v, "", MsgName);
            v.AcsDestNodeId = "";
            logger.Info($"[Step9] UpdateVehicleAcsDestNodeIdEmpty vehicleId={v.VehicleId}");
        }

        // ─────────────────────────────────────────────────────────────
        // Step 10: NA_R_VEHICLE.TransferState = ACQUIRE_COMPLETE
        // ─────────────────────────────────────────────────────────────
        private static void ChangeVehicleTransferStateToAcquireComplete(IResourceManagerEx rm, VehicleEx v)
        {
            rm.UpdateVehicleTransferState(v, VehicleEx.TRANSFERSTATE_ACQUIRE_COMPLETE, MsgName);
            logger.Info($"[Step10] ChangeVehicleTransferStateToAcquireComplete vehicleId={v.VehicleId}");
        }

        // ─────────────────────────────────────────────────────────────
        // Step 11: NA_T_TRANSPORTCMD.State=TRANSFERRING_DEST + LoadedTime=now
        // ─────────────────────────────────────────────────────────────
        private static void ChangeTransportCommandStateToTransferingDest(ITransferManagerEx tm, TransportCommandEx tc)
        {
            tc.State = TransportCommandEx.STATE_TRANSFERRING_DEST;
            tc.LoadedTime = DateTime.Now;
            var set = new Dictionary<string, object>
            {
                ["State"] = tc.State,
                ["LoadedTime"] = tc.LoadedTime
            };
            tm.UpdateTransportCommand(tc, set);
            logger.Info($"[Step11] ChangeTransportCommandStateToTransferingDest tc={tc.JobId}, state={tc.State}, loadedTime={tc.LoadedTime}");
        }

        // ─────────────────────────────────────────────────────────────
        // Step 11.5: NA_R_VEHICLE.TransferState = TRANSFERING_DEST
        // ─────────────────────────────────────────────────────────────
        private static void UpdateVehicleStateTransferingDest(IResourceManagerEx rm, VehicleEx v)
        {
            rm.UpdateVehicleTransferState(v, VehicleEx.TRANSFERSTATE_TRANSFERING_DEST, MsgName);
            logger.Info($"[Step11.5] UpdateVehicleStateTransferingDest vehicleId={v.VehicleId}");
        }

        // ─────────────────────────────────────────────────────────────
        // Step 12: tc.Dest → Location.StationId → Vehicle.AcsDestNodeId
        // ─────────────────────────────────────────────────────────────
        private static void UpdateVehicleAcsDestNodeToDest(IResourceManagerEx rm, VehicleEx v, TransportCommandEx tc)
        {
            if (string.IsNullOrEmpty(tc.Dest))
            {
                logger.Warn($"[Step12] UpdateVehicleAcsDestNodeToDest: tc.Dest 가 비어있음 tc={tc.JobId}");
                return;
            }

            LocationEx loc = rm.GetLocationByLocationId(tc.Dest);
            if (loc == null || string.IsNullOrEmpty(loc.StationId))
            {
                logger.Warn($"[Step12] UpdateVehicleAcsDestNodeToDest: Location/StationId 조회 실패 dest={tc.Dest}");
                return;
            }

            rm.UpdateVehicleAcsDestNodeId(v, loc.StationId, MsgName);
            v.AcsDestNodeId = loc.StationId;
            logger.Info($"[Step12] UpdateVehicleAcsDestNodeToDest vehicleId={v.VehicleId}, dest={tc.Dest}, destNodeId={loc.StationId}");
        }

        // ─────────────────────────────────────────────────────────────
        // Step 13: Trans → EI RAIL-CARRIERTRANSFER(jobType=LOAD) 전송
        // ─────────────────────────────────────────────────────────────
        private static void SendTransportCommandDest(IMessageManagerEx mm, IResourceManagerEx rm,
            TransportCommandEx tc, string vehicleId)
        {
            string json = CarrierTransferJsonBuilder.Build(
                tc, vehicleId,
                TransportCommandEx.JOBTYPE_LOAD,
                useSource: false,
                rm, logger);

            if (string.IsNullOrEmpty(json))
            {
                logger.Error($"[Step13] SendTransportCommandDest: JSON 빌드 실패 tc={tc.JobId}");
                return;
            }

            mm.SendCarrierTransferJson(json);
            logger.Info($"[Step13] SendTransportCommandDest: RAIL-CARRIERTRANSFER(LOAD) 전송 tc={tc.JobId}, vehicleId={vehicleId}");
        }
    }
}
