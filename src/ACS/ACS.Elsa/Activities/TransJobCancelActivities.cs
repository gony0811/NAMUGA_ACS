using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.Json;
using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using ACS.Communication.Mqtt.Model;
using ACS.Core.Cache;
using ACS.Core.History;
using ACS.Core.Logging;
using ACS.Core.Message;
using ACS.Core.Path;
using ACS.Core.Path.Model;
using ACS.Core.Resource;
using ACS.Core.Resource.Model;
using ACS.Core.Transfer;
using ACS.Core.Transfer.Model;
using ACS.Elsa.Bridge;

namespace ACS.Elsa.Activities
{
    // ═══════════════════════════════════════════════════════════════
    //  TRANS-JOBCANCEL — 취소 판정(C1~C4)·실행 본체 (사양서 "취소·오류" 시트)
    //
    //  판정은 JobCancelJudge(순수 로직)에 위임하고, 여기서는 실행만 담당한다.
    //  실행 순서 규율: CANCELING 마킹(재수신 방어) → 실행 → 보고.
    //  (C3 만 사양에 따라 승인 보고를 복귀 조치보다 먼저 발행)
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// JOBCANCEL 판정·실행: TC 상태/EXCHANGE STEP/슬롯 적재 기준으로 C1~C4 를 결정하고
    /// 취소를 실행한 뒤 JOBREPORT(CANCEL, ErrorCode) 를 회신한다.
    /// </summary>
    [Activity("ACS.Trans", "Judge And Execute JobCancel",
        "JOBCANCEL C1~C4 판정·실행 → JOBREPORT(CANCEL) 회신")]
    public class JudgeAndExecuteJobCancelActivity : CodeActivity<bool>
    {
        private static readonly Logger logger = Logger.GetLogger("ELSA_ACTIVITY");

        [Input(Description = "JOBCANCEL 메시지 (TRANS-JOBCANCEL JSON)")]
        public Input<ActionCmdMessage> JobCancel { get; set; }

        protected override void Execute(ActivityExecutionContext context)
        {
            try
            {
                var msg = JobCancel?.Get(context);
                string jobId = msg?.Data?.JobId ?? "";
                if (string.IsNullOrEmpty(jobId))
                {
                    logger.Error("JudgeAndExecuteJobCancelActivity: JobID 가 없음");
                    context.Set(Result, false);
                    return;
                }

                var accessor = context.GetService<AutofacContainerAccessor>();
                var tm = accessor?.Resolve<ITransferManagerEx>();
                var rm = accessor?.Resolve<IResourceManagerEx>();
                var mm = accessor?.Resolve<IMessageManagerEx>();
                var hm = accessor?.Resolve<IHistoryManagerEx>();
                var sm = accessor?.ResolveOptional<ISlotManagerEx>();
                var cm = accessor?.Resolve<ICacheManagerEx>();

                if (tm == null || rm == null || mm == null || hm == null)
                {
                    logger.Error("JudgeAndExecuteJobCancelActivity: 필수 서비스 해결 실패");
                    context.Set(Result, false);
                    return;
                }

                TransportCommandEx tc = tm.GetTransportCommand(jobId);
                if (tc == null)
                {
                    // C4: TC 부재 (이미 종결·이관되었거나 미존재)
                    logger.Info($"[JOBCANCEL] C4 거부 — TC 없음 jobId={jobId}");
                    SendCancelReport(mm, jobId, "", "", "",
                        JobCancelJudge.ERR_CANCEL_REJECTED, "TransportCommand not found (already terminated?)");
                    context.Set(Result, true);
                    return;
                }

                bool anySlotOccupied = HasOccupiedSlot(sm, tc.VehicleId);
                int step = ExchangeSteps.GetStep(tc.AdditionalInfo);
                List<TransportCommandEx> tripMates = GetTripMates(tm, tc);
                var verdict = JobCancelJudge.Judge(tc.State, tc.JobType, step, anySlotOccupied,
                    hasActiveTripMate: tripMates.Count > 0);

                string actionType = tc.JobType ?? "";
                string materialType = tc.GetMaterialType() ?? "";
                logger.Info($"[JOBCANCEL] 판정={verdict} — jobId={jobId}, state={tc.State}, jobType={tc.JobType}, " +
                            $"step={step}, slotOccupied={anySlotOccupied}, vehicleId={tc.VehicleId}, tripMates={tripMates.Count}");

                switch (verdict)
                {
                    case JobCancelVerdict.Reject:
                        SendCancelReport(mm, jobId, tc.VehicleId ?? "", actionType, materialType,
                            JobCancelJudge.ERR_CANCEL_REJECTED, $"terminal state: {tc.State}");
                        break;

                    case JobCancelVerdict.CancelBeforeAssign:
                        ExecuteC1(tm, hm, sm, mm, tc, actionType, materialType);
                        break;

                    case JobCancelVerdict.CancelBeforePickup:
                        if (tripMates.Count > 0)
                            ExecuteC2TripMember(tm, rm, hm, sm, mm, tc, actionType, materialType);
                        else
                            ExecuteC2(tm, rm, hm, sm, mm, tc, actionType, materialType);
                        break;

                    case JobCancelVerdict.CancelAfterLoad:
                        ExecuteC3(tm, rm, hm, mm, cm, tc, actionType, materialType);
                        break;

                    case JobCancelVerdict.CancelAfterLoadBatch:
                        ExecuteC5(tm, rm, hm, sm, mm, cm, tc, tripMates, actionType, materialType);
                        break;
                }

                context.Set(Result, true);
            }
            catch (Exception ex)
            {
                logger.Error($"JudgeAndExecuteJobCancelActivity: {ex.Message}", ex);
                context.Set(Result, false);
            }
        }

