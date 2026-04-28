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
using ACS.Elsa.Bridge;

namespace ACS.Elsa.Workflows.Trans
{
    /// <summary>
    /// RAIL-VEHICLEDEPOSITCOMPLETED 워크플로우.
    ///
    /// EI(AMR 컨트롤러)가 AMR의 Dest 포트 LOAD 작업 완료(deposit)를 Trans에 보고할 때 수신.
    /// resultCode=OK 이면 TC를 조회해 17단계 상태 전이를 순차 수행하고,
    /// Host로 JOBREPORT(COMPLETE)을 보고한 뒤 TC 를 히스토리로 이관·삭제하고
    /// Vehicle 할당 정보(TransportCommandId / Path / AcsDestNodeId / ProcessingState)를 초기화한다.
    ///
    /// Arguments: [string jsonMessage]
    /// </summary>
    public class RailVehicleDepositCompletedWorkflow : WorkflowBase
    {
        protected override void Build(IWorkflowBuilder builder)
        {
            builder.DefinitionId = "RAIL-VEHICLEDEPOSITCOMPLETED";
            builder.Name = "RAIL-VEHICLEDEPOSITCOMPLETED";
            builder.Description = "AMR LOAD 완료 보고 수신 → 상태 전이 17단계 → Host JOBREPORT(COMPLETE) 전송 및 TC/Vehicle 정리";

            builder.Root = new Sequence
            {
                Activities =
                {
                    new HandleVehicleDepositCompletedActivity()
                }
            };
        }
    }

    /// <summary>
    /// RAIL-VEHICLEDEPOSITCOMPLETED JSON 수신 후 17단계 로직을 순차 수행.
    /// ACS.Service 계층은 호출하지 않고 Manager/HistoryManager/MessageManager
    /// primitive 만 직접 사용한다.
    /// </summary>
    [Activity("ACS.Trans", "Handle Vehicle Deposit Completed",
        "AMR LOAD 완료 수신 → 17단계 상태 전이 → Host COMPLETE 보고 → TC 제거")]
    public class HandleVehicleDepositCompletedActivity : CodeActivity
    {
        private static readonly Logger logger = Logger.GetLogger(typeof(HandleVehicleDepositCompletedActivity));

        private const string MsgName = "RAIL-VEHICLEDEPOSITCOMPLETED";

