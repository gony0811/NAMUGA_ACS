# ACS 마스터 테이블 목록 — backup/restore/deploy 스크립트가 dot-source 로 공유
#
# 사용:
#   . $PSScriptRoot\_master-tables.ps1
#   $MasterTables                    # 17 개 (기본)
#   $MasterTablesWithApplication     # 18 개 (-IncludeApplication 동등)
#
# 운영/이력/로그 테이블은 의도적으로 제외 (신규 서버 init 시 비어 있어야 정상).

$script:MasterTables = @(
    # Path / Layout
    'NA_R_NODE', 'NA_R_LINK', 'NA_R_LINK_ZONE',
    'NA_R_STATION', 'NA_R_LOCATION', 'NA_R_BAY', 'NA_R_ZONE',
    # Intersection 정의
    'NA_T_INTERSECTION', 'NA_R_ORDER_PAIR',
    # Vehicle 마스터
    'NA_R_VEHICLE',
    # 자재 / 알람 정의
    'NA_M_CARRIER', 'NA_A_ALARMSPEC',
    # 통신 설정 (remoteIp/machineName 등 사이트 종속값은 이관 후 수정 필요)
    'NA_C_MQTT', 'NA_C_NIO',
    # 사이트 / 옵션
    'NA_R_SPECIALCONFIG',
    'NA_X_OPTION', 'NA_X_APPLICATION_MANAGER'
)

# NA_X_APPLICATION 은 ApplicationInitializer 가 런타임에 만들어 PK 충돌 위험 — 사이트 이관 등 명시적으로 필요할 때만 포함.
$script:MasterTablesWithApplication = $script:MasterTables + 'NA_X_APPLICATION'