        // ── C1: 배차 전 — 즉시 취소 (이력 이관) ──────────────────────
        private static void ExecuteC1(ITransferManagerEx tm, IHistoryManagerEx hm, ISlotManagerEx sm,
            IMessageManagerEx mm, TransportCommandEx tc, string actionType, string materialType)
        {
            tc.State = TransportCommandEx.STATE_CANCELED;
            tm.UpdateTransportCommand(tc);

            hm.CreateTransportCommandHistory(tc, "", JobCancelJudge.CAUSE_JOBCANCEL);
            tm.DeleteTransportCommand(tc);
            sm?.ReleaseAllByJobId(tc.JobId);

            logger.Info($"[JOBCANCEL] C1 즉시 취소 완료 — jobId={tc.JobId}");
            SendCancelReport(mm, tc.JobId, "", actionType, materialType, JobCancelJudge.ERR_OK, "");
        }

        // ── C2: 픽업 전 — cancelCmd + 즉시 취소 + 차량 IDLE ──────────
        private static void ExecuteC2(ITransferManagerEx tm, IResourceManagerEx rm, IHistoryManagerEx hm,
            ISlotManagerEx sm, IMessageManagerEx mm, TransportCommandEx tc, string actionType, string materialType)
        {
            const string MsgName = "TRANS-JOBCANCEL";
            string vehicleId = tc.VehicleId;

            // 재수신 방어 마킹 후 AMR 진행 명령 중단
            tc.State = TransportCommandEx.STATE_CANCELING;
            tm.UpdateTransportCommand(tc);
            SendCancelCmd(mm, tc.JobId, vehicleId);

            // TC 종결
            tc.State = TransportCommandEx.STATE_CANCELED;
            tm.UpdateTransportCommand(tc);
            hm.CreateTransportCommandHistory(tc, "", JobCancelJudge.CAUSE_JOBCANCEL);
            tm.DeleteTransportCommand(tc);

            // 차량 초기화 (미적재 — 슬롯 예약분 포함 전체 해제)
            VehicleEx vehicle = string.IsNullOrEmpty(vehicleId) ? null : rm.GetVehicle(vehicleId);
            if (vehicle != null)
            {
                rm.UpdateVehicleTransportCommandId(vehicle, "", MsgName);
                rm.UpdateVehicle(vehicle, "Path", "");
                rm.UpdateVehicleAcsDestNodeId(vehicle, "", MsgName);
                rm.UpdateVehicleTransferState(vehicle, VehicleEx.TRANSFERSTATE_NOTASSIGNED, MsgName);
                rm.UpdateVehicleProcessingState(vehicle, VehicleEx.PROCESSINGSTATE_IDLE, MsgName);
                sm?.ReleaseAllByVehicleId(vehicleId);
            }

            logger.Info($"[JOBCANCEL] C2 취소 완료 — jobId={tc.JobId}, vehicleId={vehicleId} (IDLE 복귀)");
            SendCancelReport(mm, tc.JobId, vehicleId ?? "", actionType, materialType, JobCancelJudge.ERR_OK, "");
        }

