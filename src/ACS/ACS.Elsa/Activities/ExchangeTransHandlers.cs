using System;
using ACS.Core.History;
using ACS.Core.Logging;
using ACS.Core.Message;
using ACS.Core.Resource;
using ACS.Core.Resource.Model;
using ACS.Core.Transfer;
using ACS.Core.Transfer.Model;

namespace ACS.Elsa.Activities
{
    // ═══════════════════════════════════════════════════════════════
    //  EXCHANGE(v2) S5 — Trans 측 도착/픽업/반납 EXCHANGE 분기 본체.
    //
    //  기존 공용 워크플로우(RailVehicleDestArrived / AcquireCompleted /
    //  DepositCompleted)에는 JobType==EXCHANGE 3줄 분기만 두고(D4),
    //  실제 전이 로직은 전부 이 클래스에 격리한다.
    //
    //  여정/STEP 전이 (상태는 EXCHANGE_ASSIGNED 유지, ExchangeSteps 참조):
    //   10 PICKUP_NEW  : origin 픽업(UNLOAD) 완료 시 STEP=20,
    //                    loadSlot Occupy(NEW), mid행 CARRIERTRANSFER(EXCHANGE)
    //   20 MOVE_TO_EQUIP: mid 도착 시 ARRIVED(20) 보고 (사양상 유일한 ARRIVED).
    //                    이후 MES ACTIONCMD(UNLOAD→LOAD) 2건 시퀀스를
    //                    RailVehicleExchangeCompletedWorkflow 가 ACT 기반으로
    //                    처리 (20→30→50, STEP_COMPLETE 30·40 보고)
    //   50 RETURN_OLD  : dest LOAD(반납) 완료 시 STEP=60,
    //                    STEP_COMPLETE(50) + COMPLETE(60,DONE) 보고 + TC 종결 + 슬롯/차량 정리
    // ═══════════════════════════════════════════════════════════════
    internal static class ExchangeTransHandlers
    {
        private static readonly Logger logger = Logger.GetLogger(typeof(ExchangeTransHandlers));

        /// <summary>
        /// RAIL-VEHICLEDESTARRIVED 의 EXCHANGE 분기.
        /// 현재 STEP 이 기대하는 waypoint 도착을 판별하되, 보고는 사양이 정의한
        /// 설비 도착(ARRIVED, 20) 한 건만 발행한다. TC/Vehicle 전이는 없음.
        /// </summary>
        public static void OnDestArrived(TransportCommandEx tc, VehicleEx vehicle,
            IResourceManagerEx rm, IMessageManagerEx mm)
        {
            int step = ExchangeSteps.GetStep(tc.AdditionalInfo);

            string sourceStationId = ResolveStationId(rm, tc.Source);
            string midStationId = ResolveStationId(rm,
                ExchangeSteps.BuildMidLocationId(tc.MidLoc, tc.MidPortId));
            string destStationId = ResolveStationId(rm, tc.Dest);

            int? arrivedStep = ExchangeSteps.ResolveArrivedStep(
                step, vehicle.CurrentNodeId, sourceStationId, midStationId, destStationId);
            if (arrivedStep == null)
            {
                logger.Info($"[EXCHANGE] ARRIVED skip — step={step}, currentNode={vehicle.CurrentNodeId}, " +
                            $"src={sourceStationId}, mid={midStationId}, dest={destStationId}, tc={tc.JobId}");
                return;
            }

            // 사양서 Scenario 상 ARRIVED 보고는 설비 도착(Step=20) 한 건만 정의.
            // origin/dest 도착(10/50)은 MES 미인지 보고라 발행하지 않는다 (도착 판별·로그만 유지).
            if (arrivedStep.Value != ExchangeSteps.STEP_MOVE_TO_EQUIP)
            {
                logger.Info($"[EXCHANGE] 도착 감지 (보고 생략 — 사양 외 step): tc={tc.JobId}, vehicleId={vehicle.VehicleId}, " +
                            $"step={arrivedStep.Value}({ExchangeSteps.StepName(arrivedStep.Value)}), node={vehicle.CurrentNodeId}");
                return;
            }

            SendReport(mm, tc, vehicle.VehicleId, "ARRIVED", arrivedStep.Value);
            logger.Info($"[EXCHANGE] ARRIVED 보고: tc={tc.JobId}, vehicleId={vehicle.VehicleId}, " +
                        $"step={arrivedStep.Value}({ExchangeSteps.StepName(arrivedStep.Value)}), node={vehicle.CurrentNodeId}");
        }

