@echo off
setlocal enabledelayedexpansion
title ACS redeploy

rem ============================================================
rem  ACS redeploy batch (ASCII only - avoid codepage issues)
rem   1) run publish-deploy.ps1  (publish + mirror src/ACS/deploy/<SITE>/ + apphost rename)
rem   2) sync runtime folder D:\ACS\deploy  (appsettings.json / logs preserved)
rem
rem  Usage : double-click, or  redeploy.bat [publish-deploy.ps1 args]
rem          e.g. redeploy.bat -Sites TS01_P,ES01_P
rem  Note  : running processes keep their old DLLs locked - stop them first.
rem ============================================================

set "REPO=%~dp0"
set "RUNTIME=D:\ACS\deploy"
set "SITES=CS01_P DS01_P ES01_P HS01_P TS01_P"

echo ============================================================
echo  ACS redeploy  (repo: %REPO%)
echo ============================================================

rem -- 0) warn if site processes are running --
set "RUNNING="
for %%S in (%SITES%) do (
    tasklist /FI "IMAGENAME eq %%S.exe" 2>nul | find /I "%%S.exe" >nul && set "RUNNING=!RUNNING! %%S"
)
if not "!RUNNING!"=="" (
    echo.
    echo [WARN] running processes:!RUNNING!
    echo        their DLLs are locked and will be SKIPPED - old version remains.
    echo        stop them first, then redeploy. ^(recommended^)
    echo.
    choice /M "continue anyway"
    if errorlevel 2 exit /b 1
)

rem -- 1) publish + mirror repo deploy --
echo.
echo [1/2] running publish-deploy.ps1 ...
powershell -NoProfile -ExecutionPolicy Bypass -File "%REPO%publish-deploy.ps1" %*
if errorlevel 1 (
    echo.
    echo [FAIL] publish-deploy.ps1 error - runtime sync skipped.
    pause
    exit /b 1
)

rem -- 2) sync runtime folder (preserve per-site appsettings.json / logs) --
echo.
echo [2/2] syncing runtime: %RUNTIME% ...
if not exist "%RUNTIME%" (
    echo [INFO] %RUNTIME% not found - only repo deploy was updated.
    goto :done
)

set "FAILED="
for %%S in (%SITES%) do (
    if exist "%RUNTIME%\%%S" (
        echo   ==^> %RUNTIME%\%%S
        robocopy "%REPO%deploy\%%S" "%RUNTIME%\%%S" /E /XF appsettings.json /XD logs /R:1 /W:1 /NJH /NJS /NDL /NP >nul
        if errorlevel 8 set "FAILED=!FAILED! %%S"
    ) else (
        echo   [SKIP] %RUNTIME%\%%S not found
    )
)

if not "!FAILED!"=="" (
    echo.
    echo [FAIL] copy error for:!FAILED!  ^(check file locks^)
    pause
    exit /b 1
)

:done
echo.
echo redeploy complete - restart the site processes.
pause
exit /b 0
