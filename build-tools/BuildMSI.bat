@echo off
REM Quick MSI Build Script
REM Builds the MSI package using WiX Toolset

echo ===================================================
echo Inventory Management System - Quick MSI Build
echo ===================================================

REM Check if running as administrator
net session >nul 2>&1
if %errorLevel% NEQ 0 (
    echo WARNING: Running without administrator privileges
    echo Some operations may fail
    echo.
)

REM Run the PowerShell build script
echo Running MSI build script...
powershell -ExecutionPolicy Bypass -File "Build-MSI.ps1"

if %errorLevel% EQ 0 (
    echo.
    echo ===================================================
    echo MSI BUILD COMPLETED SUCCESSFULLY
    echo ===================================================
    echo.
    echo Check the Setup\MSI\ folder for the generated MSI
    echo Check the Setup\Enterprise\ folder for deployment package
) else (
    echo.
    echo ===================================================
    echo MSI BUILD FAILED
    echo ===================================================
    echo.
    echo Please check the error messages above
)

pause