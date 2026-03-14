# =============================================================================
# ACS Multi-Process Build Script (Windows PowerShell)
# 한 번 빌드 후, 프로세스별로 복사하고 App.config + 실행파일명을 설정합니다.
# =============================================================================
param(
    [string]$Configuration = "Debug"
)

$ErrorActionPreference = "Stop"

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$Solution = Join-Path $ScriptDir "ACS.sln"
$AppConfigTemplate = Join-Path $ScriptDir "ACS.Builder\App.config"
$BuildOutput = Join-Path $ScriptDir "bin\$Configuration"
$DeployDir = Join-Path $ScriptDir "deploy"

# Process definitions: Name, Type, Msb, Base, ServicePath
$Processes = @(
    @{ Name="TS01_P"; Type="trans";   Msb="rabbitmq"; Base="database"; ServicePath="reload/trans" },
    @{ Name="CS01_P"; Type="control"; Msb="rabbitmq"; Base="database"; ServicePath="" },
    @{ Name="DS01_P"; Type="daemon";  Msb="rabbitmq"; Base="database"; ServicePath="" },
    @{ Name="ES01_P"; Type="ei";      Msb="rabbitmq"; Base="database"; ServicePath="" },
    @{ Name="HS01_P"; Type="host";    Msb="rabbitmq"; Base="database"; ServicePath="" }
)

Write-Host "============================================"
Write-Host " ACS Multi-Process Builder"
Write-Host " Configuration: $Configuration"
Write-Host " Deploy to: $DeployDir"
Write-Host "============================================"
Write-Host ""

# Step 1: Build solution once
Write-Host "Step 1: Building ACS solution..."
dotnet build $Solution -c $Configuration -q -m:1
if ($LASTEXITCODE -ne 0) { throw "Build failed with exit code $LASTEXITCODE" }
Write-Host "  Build completed."
Write-Host ""

# Clean deploy directory
if (Test-Path $DeployDir) {
    Remove-Item $DeployDir -Recurse -Force
}

# Step 2: Create per-process deploy folders
Write-Host "Step 2: Creating process deployments..."
foreach ($Proc in $Processes) {
    $Name = $Proc.Name
    $Type = $Proc.Type
    $Msb = $Proc.Msb
    $Base = $Proc.Base
    $ServicePath = $Proc.ServicePath

    Write-Host "  --- $Name (type=$Type) ---"

    $ProcDir = Join-Path $DeployDir $Name
    New-Item -ItemType Directory -Path $ProcDir -Force | Out-Null

    # Copy entire build output
    Copy-Item -Path "$BuildOutput\*" -Destination $ProcDir -Recurse -Force

    # 네이티브 호스트(apphost) 제거 — DLL명이 하드코딩되어 리네임 불가
    $appHost = Join-Path $ProcDir "ACS.Builder.exe"
    if (Test-Path $appHost) { Remove-Item $appHost -Force }

    # Rename DLL/config: ACS.Builder -> $Name
    $renames = @(
        @{ From="ACS.Builder.dll"; To="$Name.dll" },
        @{ From="ACS.Builder.pdb"; To="$Name.pdb" },
        @{ From="ACS.Builder.deps.json"; To="$Name.deps.json" },
        @{ From="ACS.Builder.runtimeconfig.json"; To="$Name.runtimeconfig.json" }
    )

    foreach ($r in $renames) {
        $src = Join-Path $ProcDir $r.From
        $dst = Join-Path $ProcDir $r.To
        if (Test-Path $src) {
            Move-Item $src $dst -Force
        }
    }

    # Fix deps.json internal references
    $depsFile = Join-Path $ProcDir "$Name.deps.json"
    if (Test-Path $depsFile) {
        (Get-Content $depsFile -Raw).Replace("ACS.Builder", $Name) | Set-Content $depsFile -Encoding UTF8
    }

    # Generate per-process App.config (dll.config)
    $configContent = Get-Content $AppConfigTemplate -Raw
    $configContent = $configContent.Replace("__PROCESS_NAME__", $Name)
    $configContent = $configContent.Replace("__PROCESS_TYPE__", $Type)
    $configContent = $configContent.Replace("__PROCESS_MSB__", $Msb)
    $configContent = $configContent.Replace("__PROCESS_BASE__", $Base)
    $configContent = $configContent.Replace("__PROCESS_SERVICEPATH__", $ServicePath)
    Set-Content -Path (Join-Path $ProcDir "$Name.dll.config") -Value $configContent -Encoding UTF8

    # Remove original ACS.Builder.dll.config
    $oldConfig = Join-Path $ProcDir "ACS.Builder.dll.config"
    if (Test-Path $oldConfig) { Remove-Item $oldConfig -Force }

    # Rename in Process/ subdirectory
    $processDir = Join-Path $ProcDir "Process"
    if (Test-Path $processDir) {
        $pDll = Join-Path $processDir "ACS.Builder.dll"
        $pPdb = Join-Path $processDir "ACS.Builder.pdb"
        if (Test-Path $pDll) { Move-Item $pDll (Join-Path $processDir "$Name.dll") -Force }
        if (Test-Path $pPdb) { Move-Item $pPdb (Join-Path $processDir "$Name.pdb") -Force }
    }

    Write-Host "      -> $ProcDir\"
}

Write-Host ""
Write-Host "============================================"
Write-Host " Build complete!"
Write-Host "============================================"
Write-Host ""
Write-Host "Deploy directory contents:"
foreach ($Proc in $Processes) {
    $Name = $Proc.Name
    $Dir = Join-Path $DeployDir $Name
    if (Test-Path $Dir) {
        $ExePath = Join-Path $Dir "$Name.exe"
        $DllPath = Join-Path $Dir "$Name.dll"
        if (Test-Path $ExePath) {
            Write-Host "  [OK] $Name\ ($Name.exe)"
        } elseif (Test-Path $DllPath) {
            Write-Host "  [OK] $Name\ ($Name.dll)"
        } else {
            Write-Host "  [??] $Name\ (executable not found)"
        }
    } else {
        Write-Host "  [FAIL] $Name\"
    }
}
Write-Host ""
Write-Host "Run a process: cd deploy\<NAME> && dotnet <NAME>.dll"
Write-Host " or: cd deploy\<NAME> && dotnet <NAME>.dll"
