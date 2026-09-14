@echo off
cd /d "%~dp0"

where dotnet >nul 2>&1
if errorlevel 1 (
    echo .NET SDK not found. Install .NET 8 or newer and try again.
    pause
    exit /b 1
)

taskkill /F /IM Tasker.exe >nul 2>&1

dotnet build "Tasker\Tasker.csproj" -p:EnableWindowsTargeting=true
if errorlevel 1 (
    echo Build failed.
    pause
    exit /b 1
)

start "" "Tasker\bin\Debug\net8.0-windows\Tasker.exe"