        // ── C3: 적재 후 — 승인 보고 → cancelCmd → Job 삭제 → 충전소 복귀 + 차량 ALARM ──
        //    슬롯 점유는 유지한다 (실물 반영 — 작업자 회수 후 차량 reset 이 해소)
        private static void ExecuteC3(ITransferManagerEx tm, IResourceManagerEx rm, IHistoryManagerEx hm,
            IMessageManagerEx mm, ICacheManagerEx cm, TransportCommandEx tc, string actionType, string materialType)
        {
            const string MsgName = "TRANS-JOBCANCEL";
            string vehicleId = tc.VehicleId;
            string bayId = tc.BayId;

            // 재수신 방어 마킹 + 사양 순서: 승인 보고 먼저
            tc.State = TransportCommandEx.STATE_CANCELING;
            tm.UpdateTransportCommand(tc);
            SendCancelReport(mm, tc.JobId, vehicleId ?? "", actionType, materialType, JobCancelJudge.ERR_OK, "");

            // AMR 진행 명령 중단
            SendCancelCmd(mm, tc.JobId, vehicleId);

            // TC 종결
            tc.State = TransportCommandEx.STATE_CANCELED;
            tm.UpdateTransportCommand(tc);
            hm.CreateTransportCommandHistory(tc, "", JobCancelJudge.CAUSE_JOBCANCEL);
            tm.DeleteTransportCommand(tc);

            VehicleEx vehicle = string.IsNullOrEmpty(vehicleId) ? null : rm.GetVehicle(vehicleId);
            if (vehicle == null)
            {
                logger.Warn($"[JOBCANCEL] C3: 차량 조회 실패 vehicleId={vehicleId} — 복귀/알람 생략 jobId={tc.JobId}");
                return;
            }

            // 차량 할당 해제 (슬롯은 실물 반영을 위해 유지)
            rm.UpdateVehicleTransportCommandId(vehicle, "", MsgName);
            vehicle.TransportCommandId = "";
            rm.UpdateVehicle(vehicle, "Path", "");
            rm.UpdateVehicleAcsDestNodeId(vehicle, "", MsgName);
            rm.UpdateVehicleTransferState(vehicle, VehicleEx.TRANSFERSTATE_NOTASSIGNED, MsgName);
            rm.UpdateVehicleProcessingState(vehicle, VehicleEx.PROCESSINGSTATE_IDLE, MsgName);

            // 충전소 복귀 (DispatchChargeJobActivity 패턴)
            DispatchChargeReturn(tm, rm, mm, cm, vehicle, bayId);

            // 차량 ALARM — 작업자 실물 회수 대상 표시
            rm.UpdateVehicleAlarmState(vehicle, VehicleEx.ALARMSTATE_ALARM, MsgName);
            logger.Info($"[JOBCANCEL] C3 취소 완료 — jobId={tc.JobId}, vehicleId={vehicleId}, " +
                        "충전소 복귀 지시 + ALARM (작업자 실물 회수 후 차량 reset 필요, 슬롯 점유 유지)");
        }

