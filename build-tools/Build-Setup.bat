@echo off
REM Inventory Management System - Build and Package Script
REM Creates setup.exe installer for Windows deployment

echo =====================================================
echo Inventory Management System - Build and Package
echo =====================================================

REM Check if running as administrator (required for some operations)
net session >nul 2>&1
if %errorLevel% neq 0 (
    echo Warning: Not running as administrator. Some operations may fail.
    echo It's recommended to run this script as administrator.
    pause
)

REM Check for required tools
echo Checking for required tools...

REM Check .NET SDK
dotnet --version >nul 2>&1
if %errorLevel% neq 0 (
    echo ERROR: .NET SDK is required but not found!
    echo Please install .NET 8 SDK from: https://dotnet.microsoft.com/download/dotnet/8.0
    pause
    exit /b 1
)

echo .NET SDK found: 
dotnet --version

REM Check for Inno Setup (optional - will provide download instructions if not found)
where iscc >nul 2>&1
if %errorLevel% neq 0 (
    echo WARNING: Inno Setup not found in PATH.
    echo To create setup.exe, please install Inno Setup from: https://jrsoftware.org/isinfo.php
    echo After installation, add Inno Setup directory to your PATH or run the script manually.
    echo.
    echo Current script will build and prepare files, but won't create setup.exe
    set SKIP_SETUP_CREATION=1
) else (
    echo Inno Setup found.
    set SKIP_SETUP_CREATION=0
)

echo.

REM Clean previous builds
echo Cleaning previous builds...
if exist "Published" rmdir /s /q "Published"
if exist "Setup" rmdir /s /q "Setup"
mkdir "Published"
mkdir "Published\Api"
mkdir "Published\Agent"
mkdir "Setup"

REM Restore NuGet packages
echo Restoring NuGet packages...
dotnet restore ..
if %errorLevel% neq 0 (
    echo ERROR: Failed to restore NuGet packages!
    pause
    exit /b 1
)

REM Build solution
echo Building solution...
dotnet build .. --configuration Release --no-restore
if %errorLevel% neq 0 (
    echo ERROR: Build failed!
    pause
    exit /b 1
)

REM Publish API
echo Publishing API...
dotnet publish ..\Inventory.Api --configuration Release --output "Published\Api" --no-build --self-contained false
if %errorLevel% neq 0 (
    echo ERROR: Failed to publish API!
    pause
    exit /b 1
)

REM Publish Agent
echo Publishing Agent...
dotnet publish ..\Inventory.Agent.Windows --configuration Release --output "Published\Agent" --no-build --self-contained false
if %errorLevel% neq 0 (
    echo ERROR: Failed to publish Agent!
    pause
    exit /b 1
)

REM Create default configuration files
echo Creating configuration files...

REM API configuration
echo {> "Published\Api\appsettings.json"
echo   "ConnectionStrings": {>> "Published\Api\appsettings.json"
echo     "DefaultConnection": "Data Source=inventory.db">> "Published\Api\appsettings.json"
echo   },>> "Published\Api\appsettings.json"
echo   "Logging": {>> "Published\Api\appsettings.json"
echo     "LogLevel": {>> "Published\Api\appsettings.json"
echo       "Default": "Information",>> "Published\Api\appsettings.json"
echo       "Microsoft.AspNetCore": "Warning">> "Published\Api\appsettings.json"
echo     }>> "Published\Api\appsettings.json"
echo   },>> "Published\Api\appsettings.json"
echo   "AllowedHosts": "*",>> "Published\Api\appsettings.json"
echo   "Urls": "http://localhost:5093">> "Published\Api\appsettings.json"
echo }>> "Published\Api\appsettings.json"

REM Agent configuration  
echo {> "Published\Agent\appsettings.json"
echo   "ApiSettings": {>> "Published\Agent\appsettings.json"
echo     "BaseUrl": "http://localhost:5093",>> "Published\Agent\appsettings.json"
echo     "EnableOfflineStorage": true,>> "Published\Agent\appsettings.json"
echo     "OfflineStoragePath": "C:\\Program Files\\Inventory Management System\\Data\\OfflineStorage">> "Published\Agent\appsettings.json"
echo   },>> "Published\Agent\appsettings.json"
echo   "Logging": {>> "Published\Agent\appsettings.json"
echo     "LogLevel": {>> "Published\Agent\appsettings.json"
echo       "Default": "Information",>> "Published\Agent\appsettings.json"
echo       "Microsoft.Hosting.Lifetime": "Information">> "Published\Agent\appsettings.json"
echo     }>> "Published\Agent\appsettings.json"
echo   }>> "Published\Agent\appsettings.json"
echo }>> "Published\Agent\appsettings.json"

echo Configuration files created.

REM Create setup.exe if Inno Setup is available
if %SKIP_SETUP_CREATION%==1 (
    echo.
    echo =====================================================
    echo Build completed successfully!
    echo =====================================================
    echo.
    echo Published files are ready in 'Published' folder:
    echo   - API: Published\Api\
    echo   - Agent: Published\Agent\
    echo.
    echo To create setup.exe:
    echo 1. Install Inno Setup from: https://jrsoftware.org/isinfo.php
    echo 2. Add Inno Setup to your PATH, or
    echo 3. Run: "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" InventoryManagementSystem.iss
    echo.
    echo The setup script is ready: InventoryManagementSystem.iss
) else (
    echo Creating setup.exe with Inno Setup...
    iscc InventoryManagementSystem.iss
    if %errorLevel% neq 0 (
        echo ERROR: Failed to create setup.exe!
        echo The published files are still available in 'Published' folder.
        pause
        exit /b 1
    )
    
    echo.
    echo =====================================================
    echo Build and packaging completed successfully!
    echo =====================================================
    echo.
    echo Setup file created: Setup\InventoryManagementSystem-Setup.exe
    echo Published files: Published\Api\ and Published\Agent\
    echo.
    echo You can now distribute the setup.exe file to install the system on Windows machines.
)

echo.
echo Installation requirements for target machines:
echo   - Windows 10/11 or Windows Server 2016+
echo   - Administrator privileges for installation
echo   - .NET 8 Runtime (will be installed automatically if missing)
echo   - Port 5093 available (will be configured automatically)
echo.

pause