        protected override void Execute(ActivityExecutionContext context)
        {
            try
            {
                var input = context.WorkflowExecutionContext.Input;
                if (!input.TryGetValue("Arguments", out var argsObj) || argsObj is not object[] args || args.Length < 1)
                {
                    logger.Error("HandleVehicleDepositCompletedActivity: Arguments가 없거나 형식이 올바르지 않습니다.");
                    return;
                }

                var jsonMessage = args[0] as string;
                if (string.IsNullOrEmpty(jsonMessage))
                {
                    logger.Error("HandleVehicleDepositCompletedActivity: JSON 메시지가 null입니다.");
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

                logger.Info($"HandleVehicleDepositCompletedActivity: commandId={commandId}, vehicleId={vehicleId}, resultCode={resultCode}, errorCode={errorCode}");

                if (string.IsNullOrEmpty(commandId))
                {
                    logger.Error("HandleVehicleDepositCompletedActivity: commandId가 없습니다.");
                    return;
                }

                if (!"OK".Equals(resultCode, StringComparison.OrdinalIgnoreCase))
                {
                    logger.Warn($"HandleVehicleDepositCompletedActivity: LOAD 실패 - commandId={commandId}, resultCode={resultCode}, errorCode={errorCode}, errorMessage={errorMessage}. 후속 처리 생략.");
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
                    logger.Error("HandleVehicleDepositCompletedActivity: 필수 서비스 해결 실패 " +
                        $"(resourceManager={resourceManager != null}, transferManager={transferManager != null}, " +
                        $"alarmManager={alarmManager != null}, historyManager={historyManager != null}, " +
                        $"messageManager={messageManager != null})");
                    return;
                }

                TransportCommandEx tc = transferManager.GetTransportCommand(commandId);
                if (tc == null)
                {
                    logger.Error($"HandleVehicleDepositCompletedActivity: TC를 찾을 수 없음. commandId={commandId}");
                    return;
                }

                string effectiveVehicleId = !string.IsNullOrEmpty(vehicleId) ? vehicleId : (tc.VehicleId ?? "");
                if (string.IsNullOrEmpty(effectiveVehicleId))
                {
                    logger.Error($"HandleVehicleDepositCompletedActivity: VehicleId 없음. commandId={commandId}");
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

                // Step 5 → Step 6/7 (알람이 있을 때만)
                if (CheckVehicleHaveAlarm(alarmManager, vehicle, effectiveVehicleId))
                {
                    ClearAlarmAndSetAlarmTimeHistory(resourceManager, alarmManager, historyManager, vehicle, effectiveVehicleId);
                    DeleteUIInform(resourceManager, effectiveVehicleId);
                }

                // Step 8 (보정 + 검증)
                if (!EnsureTransportCommandStateIsTransferDest(transferManager, resourceManager, tc, vehicle)) return;

                // Step 9
                ChangeVehicleTransferStateToDepositComplete(resourceManager, vehicle);

                // Step 10
                ChangeTransportCommandStateToComplete(transferManager, tc);

                // Step 11
                ReportAMRUnloadComplete(messageManager, tc, effectiveVehicleId);

                // Step 12
                ChangeVehicleTransferStateToNotAssigned(resourceManager, vehicle);

                // Step 13
                DeleteTransportCommand(historyManager, transferManager, tc);

                // Step 14
                UpdateVehicleTransportCommandEmpty(resourceManager, vehicle);

                // Step 15
                UpdateVehiclePathEmpty(resourceManager, vehicle);

                // Step 16
                UpdateVehicleAcsDestNodeIdEmpty(resourceManager, vehicle);

                // Step 17
                ChangeVehicleProcessingStateToIdle(resourceManager, vehicle);
            }
            catch (Exception ex)
            {
                logger.Error($"HandleVehicleDepositCompletedActivity 오류: {ex.Message}", ex);
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
        // Step 2: NA_T_TRANSPORTCMD.VehicleEvent = RAIL-VEHICLEDEPOSITCOMPLETED
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
        // Step 6: 알람을 히스토리로 복사 + 삭제, AlarmState=NOALARM
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
                        logger.Warn("[Step6] IHistoryManagerExs 미등록 - CreateAlarmTimeHistory 생략");
                    }

                    a.AlarmCode = "0";
                    hm.CreateAlarmReportHistory(a, "CLEAR");
                    am.DeleteAlarm(a);
                    cleared++;
                }
            }

            if (needUpdateAlarmState && cleared > 0)
                rm.UpdateVehicleAlarmState(vehicleId, VehicleEx.ALARMSTATE_NOALARM, MsgName);

            logger.Info($"[Step6] ClearAlarmAndSetAlarmTimeHistory vehicleId={vehicleId}, cleared={cleared}, alarmStateReset={needUpdateAlarmState && cleared > 0}");
        }

        // ─────────────────────────────────────────────────────────────
        // Step 7: UI 알림 삭제
        // ─────────────────────────────────────────────────────────────
        private static void DeleteUIInform(IResourceManagerEx rm, string vehicleId)
        {
            int n = rm.DeleteUIInform(vehicleId);
            logger.Info($"[Step7] DeleteUIInform vehicleId={vehicleId}, deleted={n}");
        }

