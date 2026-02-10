@echo off
REM ML-Agents Training Script for Windows
REM Usage: train.bat [run-id]
REM Example: train.bat MyFirstRun

echo =====================================
echo ML-Agents Training Launcher
echo =====================================
echo.

REM Set environment variable for protobuf compatibility
set PROTOCOL_BUFFERS_PYTHON_IMPLEMENTATION=python

REM Check if run-id was provided
if "%~1"=="" (
    echo ERROR: No run-id provided!
    echo Usage: train.bat [run-id]
    echo Example: train.bat MyFirstRun
    pause
    exit /b 1
)

echo Run ID: %~1
echo Config: mlagents-config/PlayerMLAgent_config.yaml
echo.
echo Starting ML-Agents trainer...
echo Press Play in Unity when you see "Listening on port 5004"
echo.

REM Start training
mlagents-learn mlagents-config/PlayerMLAgent_config.yaml --run-id=%~1

echo.
echo =====================================
echo Training session ended
echo =====================================
pause