@echo off
setlocal
cd /d "%~dp0"

if not exist "Assets\ProjectDelta\Scripts\Editor\ProjectDelta.Editor.asmdef" (
    echo [ERROR] Project Delta project root was not detected.
    echo Extract this ZIP into the project root first.
    pause
    exit /b 1
)

if exist "Assets\ProjectDelta\Editor\ProjectDelta.Editor.asmdef" (
    del /f /q "Assets\ProjectDelta\Editor\ProjectDelta.Editor.asmdef"
)

if exist "Assets\ProjectDelta\Editor\ProjectDelta.Editor.asmdef.meta" (
    del /f /q "Assets\ProjectDelta\Editor\ProjectDelta.Editor.asmdef.meta"
)

if exist "Assets\ProjectDelta\Editor\ProjectDeltaDay44EncounterCanvasInstaller.cs" (
    del /f /q "Assets\ProjectDelta\Editor\ProjectDeltaDay44EncounterCanvasInstaller.cs"
)

if exist "Assets\ProjectDelta\Editor\ProjectDeltaDay44EncounterCanvasInstaller.cs.meta" (
    del /f /q "Assets\ProjectDelta\Editor\ProjectDeltaDay44EncounterCanvasInstaller.cs.meta"
)

if exist "Assets\ProjectDelta\Editor" (
    rmdir /q "Assets\ProjectDelta\Editor" 2>nul
)

if exist "Assets\ProjectDelta\Editor.meta" (
    del /f /q "Assets\ProjectDelta\Editor.meta"
)

echo.
echo [OK] Day44 duplicate ProjectDelta.Editor assembly files were removed.
echo [OK] Canvas Installer now uses Assets\ProjectDelta\Scripts\Editor.
echo.
pause
