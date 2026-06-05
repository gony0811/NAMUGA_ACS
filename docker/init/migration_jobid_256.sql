-- ============================================================================
-- migration_jobid_256.sql
--
-- jobId 및 그 값을 참조 저장하는 transportCommandId 컬럼을 varchar(64) → varchar(256) 으로 확장.
-- 대응 사고: MES 가 보낸 JobID 75자가 NA_T_TRANSPORTCMD.jobId varchar(64) 를 초과해
--           SaveChanges 실패 → JOBREPORT ErrorCode=03 NACK.
--
-- 적용 대상 운영 DB: acsdb@10.0.26.2
-- 실행 예:
--   psql -h 10.0.26.2 -U acsuser -d acsdb -f migration_jobid_256.sql
--
-- 멱등성: ALTER COLUMN TYPE 은 현재 타입이 동일하면 사실상 no-op (테이블 rewrite 만 발생).
--        NA_T_TRANSPORTCMD.jobId 는 이미 256 으로 수동 확장된 상태일 수 있음.
--
-- 주의: 각 ALTER 는 ACCESS EXCLUSIVE 락을 잠깐 잡음. 트래픽 적은 시점에 실행 권장.
-- ============================================================================

BEGIN;

-- jobId (3개)
ALTER TABLE public."NA_T_TRANSPORTCMD"        ALTER COLUMN "jobId" TYPE character varying(256);
ALTER TABLE public."NA_H_TRANSPORTCMDHISTORY" ALTER COLUMN "jobId" TYPE character varying(256);
ALTER TABLE public."NA_Q_TRANSPORTCMDREQUEST" ALTER COLUMN "jobId" TYPE character varying(256);

-- transportCommandId (6개) — jobId 값을 참조 저장하므로 동일 길이로 통일
ALTER TABLE public."NA_A_ALARM"               ALTER COLUMN "transportCommandId" TYPE character varying(256);
ALTER TABLE public."NA_H_ALARMRPTHISTORY"     ALTER COLUMN "transportCommandId" TYPE character varying(256);
ALTER TABLE public."NA_H_ALARMTIMEHISTORY"    ALTER COLUMN "transportCommandId" TYPE character varying(256);
ALTER TABLE public."NA_H_VEHICLEHISTORY"      ALTER COLUMN "transportCommandId" TYPE character varying(256);
ALTER TABLE public."NA_L_LOGMESSAGE"          ALTER COLUMN "transportCommandId" TYPE character varying(256);
ALTER TABLE public."NA_R_VEHICLE"             ALTER COLUMN "transportCommandId" TYPE character varying(256);

COMMIT;

-- ============================================================================
-- 사후 검증:
--   \d "NA_T_TRANSPORTCMD"
--   \d "NA_H_TRANSPORTCMDHISTORY"
--   \d "NA_Q_TRANSPORTCMDREQUEST"
--   \d "NA_A_ALARM"
--   \d "NA_H_ALARMRPTHISTORY"
--   \d "NA_H_ALARMTIMEHISTORY"
--   \d "NA_H_VEHICLEHISTORY"
--   \d "NA_L_LOGMESSAGE"
--   \d "NA_R_VEHICLE"
-- 각 컬럼이 character varying(256) 임을 확인.
-- ============================================================================