        // ── C2 (트립 멤버): X 만 종결, 트립은 잔여 TC 로 계속 ────────────
        //    X 가 현재 진행 leg 였으면 cancelCmd 후 AdvanceTour 재개, 아니면 cancelCmd 생략
        //    (다른 잡의 진행 명령을 죽이지 않음). 잔여 1건이면 단독 잡 의미론으로 강등.
        private static void ExecuteC2TripMember(ITransferManagerEx tm, IResourceManagerEx rm, IHistoryManagerEx hm,
            ISlotManagerEx sm, IMessageManagerEx mm, TransportCommandEx tc, string actionType, string materialType)
        {
            const string MsgName = "TRANS-JOBCANCEL";
            string vehicleId = tc.VehicleId;

            // 현재 진행 leg 판정 — 호출 시점에 완료 이벤트가 없었으므로 NextAfter = 실행 중 명령
            var steps = new List<(string JobId, int Step)>();
            foreach (var item in tm.GetActiveExchangeTransportCommandsByVehicleId(vehicleId))
                if (item is TransportCommandEx t)
                    steps.Add((t.JobId, ExchangeSteps.GetStep(t.AdditionalInfo)));
            var action = ExchangeTour.NextAfter(steps);
            bool isCurrentLeg = string.Equals(action.JobId, tc.JobId, StringComparison.OrdinalIgnoreCase);

            // 재수신 방어 마킹, 현재 leg 였을 때만 AMR 진행 명령 중단
            tc.State = TransportCommandEx.STATE_CANCELING;
            tm.UpdateTransportCommand(tc);
            if (isCurrentLeg)
                SendCancelCmd(mm, tc.JobId, vehicleId);

            // X 만 종결 (픽업 전 — 슬롯은 예약분만 존재)
            tc.State = TransportCommandEx.STATE_CANCELED;
            tm.UpdateTransportCommand(tc);
            hm.CreateTransportCommandHistory(tc, "", JobCancelJudge.CAUSE_JOBCANCEL);
            tm.DeleteTransportCommand(tc);
            sm?.ReleaseAllByJobId(tc.JobId);

            VehicleEx vehicle = string.IsNullOrEmpty(vehicleId) ? null : rm.GetVehicle(vehicleId);
            if (vehicle != null)
            {
                var remain = new List<TransportCommandEx>();
                foreach (var item in tm.GetActiveExchangeTransportCommandsByVehicleId(vehicleId))
                    if (item is TransportCommandEx t)
                        remain.Add(t);

                if (remain.Count == 1)
                {
                    var solo = remain[0];
                    solo.AdditionalInfo = ExchangeInfo.Set(solo.AdditionalInfo, ExchangeInfo.KEY_TRIP, "");
                    tm.UpdateTransportCommand(solo);
                    rm.UpdateVehicleTransportCommandId(vehicle, solo.JobId, MsgName);
                    vehicle.TransportCommandId = solo.JobId;
                    logger.Info($"[JOBCANCEL] C2(트립): 잔여 1건 — 단독 잡으로 강등 solo={solo.JobId}, vehicleId={vehicleId}");
                }

                if (isCurrentLeg)
                    ExchangeTransHandlers.AdvanceTour(vehicle, tm, rm, mm, MsgName);
            }

            logger.Info($"[JOBCANCEL] C2(트립 멤버) 취소 완료 — jobId={tc.JobId}, vehicleId={vehicleId}, " +
                        $"currentLeg={isCurrentLeg} (트립 잔여 계속)");
            SendCancelReport(mm, tc.JobId, vehicleId ?? "", actionType, materialType, JobCancelJudge.ERR_OK, "");
        }

        // ── C5: 배칭 중 1건 적재 후 취소 — X 는 C3 시퀀스, 페어는 연대 종결 ──
        //    페어 보고: EXCHANGE-JOBREPORT(COMPLETE, ErrorCode=EXCHANGE_CANCELED).
        //    슬롯은 실물(OCCUPIED)만 유지, 예약분 해제 — 작업자 회수 후 차량 reset 이 해소.
        private static void ExecuteC5(ITransferManagerEx tm, IResourceManagerEx rm, IHistoryManagerEx hm,
            ISlotManagerEx sm, IMessageManagerEx mm, ICacheManagerEx cm, TransportCommandEx tc,
            List<TransportCommandEx> mates, string actionType, string materialType)
        {
            string vehicleId = tc.VehicleId;

            // 진행 중 명령이 페어 것일 수 있으므로 먼저 중단 (C3 의 충전 복귀 지시보다 앞서야 함)
            foreach (var mate in mates)
                SendCancelCmd(mm, mate.JobId, vehicleId);

            // X: C3 시퀀스 그대로 (승인 보고 → cancelCmd → 종결 → 충전 복귀 + ALARM)
            ExecuteC3(tm, rm, hm, mm, cm, tc, actionType, materialType);

            // 페어 연대 종결
            foreach (var mate in mates)
            {
                try
                {
                    int mateStep = ExchangeSteps.GetStep(mate.AdditionalInfo);
                    string loadSlot = ExchangeInfo.Get(mate.AdditionalInfo, ExchangeInfo.KEY_LOADSLOT) ?? "";
                    mm.SendExchangeJobReportToHost(
                        "COMPLETE", mate.JobId, vehicleId ?? "",
                        mateStep.ToString(), ExchangeSteps.StepName(mateStep), loadSlot,
                        "EXCHANGE", mate.GetMaterialType() ?? "",
                        JobCancelJudge.ERR_EXCHANGE_CANCELED, $"trip mate canceled by JOBCANCEL({tc.JobId})");

                    mate.State = TransportCommandEx.STATE_CANCELED;
                    tm.UpdateTransportCommand(mate);
                    hm.CreateTransportCommandHistory(mate, "", JobCancelJudge.CAUSE_JOBCANCEL);
                    tm.DeleteTransportCommand(mate);
                    ReleaseReservedSlots(sm, vehicleId, mate.JobId);

                    logger.Info($"[JOBCANCEL] C5 페어 연대 종결 — mate={mate.JobId}, step={mateStep} " +
                                "(COMPLETE + EXCHANGE_CANCELED, OCCUPIED 슬롯 유지)");
                }
                catch (Exception ex)
                {
                    logger.Error($"[JOBCANCEL] C5 페어 종결 실패 — mate={mate.JobId}: {ex.Message}", ex);
                }
            }
        }