        /// <summary>
        /// RAIL-VEHICLEACQUIRECOMPLETED 의 EXCHANGE 분기 (origin 신자재 픽업 완료).
        /// 공용 Step1~10(차량 확인/알람 클리어/AcsDest 클리어/ACQUIRE_COMPLETE) 이후에 호출된다.
        /// 조기 dest행(기존 Step11~13)을 대체: STEP=20, loadSlot 적재, mid행 EXCHANGE 명령 전송.
        /// </summary>
        public static void OnAcquireCompleted(TransportCommandEx tc, VehicleEx vehicle,
            ITransferManagerEx tm, IResourceManagerEx rm, ISlotManagerEx sm, IMessageManagerEx mm)
        {
            const string MsgName = "RAIL-VEHICLEACQUIRECOMPLETED";

            if (!GuardExchangeAssigned(tc, "OnAcquireCompleted")) return;

            int step = ExchangeSteps.GetStep(tc.AdditionalInfo);
            if (step != ExchangeSteps.STEP_PICKUP_NEW)
            {
                logger.Warn($"[EXCHANGE] OnAcquireCompleted: 예상외 STEP={step} (기대 10) — 진행 생략 tc={tc.JobId}");
                return;
            }

            string loadSlot = ExchangeInfo.Get(tc.AdditionalInfo, ExchangeInfo.KEY_LOADSLOT);

            // ① DB 전이: LoadedTime + STEP=20 (상태는 EXCHANGE_ASSIGNED 유지)
            tc.LoadedTime = DateTime.Now;
            tc.AdditionalInfo = ExchangeInfo.Set(tc.AdditionalInfo,
                ExchangeInfo.KEY_STEP, ExchangeSteps.STEP_MOVE_TO_EQUIP.ToString());
            tm.UpdateTransportCommand(tc);
            logger.Info($"[EXCHANGE] STEP 10→20 (PICKUP_NEW 완료) tc={tc.JobId}, loadedTime={tc.LoadedTime}");

            // ② 슬롯 실물 적재: 신자재 → loadSlot (PHASE_NEW)
            OccupySlot(sm, vehicle.VehicleId, loadSlot, tc.JobId, VehicleSlotEx.PHASE_NEW, "loadSlot");

            // ③ 차량 이동 상태 + 다음 waypoint(mid) 설정
            rm.UpdateVehicleTransferState(vehicle, VehicleEx.TRANSFERSTATE_TRANSFERING_DEST, MsgName);
            string midLocationId = ExchangeSteps.BuildMidLocationId(tc.MidLoc, tc.MidPortId);
            if (!UpdateAcsDestNode(rm, vehicle, midLocationId, MsgName, tc))
                return;

            // ④ mid행 RAIL-CARRIERTRANSFER(jobType=EXCHANGE, amrSlot=loadSlot)
            //    (사양서 Scenario 상 Step=10 은 RECEIVE/START 보고만 정의 — STEP_COMPLETE(10) 미발행)
            SendCarrierTransfer(mm, rm, tc, vehicle.VehicleId,
                TransportCommandEx.JOBTYPE_EXCHANGE, midLocationId, loadSlot);
        }