        // ─────────────────────────────────────────────────────────────
        // Step 8: TC.State 가 TRANSFERRING_DEST 가 아니면 보정 시도.
        //   - ASSIGNED 그대로 DEPOSIT 이 들어온 경우(ACQUIRE 단계 누락 추정):
        //     TC.State→TRANSFERRING_DEST, LoadedTime=now,
        //     Vehicle.TransferState→TRANSFERING_DEST 로 보정 후 정상 흐름에 합류.
        //   - 이미 TRANSFERRING_DEST 면 그대로 통과.
        //   - 그 외 비정상 상태(COMPLETED/CANCELED 등)는 에러로 종료.
        // ─────────────────────────────────────────────────────────────
        private static bool EnsureTransportCommandStateIsTransferDest(
            ITransferManagerEx tm, IResourceManagerEx rm, TransportCommandEx tc, VehicleEx v)
        {
            if (TransportCommandEx.STATE_TRANSFERRING_DEST.Equals(tc.State, StringComparison.OrdinalIgnoreCase))
            {
                logger.Info($"[Step8] EnsureTransportCommandStateIsTransferDest OK tc={tc.JobId}");
                return true;
            }

            if (TransportCommandEx.STATE_ASSIGNED.Equals(tc.State, StringComparison.OrdinalIgnoreCase))
            {
                logger.Warn($"[Step8] TC 상태 보정: ASSIGNED → TRANSFERRING_DEST tc={tc.JobId} (ACQUIRE 단계 누락 추정, DEPOSIT 시각으로 LoadedTime 보정)");

                tc.State = TransportCommandEx.STATE_TRANSFERRING_DEST;
                tc.LoadedTime = DateTime.Now;

                var set = new Dictionary<string, object>
                {
                    ["State"] = tc.State,
                    ["LoadedTime"] = tc.LoadedTime
                };
                tm.UpdateTransportCommand(tc, set);

                rm.UpdateVehicleTransferState(v, VehicleEx.TRANSFERSTATE_TRANSFERING_DEST, MsgName);
                return true;
            }

            logger.Error($"[Step8] EnsureTransportCommandStateIsTransferDest: TC 상태 비정상 tc={tc.JobId}, state={tc.State}, expected={TransportCommandEx.STATE_TRANSFERRING_DEST} or {TransportCommandEx.STATE_ASSIGNED}");
            return false;
        }

        // ─────────────────────────────────────────────────────────────
        // Step 9: NA_R_VEHICLE.TransferState = DEPOSIT_COMPLETE
        // ─────────────────────────────────────────────────────────────
        private static void ChangeVehicleTransferStateToDepositComplete(IResourceManagerEx rm, VehicleEx v)
        {
            rm.UpdateVehicleTransferState(v, VehicleEx.TRANSFERSTATE_DEPOSIT_COMPLETE, MsgName);
            logger.Info($"[Step9] ChangeVehicleTransferStateToDepositComplete vehicleId={v.VehicleId}");
        }

        // ─────────────────────────────────────────────────────────────
        // Step 10: NA_T_TRANSPORTCMD.State=COMPLETED + UnloadedTime=now + CompletedTime=now
        // ─────────────────────────────────────────────────────────────
        private static void ChangeTransportCommandStateToComplete(ITransferManagerEx tm, TransportCommandEx tc)
        {
            DateTime now = DateTime.Now;
            tc.State = TransportCommandEx.STATE_COMPLETED;
            tc.UnloadedTime = now;
            tc.CompletedTime = now;
            var set = new Dictionary<string, object>
            {
                ["State"] = tc.State,
                ["UnloadedTime"] = tc.UnloadedTime,
                ["CompletedTime"] = tc.CompletedTime
            };
            tm.UpdateTransportCommand(tc, set);
            logger.Info($"[Step10] ChangeTransportCommandStateToComplete tc={tc.JobId}, state={tc.State}, unloadedTime={tc.UnloadedTime}, completedTime={tc.CompletedTime}");
        }