        // ── 공통 헬퍼 ────────────────────────────────────────────────

        /// <summary>배칭 트립의 다른 활성 EXCHANGE TC 목록 (단독/비EXCHANGE 는 빈 목록).</summary>
        private static List<TransportCommandEx> GetTripMates(ITransferManagerEx tm, TransportCommandEx tc)
        {
            var mates = new List<TransportCommandEx>();
            if (!TransportCommandEx.JOBTYPE_EXCHANGE.Equals(tc.JobType, StringComparison.OrdinalIgnoreCase)
                || string.IsNullOrEmpty(tc.VehicleId))
                return mates;

            foreach (var item in tm.GetActiveExchangeTransportCommandsByVehicleId(tc.VehicleId))
            {
                if (item is TransportCommandEx t
                    && !string.Equals(t.JobId, tc.JobId, StringComparison.OrdinalIgnoreCase))
                    mates.Add(t);
            }
            return mates;
        }

        /// <summary>해당 jobId 의 예약 슬롯만 해제 — OCCUPIED(실물) 는 유지.</summary>
        private static void ReleaseReservedSlots(ISlotManagerEx sm, string vehicleId, string jobId)
        {
            if (sm == null || string.IsNullOrEmpty(vehicleId) || string.IsNullOrEmpty(jobId)) return;
            foreach (var slot in sm.GetSlots(vehicleId))
            {
                if (jobId.Equals(slot.JobId, StringComparison.OrdinalIgnoreCase)
                    && !VehicleSlotEx.STATE_OCCUPIED.Equals(slot.State, StringComparison.OrdinalIgnoreCase))
                    sm.Release(vehicleId, slot.SlotNo);
            }
        }