        /// <summary>
        /// RAIL-VEHICLEDEPOSITCOMPLETED 의 EXCHANGE 분기 (dest 구자재 반납 완료 = 여정 종결).
        /// 공용 Step1~7(차량 확인/알람 클리어) 이후, Step8 상태가드 이전에 호출된다.
        /// COMPLETE(60, DONE) 를 EXCHANGE-JOBREPORT 형식으로 발행하고 TC/슬롯/차량을 정리한다.
        /// </summary>
        public static void OnDepositCompleted(TransportCommandEx tc, VehicleEx vehicle,
            ITransferManagerEx tm, IResourceManagerEx rm, ISlotManagerEx sm,
            IHistoryManagerEx hm, IMessageManagerEx mm)
        {
            const string MsgName = "RAIL-VEHICLEDEPOSITCOMPLETED";

            if (!GuardExchangeAssigned(tc, "OnDepositCompleted")) return;

            int step = ExchangeSteps.GetStep(tc.AdditionalInfo);
            if (step != ExchangeSteps.STEP_RETURN_OLD)
            {
                logger.Warn($"[EXCHANGE] OnDepositCompleted: 예상외 STEP={step} (기대 50) — 진행 생략 tc={tc.JobId}");
                return;
            }

            // ① 차량 하역 완료 상태
            rm.UpdateVehicleTransferState(vehicle, VehicleEx.TRANSFERSTATE_DEPOSIT_COMPLETE, MsgName);

            // ② DB 전이: STEP=60 + TC 종결 (히스토리에 STEP=60 이 남도록 먼저 기록)
            DateTime now = DateTime.Now;
            tc.AdditionalInfo = ExchangeInfo.Set(tc.AdditionalInfo,
                ExchangeInfo.KEY_STEP, ExchangeSteps.STEP_DONE.ToString());
            tc.State = TransportCommandEx.STATE_COMPLETED;
            tc.UnloadedTime = now;
            tc.CompletedTime = now;
            tm.UpdateTransportCommand(tc);
            logger.Info($"[EXCHANGE] STEP 50→60 (RETURN_OLD 완료) + TC COMPLETED tc={tc.JobId}");

            // ③ 보고: 반납 단계 완료(STEP=50, CarrierSlot=unloadSlot) → 최종 COMPLETE(60, DONE)
            //    (사양서 Scenario row 12~13 — STEP_COMPLETE(50) 후 COMPLETE(60) 순)
            SendReport(mm, tc, vehicle.VehicleId, "STEP_COMPLETE", ExchangeSteps.STEP_RETURN_OLD);
            SendReport(mm, tc, vehicle.VehicleId, "COMPLETE", ExchangeSteps.STEP_DONE);

            // ④ 슬롯 정리 (idempotent — 예약/점유 잔여분 일괄 해제)
            sm.ReleaseAllByJobId(tc.JobId);
            logger.Info($"[EXCHANGE] 슬롯 전체 해제 tc={tc.JobId}, vehicleId={vehicle.VehicleId}");

            // ⑤ TC 히스토리 이관 + 삭제
            hm.CreateTransportCommandHistory(tc, "", MsgName);
            int deleted = tm.DeleteTransportCommand(tc);
            logger.Info($"[EXCHANGE] TC 히스토리 이관+삭제 tc={tc.JobId}, deleted={deleted}");

            // ⑥ 차량 정리 (기존 Deposit Step12/14~17 과 동일 primitive)
            rm.UpdateVehicleTransferState(vehicle, VehicleEx.TRANSFERSTATE_NOTASSIGNED, MsgName);
            rm.UpdateVehicleTransportCommandId(vehicle, "", MsgName);
            vehicle.TransportCommandId = "";
            rm.UpdateVehicle(vehicle, "Path", "");
            vehicle.Path = "";
            rm.UpdateVehicleAcsDestNodeId(vehicle, "", MsgName);
            vehicle.AcsDestNodeId = "";
            rm.UpdateVehicleProcessingState(vehicle, VehicleEx.PROCESSINGSTATE_IDLE, MsgName);
            logger.Info($"[EXCHANGE] 여정 완료 — 차량 초기화 vehicleId={vehicle.VehicleId}, tc={tc.JobId}");
        }

