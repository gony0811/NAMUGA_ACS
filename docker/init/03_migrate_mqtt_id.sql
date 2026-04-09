--
-- NA_C_MQTT 마이그레이션: id(varchar PK) → id(serial PK) + NAME(UNIQUE)
-- 기존 운영 DB에 적용하는 스크립트
--
-- 실행 전 반드시 백업할 것:
--   pg_dump -t '"NA_C_MQTT"' acsdb > NA_C_MQTT_backup.sql
--

BEGIN;

-- 1. 기존 PK 제약 삭제
ALTER TABLE public."NA_C_MQTT"
    DROP CONSTRAINT IF EXISTS "NA_C_MQTT_pkey";

-- 2. 기존 id 컬럼 삭제 (NAME과 값이 중복되므로 제거)
ALTER TABLE public."NA_C_MQTT"
    DROP COLUMN id;

-- 3. 새 id 컬럼 추가 (serial = auto-increment)
ALTER TABLE public."NA_C_MQTT"
    ADD COLUMN id serial;

-- 4. 새 id를 PK로 설정
ALTER TABLE public."NA_C_MQTT"
    ADD CONSTRAINT "NA_C_MQTT_pkey" PRIMARY KEY (id);

-- 5. NAME에 UNIQUE 제약 추가
ALTER TABLE public."NA_C_MQTT"
    ADD CONSTRAINT "NA_C_MQTT_NAME_unique" UNIQUE ("NAME");

-- 6. NAME NOT NULL 제약 추가
ALTER TABLE public."NA_C_MQTT"
    ALTER COLUMN "NAME" SET NOT NULL;

COMMIT;
