@echo off
setlocal
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0scripts\Play.ps1" %*
set "icLaunchExit=%errorlevel%"
if not "%icLaunchExit%"=="0" (
    echo Nao foi possivel iniciar o jogo. Veja o erro acima.
    pause
)
exit /b %icLaunchExit%