        /// <summary>
        /// RailVehicleExchangeCompletedWorkflow 본체 (설비 액션 완료 reply).
        /// 사양서 시나리오상 설비 교체는 MES ACTIONCMD 2건(UNLOAD → LOAD) 시퀀스로 진행되므로,
        /// 진행 중 액션(ACT — RouteActionCmdToVehicleActivity 가 기록)에 따라 분기한다:
        ///  - ACT 빈값: 설비 액션 미진행 — mid행 이동/도킹 완료 reply 로 간주하고 무시.
        ///  - ACT=UNLOAD (STEP=20): 구자재 회수 완료 → STEP=30, unloadSlot 적재(OLD),
        ///    STEP_COMPLETE(30). 차량은 설비 앞 대기 (MES 의 ACTIONCMD(LOAD) 대기).
        ///  - ACT=LOAD (STEP=30): 신자재 투입 완료 → loadSlot 하치, STEP_COMPLETE(40) →
        ///    STEP=50 + dest행 LOAD 명령 (RETURN_OLD 이동 시작).
        /// </summary>
        public static void OnExchangeCompleted(TransportCommandEx tc, VehicleEx vehicle,
            ITransferManagerEx tm, IResourceManagerEx rm, ISlotManagerEx sm, IMessageManagerEx mm)
        {
            const string MsgName = "RAIL-VEHICLEEXCHANGECOMPLETED";

            if (!GuardExchangeAssigned(tc, "OnExchangeCompleted")) return;

            int step = ExchangeSteps.GetStep(tc.AdditionalInfo);
            string act = ExchangeInfo.Get(tc.AdditionalInfo, ExchangeInfo.KEY_ACT);

            if (string.IsNullOrEmpty(act))
            {
                logger.Info($"[EXCHANGE] OnExchangeCompleted: 진행 중 액션 없음(ACT 빈값) — " +
                            $"이동/도킹 완료 reply 로 간주하고 무시. STEP={step}, tc={tc.JobId}");
                return;
            }

            if (ExchangeInfo.ACT_UNLOAD.Equals(act, StringComparison.OrdinalIgnoreCase))
            {
                if (step != ExchangeSteps.STEP_MOVE_TO_EQUIP)
                {
                    logger.Warn($"[EXCHANGE] OnExchangeCompleted: ACT=UNLOAD 인데 STEP={step} (기대 20) — 진행 생략 tc={tc.JobId}");
                    return;
                }

                string unloadSlot = ExchangeInfo.Get(tc.AdditionalInfo, ExchangeInfo.KEY_UNLOADSLOT);

                // ① DB 전이: STEP=30 + ACT 클리어 (설비 앞 대기 유지 — 이동 명령 없음)
                tc.AdditionalInfo = ExchangeInfo.Set(tc.AdditionalInfo,
                    ExchangeInfo.KEY_STEP, ExchangeSteps.STEP_UNLOAD_OLD.ToString());
                tc.AdditionalInfo = ExchangeInfo.Set(tc.AdditionalInfo, ExchangeInfo.KEY_ACT, "");
                tm.UpdateTransportCommand(tc);
                logger.Info($"[EXCHANGE] STEP 20→30 (구자재 회수 완료) tc={tc.JobId}");

                // ② 슬롯 실물 전이: 구자재 → unloadSlot 적재
                OccupySlot(sm, vehicle.VehicleId, unloadSlot, tc.JobId, VehicleSlotEx.PHASE_OLD, "unloadSlot");

                // ③ 보고: 회수 단계 완료 (CarrierSlot=unloadSlot)
                SendReport(mm, tc, vehicle.VehicleId, "STEP_COMPLETE", ExchangeSteps.STEP_UNLOAD_OLD);
                return;
            }

            if (ExchangeInfo.ACT_LOAD.Equals(act, StringComparison.OrdinalIgnoreCase))
            {
                if (step != ExchangeSteps.STEP_UNLOAD_OLD)
                {
                    logger.Warn($"[EXCHANGE] OnExchangeCompleted: ACT=LOAD 인데 STEP={step} (기대 30) — 진행 생략 tc={tc.JobId}");
                    return;
                }

                string loadSlot = ExchangeInfo.Get(tc.AdditionalInfo, ExchangeInfo.KEY_LOADSLOT);
                string unloadSlot = ExchangeInfo.Get(tc.AdditionalInfo, ExchangeInfo.KEY_UNLOADSLOT);

                // ① DB 전이: STEP 30→(40 완료)→50 + ACT 클리어 (RETURN_OLD 이동 시작)
                tc.AdditionalInfo = ExchangeInfo.Set(tc.AdditionalInfo,
                    ExchangeInfo.KEY_STEP, ExchangeSteps.STEP_RETURN_OLD.ToString());
                tc.AdditionalInfo = ExchangeInfo.Set(tc.AdditionalInfo, ExchangeInfo.KEY_ACT, "");
                tm.UpdateTransportCommand(tc);
                logger.Info($"[EXCHANGE] STEP 30→40→50 (신자재 투입 완료, 반납 이동 시작) tc={tc.JobId}");

                // ② 슬롯 실물 전이: 신자재 투입 완료 → loadSlot 하치
                ReleaseSlot(sm, vehicle.VehicleId, loadSlot, "loadSlot");

                // ③ 다음 waypoint(dest) 설정 + dest행 RAIL-CARRIERTRANSFER(jobType=LOAD, amrSlot=unloadSlot)
                if (!UpdateAcsDestNode(rm, vehicle, tc.Dest, MsgName, tc))
                    return;
                SendCarrierTransfer(mm, rm, tc, vehicle.VehicleId,
                    TransportCommandEx.JOBTYPE_LOAD, tc.Dest, unloadSlot);

                // ④ 보고: 투입 단계 완료 (STEP=40, CarrierSlot=loadSlot)
                SendReport(mm, tc, vehicle.VehicleId, "STEP_COMPLETE", ExchangeSteps.STEP_LOAD_NEW);
                return;
            }

            logger.Warn($"[EXCHANGE] OnExchangeCompleted: 알 수 없는 ACT='{act}' — 진행 생략 tc={tc.JobId}");
        }

        // ─────────────────────────────────────────────────────────────
        //  내부 공통
        // ─────────────────────────────────────────────────────────────