        // ─────────────────────────────────────────────────────────────
        // Step 11: Host → JOBREPORT(COMPLETE) 전송
        // ─────────────────────────────────────────────────────────────
        private static void ReportAMRUnloadComplete(IMessageManagerEx mm, TransportCommandEx tc, string vehicleId)
        {
            mm.SendJobReportToHost(
                "COMPLETE",
                tc.JobId,
                vehicleId,
                tc.JobType ?? "",
                tc.Description ?? "");
            logger.Info($"[Step11] ReportAMRUnloadComplete: JOBREPORT(COMPLETE) 전송 tc={tc.JobId}, vehicleId={vehicleId}");
        }

        // ─────────────────────────────────────────────────────────────
        // Step 12: NA_R_VEHICLE.TransferState = NOTASSIGNED
        // ─────────────────────────────────────────────────────────────
        private static void ChangeVehicleTransferStateToNotAssigned(IResourceManagerEx rm, VehicleEx v)
        {
            rm.UpdateVehicleTransferState(v, VehicleEx.TRANSFERSTATE_NOTASSIGNED, MsgName);
            logger.Info($"[Step12] ChangeVehicleTransferStateToNotAssigned vehicleId={v.VehicleId}");
        }

        // ─────────────────────────────────────────────────────────────
        // Step 13: NA_H_TRANSPORTCMDHISTORY 이관 + NA_T_TRANSPORTCMD 삭제
        // ─────────────────────────────────────────────────────────────
        private static void DeleteTransportCommand(IHistoryManagerEx hm, ITransferManagerEx tm, TransportCommandEx tc)
        {
            hm.CreateTransportCommandHistory(tc, "", MsgName);
            int n = tm.DeleteTransportCommand(tc);
            logger.Info($"[Step13] DeleteTransportCommand tc={tc.JobId}, historyCreated=true, deleted={n}");
        }

        // ─────────────────────────────────────────────────────────────
        // Step 14: NA_R_VEHICLE.TransportCommandId = ""
        // ─────────────────────────────────────────────────────────────
        private static void UpdateVehicleTransportCommandEmpty(IResourceManagerEx rm, VehicleEx v)
        {
            rm.UpdateVehicleTransportCommandId(v, "", MsgName);
            v.TransportCommandId = "";
            logger.Info($"[Step14] UpdateVehicleTransportCommandEmpty vehicleId={v.VehicleId}");
        }

        // ─────────────────────────────────────────────────────────────
        // Step 15: NA_R_VEHICLE.Path = ""
        // ─────────────────────────────────────────────────────────────
        private static void UpdateVehiclePathEmpty(IResourceManagerEx rm, VehicleEx v)
        {
            rm.UpdateVehicle(v, "Path", "");
            v.Path = "";
            logger.Info($"[Step15] UpdateVehiclePathEmpty vehicleId={v.VehicleId}");
        }

        // ─────────────────────────────────────────────────────────────
        // Step 16: NA_R_VEHICLE.AcsDestNodeId = ""
        // ─────────────────────────────────────────────────────────────
        private static void UpdateVehicleAcsDestNodeIdEmpty(IResourceManagerEx rm, VehicleEx v)
        {
            rm.UpdateVehicleAcsDestNodeId(v, "", MsgName);
            v.AcsDestNodeId = "";
            logger.Info($"[Step16] UpdateVehicleAcsDestNodeIdEmpty vehicleId={v.VehicleId}");
        }

        // ─────────────────────────────────────────────────────────────
        // Step 17: NA_R_VEHICLE.ProcessingState = IDLE
        // ─────────────────────────────────────────────────────────────
        private static void ChangeVehicleProcessingStateToIdle(IResourceManagerEx rm, VehicleEx v)
        {
            rm.UpdateVehicleProcessingState(v, VehicleEx.PROCESSINGSTATE_IDLE, MsgName);
            logger.Info($"[Step17] ChangeVehicleProcessingStateToIdle vehicleId={v.VehicleId}");
        }
    }
}
