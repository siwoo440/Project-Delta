@echo off
setlocal
cd /d "%~dp0"

powershell.exe -NoProfile -ExecutionPolicy Bypass ^
  -File "%~dp0Fix-Day93-BattleCompile.ps1" ^
  -ProjectRoot "%~dp0"

endlocal
