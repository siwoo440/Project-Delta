@echo off
cd /d "%~dp0"
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Fix-Day70-CorruptedEncounter.ps1"
echo.
pause
