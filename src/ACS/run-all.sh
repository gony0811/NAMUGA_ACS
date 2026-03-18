#!/bin/bash
# ============================================================
# ACS 멀티 프로세스 빌드 & 실행 스크립트
# UI01_P (ui), HS01_P (host), CS01_P (control) 동시 실행
# ============================================================

set -e

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
PUBLISH_DIR="$SCRIPT_DIR/publish"
DEPLOY_DIR="$SCRIPT_DIR/deploy"
PROCESSES=("UI01_P" "HS01_P")
PIDS=()

# 색상 코드 (프로세스별 구분)
COLOR_0="\033[1;34m"  # 파랑
COLOR_1="\033[1;32m"  # 초록
COLOR_2="\033[1;33m"  # 노랑
RESET="\033[0m"

# 종료 시 모든 프로세스 정리 (프로세스 그룹 단위로 종료)
cleanup() {
    echo ""
    echo "=========================================="
    echo " 모든 ACS 프로세스를 종료합니다..."
    echo "=========================================="
    for pid in "${PIDS[@]}"; do
        if kill -0 "$pid" 2>/dev/null; then
            # 서브셸 + 파이프 자식 프로세스 모두 종료
            kill -- -"$pid" 2>/dev/null || kill "$pid" 2>/dev/null
            echo "  PGID $pid 종료됨"
        fi
    done
    # dotnet 잔여 프로세스 정리
    pkill -f "dotnet ACS.App.dll" 2>/dev/null || true
    wait 2>/dev/null
    echo "완료."
    exit 0
}

trap cleanup SIGINT SIGTERM

# ============================================================
# 1. 빌드
# ============================================================
echo "=========================================="
echo " [1/3] ACS.App 빌드 중..."
echo "=========================================="
dotnet publish "$SCRIPT_DIR/ACS.App/ACS.App.csproj" \
    -c Release \
    -o "$PUBLISH_DIR/base" \
    --no-self-contained

echo ""

# ============================================================
# 2. 프로세스별 배포 디렉토리 구성
# ============================================================
echo "=========================================="
echo " [2/3] 프로세스별 배포 구성 중..."
echo "=========================================="

for proc in "${PROCESSES[@]}"; do
    PROC_DIR="$PUBLISH_DIR/$proc"

    # 기존 디렉토리 정리 후 복사
    rm -rf "$PROC_DIR"
    cp -r "$PUBLISH_DIR/base" "$PROC_DIR"

    # 프로세스별 appsettings.json 덮어쓰기
    cp "$DEPLOY_DIR/$proc/appsettings.json" "$PROC_DIR/appsettings.json"

    echo "  $proc -> $PROC_DIR"
done

echo ""

# ============================================================
# 3. 프로세스 동시 실행
# ============================================================
echo "=========================================="
echo " [3/3] ACS 프로세스 실행 중..."
echo "=========================================="
echo ""
echo "  UI01_P (ui)      - API: 5100"
echo "  HS01_P (host)    - API: 5101, TCP Listen: 3334, TCP Send: 3333"
echo "  CS01_P (control) - API: 5102"
echo ""
echo "  종료: Ctrl+C"
echo "=========================================="
echo ""

COLORS=("$COLOR_0" "$COLOR_1" "$COLOR_2")

for i in 0 1 2; do
    proc="${PROCESSES[$i]}"
    color="${COLORS[$i]}"
    PROC_DIR="$PUBLISH_DIR/$proc"

    # 로그 디렉토리 생성
    mkdir -p "$PROC_DIR/logs"

    # CWD를 프로세스 디렉토리로 설정하여 실행 (appsettings.json을 CWD에서 찾도록)
    # set -m으로 각 서브셸을 별도 프로세스 그룹으로 실행 → kill -- -PID로 그룹 종료 가능
    set -m
    (cd "$PROC_DIR" && exec dotnet ACS.App.dll < /dev/null 2>&1 | while IFS= read -r line; do
        echo -e "${color}[$proc]${RESET} $line"
    done) &
    PIDS+=($!)
    set +m

    pid_count=${#PIDS[@]}
    last_pid="${PIDS[$((pid_count - 1))]}"
    echo "  $proc 시작됨 (PID: $last_pid)"
done

echo ""
echo "모든 프로세스가 실행 중입니다. Ctrl+C로 종료하세요."
echo ""

# 모든 백그라운드 프로세스 대기 (trap이 동작하도록 개별 wait)
while true; do
    alive=false
    for pid in "${PIDS[@]}"; do
        if kill -0 "$pid" 2>/dev/null; then
            alive=true
            break
        fi
    done
    $alive || break
    sleep 1
done
