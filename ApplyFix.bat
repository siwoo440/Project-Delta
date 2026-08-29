@echo off
REM 현재 폴더를 프로젝트 루트로 사용
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0ApplyFix.ps1" "%cd%"
REM 실행 결과 확인
pause
