#requires -Version 5.1
<#
.SYNOPSIS
    ACS 멀티 프로세스 빌드 & 실행 스크립트 (Windows PowerShell)

.DESCRIPTION
    인자 없이 실행하면 전체 6개 프로세스, 인자로 지정 시 해당 프로세스만 실행.
    빌드 -> 프로세스별 배포 디렉토리 구성 -> 동시 실행 -> Ctrl+C 시 일괄 종료.

.EXAMPLE
    .\run-all.ps1
    전체 6개 프로세스 실행

.EXAMPLE
    .\run-all.ps1 TS01_P HS01_P
    지정한 프로세스만 실행

.NOTES
    실행정책 차단 시:
        powershell -ExecutionPolicy Bypass -File .\run-all.ps1
#>
[CmdletBinding()]
param(
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$Processes
)

$ErrorActionPreference = 'Stop'

$ScriptDir  = if ($PSScriptRoot) { $PSScriptRoot } else { Split-Path -Parent $MyInvocation.MyCommand.Path }
$PublishDir = Join-Path $ScriptDir 'publish'
$DeployDir  = Join-Path $ScriptDir 'deploy'

#$AllProcesses = @('TS01_P','ES01_P','DS01_P','CS01_P','HS01_P','UI01_P')
$AllProcesses = @('TS01_P','ES01_P','DS01_P','CS01_P','HS01_P','UI01_P')

# 콘솔 인코딩 UTF-8 (한국어 출력 깨짐 방지)
try {
    [Console]::OutputEncoding = [System.Text.Encoding]::UTF8
    $OutputEncoding           = [System.Text.Encoding]::UTF8
} catch { }

# ANSI VT 모드 활성화 (Windows 10+ conhost / Windows Terminal)
if (-not ([System.Management.Automation.PSTypeName]'Win32.ConsoleVT').Type) {
    Add-Type -Namespace Win32 -Name ConsoleVT -MemberDefinition @'
        [System.Runtime.InteropServices.DllImport("kernel32.dll", SetLastError=true)]
        public static extern System.IntPtr GetStdHandle(int nStdHandle);
        [System.Runtime.InteropServices.DllImport("kernel32.dll", SetLastError=true)]
        public static extern bool GetConsoleMode(System.IntPtr hConsoleHandle, out uint lpMode);
        [System.Runtime.InteropServices.DllImport("kernel32.dll", SetLastError=true)]
        public static extern bool SetConsoleMode(System.IntPtr hConsoleHandle, uint dwMode);
'@
}
try {
    $stdOut = [Win32.ConsoleVT]::GetStdHandle(-11)  # STD_OUTPUT_HANDLE
    $cmode  = 0
    if ([Win32.ConsoleVT]::GetConsoleMode($stdOut, [ref]$cmode)) {
        [void][Win32.ConsoleVT]::SetConsoleMode($stdOut, $cmode -bor 0x0004)
    }
} catch { }

$ESC   = [char]27
$Reset = "${ESC}[0m"
$Colors = @{
    'TS01_P' = "${ESC}[1;36m"  # cyan
    'ES01_P' = "${ESC}[1;35m"  # magenta
    'DS01_P' = "${ESC}[1;33m"  # yellow
    'CS01_P' = "${ESC}[1;32m"  # green
    'HS01_P' = "${ESC}[1;34m"  # blue
    'UI01_P' = "${ESC}[1;31m"  # red
}

# ============================================================
# 인자 처리
# ============================================================
if (-not $Processes -or $Processes.Count -eq 0) {
    $Processes = $AllProcesses
} else {
    foreach ($p in $Processes) {
        if (-not (Test-Path (Join-Path $DeployDir $p))) {
            Write-Host "오류: deploy/$p 디렉토리가 없습니다." -ForegroundColor Red
            Write-Host "사용 가능: $($AllProcesses -join ', ')"
            exit 1
        }
    }
}

# ============================================================
# 1. 빌드
# ============================================================
Write-Host "=========================================="
Write-Host " [1/3] ACS.App 빌드 중..."
Write-Host "=========================================="
$csproj  = Join-Path $ScriptDir 'ACS.App\ACS.App.csproj'
$baseOut = Join-Path $PublishDir 'base'
& dotnet publish $csproj -c Release -o $baseOut --no-self-contained
if ($LASTEXITCODE -ne 0) {
    Write-Host "빌드 실패 (exit code: $LASTEXITCODE)" -ForegroundColor Red
    exit 1
}
Write-Host ""

