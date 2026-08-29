@echo off
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0Apply113.ps1"
if errorlevel 1 (
  echo.
  echo Patch failed.
  pause
  exit /b 1
)
echo.
echo Patch complete.
pause