        private static bool HasOccupiedSlot(ISlotManagerEx sm, string vehicleId)
        {
            if (sm == null || string.IsNullOrEmpty(vehicleId)) return false;
            foreach (var slot in sm.GetSlots(vehicleId))
            {
                if (VehicleSlotEx.STATE_OCCUPIED.Equals(slot.State, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        private static void SendCancelReport(IMessageManagerEx mm, string jobId, string vehicleId,
            string actionType, string materialType, string errCode, string errMsg)
        {
            mm.SendJobReportToHost("CANCEL", jobId, vehicleId, actionType, materialType, errCode, errMsg);
            logger.Info($"[JOBCANCEL] JOBREPORT(CANCEL) 발행 — jobId={jobId}, errCode={errCode}");
        }

        private static void SendCancelCmd(IMessageManagerEx mm, string jobId, string vehicleId)
        {
            if (string.IsNullOrEmpty(vehicleId))
            {
                logger.Warn($"[JOBCANCEL] cancelCmd 생략 — vehicleId 없음 jobId={jobId}");
                return;
            }

            var msg = new RailCancelCmdMessage
            {
                Header = new RailCancelCmdHeader
                {
                    MessageName = "RAIL-CANCELCMD",
                    TransactionId = Guid.NewGuid().ToString(),
                    Timestamp = DateTime.UtcNow,
                    Sender = "Trans"
                },
                Data = new RailCancelCmdData { CommandId = jobId, VehicleId = vehicleId }
            };
            mm.SendActionCmdJson(JsonSerializer.Serialize(msg), vehicleId);
            logger.Info($"[JOBCANCEL] RAIL-CANCELCMD 전송 — jobId={jobId}, vehicleId={vehicleId}");
        }

        /// <summary>
        /// 취소된 차량을 빈 충전 슬롯으로 복귀시킨다 (DispatchChargeJobActivity 의
        /// 슬롯 선정·CHARGEMOVE TC 생성·RAIL-CARRIERTRANSFER 전송 패턴 재사용).
        /// 빈 슬롯이 없으면 복귀를 생략한다 (차량 ALARM 은 호출자가 세팅 — 작업자 조치).
        /// </summary>
        private static void DispatchChargeReturn(ITransferManagerEx tm, IResourceManagerEx rm,
            IMessageManagerEx mm, ICacheManagerEx cm, VehicleEx vehicle, string bayId)
        {
            if (cm == null)
            {
                logger.Warn("[JOBCANCEL] C3: ICacheManagerEx 미해결 — 충전소 복귀 생략");
                return;
            }

            List<LocationViewEx> chargeLocations = cm.GetChargeLocationViewsByBayId(bayId);
            LocationViewEx availableSlot = null;
            if (chargeLocations != null)
            {
                foreach (LocationViewEx loc in chargeLocations)
                {
                    IList occupied = rm.GetVehiclesByCurrentNode(loc.StationId);
                    bool nodeBusy = occupied != null && occupied.Count > 0;
                    bool tcBusy = tm.CheckTransportCommandByDestLocationId(loc.LocationId);
                    if (!nodeBusy && !tcBusy)
                    {
                        availableSlot = loc;
                        break;
                    }
                }
            }
            if (availableSlot == null)
            {
                logger.Warn($"[JOBCANCEL] C3: bayId={bayId} 빈 충전 슬롯 없음 — 복귀 생략 (작업자 조치) vehicleId={vehicle.VehicleId}");
                return;
            }

            string commandId = "C" + vehicle.VehicleId + DateTime.Now.ToString("yyyyMMddHHmmss");
            var chargeTc = new TransportCommandEx
            {
                JobId = commandId,
                CarrierId = commandId,
                State = TransportCommandEx.STATE_CREATED,
                Dest = availableSlot.LocationId,
                VehicleId = vehicle.VehicleId,
                BayId = bayId,
                CreateTime = DateTime.Now,
                AssignedTime = DateTime.Now,
                JobType = TransportCommandEx.JOBTYPE_CHARGEMOVE,
                LoadedTime = null,
                UnloadArrivedTime = null,
                UnloadedTime = null,
                LoadingTime = null,
                UnloadingTime = null,
                CompletedTime = null,
                StartedTime = null
            };

            if (tm.CreateRechargeTransportCommand(chargeTc) == null)
            {
                logger.Error($"[JOBCANCEL] C3: 충전 TC 생성 실패 vehicleId={vehicle.VehicleId}");
                return;
            }
            rm.UpdateVehicleTransportCommandId(vehicle, chargeTc.JobId);
            vehicle.TransportCommandId = chargeTc.JobId;

            string json = CarrierTransferJsonBuilder.Build(chargeTc, vehicle.VehicleId,
                TransportCommandEx.JOBTYPE_CHARGEMOVE, useSource: false, rm, logger);
            if (string.IsNullOrEmpty(json))
            {
                logger.Error($"[JOBCANCEL] C3: CHARGEMOVE JSON 빌드 실패 vehicleId={vehicle.VehicleId}, tc={chargeTc.JobId}");
                return;
            }
            mm.SendCarrierTransferJson(json);
            logger.Info($"[JOBCANCEL] C3: 충전소 복귀 지시 — vehicleId={vehicle.VehicleId}, " +
                        $"chargeLocationId={availableSlot.LocationId}, tc={chargeTc.JobId}");
        }
    }
}