        private static bool GuardExchangeAssigned(TransportCommandEx tc, string caller)
        {
            if (!TransportCommandEx.STATE_EXCHANGE_ASSIGNED.Equals(tc.State, StringComparison.OrdinalIgnoreCase))
            {
                logger.Warn($"[EXCHANGE] {caller}: 예상외 TC 상태 — state={tc.State}, tc={tc.JobId}. 진행 생략.");
                return false;
            }
            return true;
        }

        /// <summary>LocationId → StationId. 조회 실패 시 "" (호출자가 무효 처리).</summary>
        private static string ResolveStationId(IResourceManagerEx rm, string locationId)
        {
            if (string.IsNullOrEmpty(locationId)) return "";
            try
            {
                LocationEx loc = rm.GetLocationByLocationId(locationId);
                return loc?.StationId ?? "";
            }
            catch (Exception ex)
            {
                logger.Warn($"[EXCHANGE] Location 조회 실패 locationId={locationId} - {ex.Message}");
                return "";
            }
        }

        private static bool UpdateAcsDestNode(IResourceManagerEx rm, VehicleEx vehicle,
            string locationId, string msgName, TransportCommandEx tc)
        {
            string stationId = ResolveStationId(rm, locationId);
            if (string.IsNullOrEmpty(stationId))
            {
                logger.Error($"[EXCHANGE] waypoint StationId 조회 실패 — locationId={locationId}, tc={tc.JobId}. " +
                             "AcsDestNodeId 미갱신, 이동 명령 생략 (STEP/슬롯은 유지 — 운영자 개입 필요).");
                return false;
            }

            rm.UpdateVehicleAcsDestNodeId(vehicle, stationId, msgName);
            vehicle.AcsDestNodeId = stationId;
            logger.Info($"[EXCHANGE] AcsDestNodeId → {stationId} (loc={locationId}) vehicleId={vehicle.VehicleId}");
            return true;
        }

        private static void SendCarrierTransfer(IMessageManagerEx mm, IResourceManagerEx rm,
            TransportCommandEx tc, string vehicleId, string jobType, string targetLocationId, string slot)
        {
            int amrSlot;
            if (!int.TryParse(slot, out amrSlot) || amrSlot < 1)
                amrSlot = 1;

            string json = CarrierTransferJsonBuilder.Build(tc, vehicleId, jobType, targetLocationId, amrSlot, rm, logger);
            if (string.IsNullOrEmpty(json))
            {
                logger.Error($"[EXCHANGE] CARRIERTRANSFER JSON 빌드 실패 tc={tc.JobId}, target={targetLocationId}");
                return;
            }

            mm.SendCarrierTransferJson(json);
            logger.Info($"[EXCHANGE] RAIL-CARRIERTRANSFER({jobType}) 전송 tc={tc.JobId}, target={targetLocationId}, amrSlot={amrSlot}");
        }

        private static void SendReport(IMessageManagerEx mm, TransportCommandEx tc,
            string vehicleId, string reportType, int step)
        {
            string loadSlot = ExchangeInfo.Get(tc.AdditionalInfo, ExchangeInfo.KEY_LOADSLOT);
            string unloadSlot = ExchangeInfo.Get(tc.AdditionalInfo, ExchangeInfo.KEY_UNLOADSLOT);

            mm.SendExchangeJobReportToHost(
                reportType, tc.JobId, vehicleId,
                step.ToString(), ExchangeSteps.StepName(step),
                ExchangeSteps.CarrierSlotFor(step, loadSlot, unloadSlot),
                TransportCommandEx.JOBTYPE_EXCHANGE, tc.GetMaterialType() ?? "", "0", "");
        }

        private static void OccupySlot(ISlotManagerEx sm, string vehicleId, string slot,
            string jobId, string phase, string label)
        {
            int slotNo;
            if (!int.TryParse(slot, out slotNo))
            {
                logger.Warn($"[EXCHANGE] {label} 번호 결손('{slot}') — 슬롯 적재 생략 job={jobId}");
                return;
            }
            sm.Occupy(vehicleId, slotNo, jobId, phase);
        }

        private static void ReleaseSlot(ISlotManagerEx sm, string vehicleId, string slot, string label)
        {
            int slotNo;
            if (!int.TryParse(slot, out slotNo))
            {
                logger.Warn($"[EXCHANGE] {label} 번호 결손('{slot}') — 슬롯 하치 생략 vehicleId={vehicleId}");
                return;
            }
            sm.Release(vehicleId, slotNo);
        }
    }
}
