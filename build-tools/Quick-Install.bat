@echo off
REM Inventory Management System - Quick Installer
REM This batch file downloads and runs the PowerShell installer

echo ============================================
echo Inventory Management System - Quick Installer
echo ============================================

REM Check if running as administrator
net session >nul 2>&1
if %errorLevel% == 0 (
    echo Running as Administrator - OK
) else (
    echo This installer requires Administrator privileges.
    echo Please right-click this file and select "Run as administrator"
    pause
    exit /b 1
)

echo.
echo Downloading installer script...

REM Create temp directory
if not exist "%TEMP%\InventoryInstall" mkdir "%TEMP%\InventoryInstall"

REM Download the installer script using PowerShell
powershell -Command "& { [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12; Invoke-WebRequest -Uri 'https://raw.githubusercontent.com/hizir-ceylan/InventoryManagementSystem/master/build-tools/Install-InventorySystem.ps1' -OutFile '%TEMP%\InventoryInstall\Install-InventorySystem.ps1' -UseBasicParsing }"

if %errorLevel% neq 0 (
    echo Failed to download installer script.
    echo Please check your internet connection and try again.
    pause
    exit /b 1
)

echo Download completed!
echo.
echo Starting installation...
echo.

REM Run the PowerShell installer
powershell -ExecutionPolicy Bypass -File "%TEMP%\InventoryInstall\Install-InventorySystem.ps1"

if %errorLevel% neq 0 (
    echo Installation failed. Check the log file for details.
    pause
    exit /b 1
)

echo.
echo Installation completed successfully!
echo.
echo You can access the system at: http://localhost:5093/swagger
echo.

pause