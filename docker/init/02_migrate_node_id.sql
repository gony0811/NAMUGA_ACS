--
-- NA_R_NODE 마이그레이션: id(varchar PK) → id(serial PK) + node_id(varchar UNIQUE)
-- 기존 운영 DB에 적용하는 스크립트
--
-- 실행 전 반드시 백업할 것:
--   pg_dump -t '"NA_R_NODE"' acsdb > NA_R_NODE_backup.sql
--

BEGIN;

-- 1. 기존 PK 제약 삭제
ALTER TABLE public."NA_R_NODE"
    DROP CONSTRAINT IF EXISTS "PK_NA_R_NODE";

-- 2. 기존 id 컬럼을 node_id로 이름 변경
ALTER TABLE public."NA_R_NODE"
    RENAME COLUMN id TO node_id;

-- 3. 새 id 컬럼 추가 (serial = auto-increment)
ALTER TABLE public."NA_R_NODE"
    ADD COLUMN id serial;

-- 4. 새 id를 PK로 설정
ALTER TABLE public."NA_R_NODE"
    ADD CONSTRAINT "PK_NA_R_NODE" PRIMARY KEY (id);

-- 5. node_id에 UNIQUE 제약 추가
ALTER TABLE public."NA_R_NODE"
    ADD CONSTRAINT "UQ_NA_R_NODE_node_id" UNIQUE (node_id);

-- 6. node_id NOT NULL 제약 추가
ALTER TABLE public."NA_R_NODE"
    ALTER COLUMN node_id SET NOT NULL;

COMMIT;
