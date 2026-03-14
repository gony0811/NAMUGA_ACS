#!/bin/bash
# =============================================================================
# ACS Multi-Process Build Script (macOS / Linux)
# 한 번 빌드 후, 프로세스별로 복사하고 App.config + 실행파일명을 설정합니다.
# =============================================================================
set -e

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
SOLUTION="$SCRIPT_DIR/ACS.sln"
APP_CONFIG_TEMPLATE="$SCRIPT_DIR/ACS.Builder/App.config"
BUILD_OUTPUT="$SCRIPT_DIR/bin/Debug"
DEPLOY_DIR="$SCRIPT_DIR/deploy"

# 프로세스 정의: NAME|TYPE|MSB|BASE|SERVICEPATH
PROCESSES=(
    "TS01_P|trans|rabbitmq|database|reload/trans"
    "CS01_P|control|rabbitmq|database|"
    "DS01_P|daemon|rabbitmq|database|"
    "ES01_P|ei|rabbitmq|database|"
    "HS01_P|host|rabbitmq|database|"
)

echo "============================================"
echo " ACS Multi-Process Builder"
echo " Deploy to: $DEPLOY_DIR"
echo "============================================"
echo ""

# 1단계: 빌드 (ACS.Builder라는 이름으로 한 번만)
echo "Step 1: Building ACS solution..."
dotnet build "$SOLUTION" -q -m:1
echo "  Build completed."
echo ""

# 이전 deploy 폴더 정리
rm -rf "$DEPLOY_DIR"

# 2단계: 프로세스별 배포 폴더 생성
echo "Step 2: Creating process deployments..."
for PROC in "${PROCESSES[@]}"; do
    IFS='|' read -r NAME TYPE MSB BASE SERVICEPATH <<< "$PROC"

    echo "  --- $NAME (type=$TYPE) ---"

    PROC_DIR="$DEPLOY_DIR/$NAME"
    mkdir -p "$PROC_DIR"

    # 빌드 결과 전체 복사
    cp -R "$BUILD_OUTPUT"/* "$PROC_DIR/"

    # 네이티브 호스트(apphost) 제거 — DLL명이 하드코딩되어 리네임 불가
    # dotnet $NAME.dll 로 실행해야 함
    rm -f "$PROC_DIR/ACS.Builder"
    if [ -f "$PROC_DIR/ACS.Builder.dll" ]; then
        mv "$PROC_DIR/ACS.Builder.dll" "$PROC_DIR/$NAME.dll"
    fi
    if [ -f "$PROC_DIR/ACS.Builder.pdb" ]; then
        mv "$PROC_DIR/ACS.Builder.pdb" "$PROC_DIR/$NAME.pdb"
    fi
    if [ -f "$PROC_DIR/ACS.Builder.deps.json" ]; then
        mv "$PROC_DIR/ACS.Builder.deps.json" "$PROC_DIR/$NAME.deps.json"
        # deps.json 내부 참조도 수정
        sed -i.tmp 's/ACS\.Builder/'"$NAME"'/g' "$PROC_DIR/$NAME.deps.json"
        rm -f "$PROC_DIR/$NAME.deps.json.tmp"
    fi
    if [ -f "$PROC_DIR/ACS.Builder.runtimeconfig.json" ]; then
        mv "$PROC_DIR/ACS.Builder.runtimeconfig.json" "$PROC_DIR/$NAME.runtimeconfig.json"
    fi

    # App.config (dll.config) 생성 — placeholder 치환
    sed \
        -e "s|__PROCESS_NAME__|$NAME|g" \
        -e "s|__PROCESS_TYPE__|$TYPE|g" \
        -e "s|__PROCESS_MSB__|$MSB|g" \
        -e "s|__PROCESS_BASE__|$BASE|g" \
        -e "s|__PROCESS_SERVICEPATH__|$SERVICEPATH|g" \
        "$APP_CONFIG_TEMPLATE" > "$PROC_DIR/$NAME.dll.config"

    # 기존 ACS.Builder.dll.config 제거
    rm -f "$PROC_DIR/ACS.Builder.dll.config"

    # Process/ 내 ACS.Builder.dll도 리네임
    if [ -f "$PROC_DIR/Process/ACS.Builder.dll" ]; then
        mv "$PROC_DIR/Process/ACS.Builder.dll" "$PROC_DIR/Process/$NAME.dll"
    fi
    if [ -f "$PROC_DIR/Process/ACS.Builder.pdb" ]; then
        mv "$PROC_DIR/Process/ACS.Builder.pdb" "$PROC_DIR/Process/$NAME.pdb"
    fi

    echo "      -> $PROC_DIR/"
done

echo ""
echo "============================================"
echo " Build complete!"
echo "============================================"
echo ""
echo "Deploy directory:"
for PROC in "${PROCESSES[@]}"; do
    IFS='|' read -r NAME _ <<< "$PROC"
    if [ -f "$DEPLOY_DIR/$NAME/$NAME" ]; then
        SIZE=$(du -sh "$DEPLOY_DIR/$NAME" | cut -f1)
        echo "  [OK] $NAME/ ($SIZE) - ./$NAME"
    elif [ -f "$DEPLOY_DIR/$NAME/$NAME.dll" ]; then
        SIZE=$(du -sh "$DEPLOY_DIR/$NAME" | cut -f1)
        echo "  [OK] $NAME/ ($SIZE) - dotnet $NAME.dll"
    else
        echo "  [FAIL] $NAME/"
    fi
done
echo ""
echo "Run: cd deploy/<NAME> && dotnet <NAME>.dll"