# ============================================================
# 2. 프로세스별 배포 디렉토리 구성
# ============================================================
Write-Host "=========================================="
Write-Host " [2/3] 프로세스별 배포 구성 중..."
Write-Host "=========================================="
foreach ($p in $Processes) {
    $procDir = Join-Path $PublishDir $p
    if (Test-Path $procDir) { Remove-Item -Recurse -Force $procDir }
    Copy-Item -Recurse $baseOut $procDir
    Copy-Item -Force (Join-Path $DeployDir "$p\appsettings.json") (Join-Path $procDir 'appsettings.json')
    New-Item -ItemType Directory -Force -Path (Join-Path $procDir 'logs') | Out-Null
    Write-Host "  $p -> $procDir"
}
Write-Host ""

# ============================================================
# 3. 프로세스 동시 실행
# ============================================================
Write-Host "=========================================="
Write-Host " [3/3] ACS 프로세스 실행 중..."
Write-Host "=========================================="
Write-Host ""
Write-Host "  프로세스 목록:"
Write-Host "  TS01_P (trans)   - API: 5103"
Write-Host "  ES01_P (ei)      - API: 5104"
Write-Host "  DS01_P (daemon)  - API: 5105"
Write-Host "  CS01_P (control) - API: 5102"
Write-Host "  HS01_P (host)    - API: 5101, TCP Listen: 3334, TCP Send: 3333"
Write-Host "  UI01_P (ui)      - API: 5100"
Write-Host ""
Write-Host "  실행 대상: $($Processes -join ' ')"
Write-Host "  종료: Ctrl+C"
Write-Host "=========================================="
Write-Host ""

$procs = New-Object System.Collections.Generic.List[System.Diagnostics.Process]
$subs  = New-Object System.Collections.Generic.List[object]

# stdout/stderr 라인을 prefix와 함께 콘솔에 출력하는 공통 핸들러
$onData = {
    $prefix = $Event.MessageData
    $line   = $EventArgs.Data
    if ($null -ne $line) {
        [Console]::Out.WriteLine("$prefix $line")
    }
}

foreach ($name in $Processes) {
    $procDir = Join-Path $PublishDir $name
    $color   = $Colors[$name]
    $prefix  = "${color}[${name}]${Reset}"

    $psi = New-Object System.Diagnostics.ProcessStartInfo
    $psi.FileName                  = 'dotnet'
    $psi.Arguments                 = 'ACS.App.dll'
    $psi.WorkingDirectory          = $procDir
    $psi.RedirectStandardOutput    = $true
    $psi.RedirectStandardError     = $true
    $psi.StandardOutputEncoding    = [System.Text.Encoding]::UTF8
    $psi.StandardErrorEncoding     = [System.Text.Encoding]::UTF8
    $psi.UseShellExecute           = $false
    $psi.CreateNoWindow            = $true

    $proc = New-Object System.Diagnostics.Process
    $proc.StartInfo            = $psi
    $proc.EnableRaisingEvents  = $true

    $sub1 = Register-ObjectEvent -InputObject $proc -EventName OutputDataReceived -MessageData $prefix -Action $onData
    $sub2 = Register-ObjectEvent -InputObject $proc -EventName ErrorDataReceived  -MessageData $prefix -Action $onData
    $subs.Add($sub1); $subs.Add($sub2)

    [void]$proc.Start()
    $proc.BeginOutputReadLine()
    $proc.BeginErrorReadLine()
    $procs.Add($proc)
    Write-Host "  $name 시작됨 (PID: $($proc.Id))"
}

Write-Host ""
Write-Host "모든 프로세스가 실행 중입니다. Ctrl+C로 종료하세요."
Write-Host ""

try {
    while ($true) {
        $alive = $false
        foreach ($p in $procs) {
            if (-not $p.HasExited) { $alive = $true; break }
        }
        if (-not $alive) { break }
        Start-Sleep -Milliseconds 500
    }
} finally {
    Write-Host ""
    Write-Host "=========================================="
    Write-Host " 모든 ACS 프로세스를 종료합니다..."
    Write-Host "=========================================="
    foreach ($p in $procs) {
        try {
            if (-not $p.HasExited) {
                & taskkill.exe /T /F /PID $p.Id 2>$null | Out-Null
                Write-Host "  PID $($p.Id) 종료됨"
            }
        } catch { }
    }
    foreach ($s in $subs) {
        try { Unregister-Event -SubscriptionId $s.Id -ErrorAction SilentlyContinue } catch { }
    }
    Write-Host "완료."
}
