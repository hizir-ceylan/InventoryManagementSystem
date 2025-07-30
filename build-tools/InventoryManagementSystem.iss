; Inventory Management System - Windows Installer Script
; This script creates a setup.exe that installs both API and Agent services

#define MyAppName "Inventory Management System"
#define MyAppVersion "1.0.1"
#define MyAppPublisher "Hizir Ceylan"
#define MyAppURL "https://github.com/hizir-ceylan/InventoryManagementSystem"
#define MyAppExeName "Inventory.Api.exe"
#define MyAgentExeName "Inventory.Agent.Windows.exe"
; Note: On Windows, these will have .exe extension, on Linux they won't

[Setup]
; Application information
AppId={{8B9B2A1D-4F5E-4C3B-8A1D-2E3F4A5B6C7D}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
AllowNoIcons=yes
LicenseFile=
OutputDir=Setup
OutputBaseFilename=InventoryManagementSystem-Setup
SetupIconFile=
Compression=lzma
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin

; System requirements
MinVersion=6.1sp1
ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "turkish"; MessagesFile: "compiler:Languages\Turkish.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "quicklaunchicon"; Description: "{cm:CreateQuickLaunchIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked; OnlyBelowVersion: 6.1

[Files]
; API Files
Source: "Published\Api\*"; DestDir: "{app}\Api"; Flags: ignoreversion recursesubdirs createallsubdirs
; Agent Files  
Source: "Published\Agent\*"; DestDir: "{app}\Agent"; Flags: ignoreversion recursesubdirs createallsubdirs
; Configuration files
Source: "Published\Api\appsettings.json"; DestDir: "{app}\Api"; Flags: ignoreversion
Source: "Published\Agent\appsettings.json"; DestDir: "{app}\Agent"; Flags: ignoreversion; AfterInstall: CreateAgentConfig
; Documentation
Source: "..\README.md"; DestDir: "{app}"; Flags: ignoreversion

[Dirs]
Name: "{app}\Data"
Name: "{app}\Logs"
Name: "{app}\Data\OfflineStorage"
Name: "{commonappdata}\Inventory Management System"
Name: "{commonappdata}\Inventory Management System\Data"
Name: "{commonappdata}\Inventory Management System\Logs"
Name: "{commonappdata}\Inventory Management System\OfflineStorage"

[Icons]
Name: "{group}\{#MyAppName} API (Swagger)"; Filename: "http://localhost:5093/swagger"; IconFilename: "{sys}\shell32.dll"; IconIndex: 14
Name: "{group}\{#MyAppName} Folder"; Filename: "{app}"; IconFilename: "{sys}\shell32.dll"; IconIndex: 3
Name: "{group}\Servis Yönetimi"; Filename: "{app}\ServiceManagement.bat"; IconFilename: "{sys}\shell32.dll"; IconIndex: 15; WorkingDir: "{app}"; Comment: "Servisleri yönetici yetkisi ile yönet"
Name: "{group}\Services Manager"; Filename: "services.msc"; IconFilename: "{sys}\shell32.dll"; IconIndex: 15
Name: "{group}\Event Viewer"; Filename: "eventvwr.msc"; IconFilename: "{sys}\shell32.dll"; IconIndex: 15
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "http://localhost:5093/swagger"; Tasks: desktopicon; IconFilename: "{sys}\shell32.dll"; IconIndex: 14
Name: "{userappdata}\Microsoft\Internet Explorer\Quick Launch\{#MyAppName}"; Filename: "http://localhost:5093/swagger"; Tasks: quicklaunchicon; IconFilename: "{sys}\shell32.dll"; IconIndex: 14

[Run]
; Start services after installation with improved timing
Filename: "{sys}\sc.exe"; Parameters: "start InventoryManagementApi"; Flags: runhidden; StatusMsg: "Starting API service..."
Filename: "{sys}\timeout.exe"; Parameters: "/t 15 /nobreak"; Flags: runhidden; StatusMsg: "Waiting for API to initialize..."
Filename: "{sys}\sc.exe"; Parameters: "start InventoryManagementAgent"; Flags: runhidden; StatusMsg: "Starting Agent service..."
Filename: "{sys}\timeout.exe"; Parameters: "/t 5 /nobreak"; Flags: runhidden; StatusMsg: "Waiting for Agent to initialize..."
; Open swagger in browser
Filename: "http://localhost:5093/swagger"; Description: "{cm:LaunchProgram,API Documentation (Swagger)}"; Flags: nowait postinstall skipifsilent shellexec
; Service management aracını yönetici olarak çalıştır
Filename: "{app}\ServiceManagement.bat"; Description: "Servis Yönetim Aracını Aç (Yönetici Yetkisi ile)"; Flags: nowait postinstall skipifsilent runascurrentuser; Verb: "runas"

[UninstallRun]
; Stop and remove services before uninstall
Filename: "{sys}\sc.exe"; Parameters: "stop InventoryManagementAgent"; Flags: runhidden
Filename: "{sys}\sc.exe"; Parameters: "stop InventoryManagementApi"; Flags: runhidden
Filename: "{sys}\timeout.exe"; Parameters: "/t 5 /nobreak"; Flags: runhidden
Filename: "{sys}\sc.exe"; Parameters: "delete InventoryManagementAgent"; Flags: runhidden
Filename: "{sys}\sc.exe"; Parameters: "delete InventoryManagementApi"; Flags: runhidden

[Code]
var
  DotNetInstallNeeded: Boolean;
  
function InitializeSetup(): Boolean;
var
  DotNetVersion: String;
  ResultCode: Integer;
begin
  Result := True;
  DotNetInstallNeeded := False;
  
  // Check if .NET 8 Runtime is installed
  if not Exec('dotnet', '--version', '', SW_HIDE, ewWaitUntilTerminated, ResultCode) then
  begin
    MsgBox('Microsoft .NET 8 Runtime is required but not found on this system.' + #13#10 + 
           'The installer will now download and install .NET 8 Runtime.', mbInformation, MB_OK);
    DotNetInstallNeeded := True;
  end
  else
  begin
    // Check .NET version
    if not Exec('dotnet', '--version', '', SW_HIDE, ewWaitUntilTerminated, ResultCode) then
    begin
      DotNetInstallNeeded := True;
    end;
  end;
end;

function NextButtonClick(CurPageID: Integer): Boolean;
var
  ResultCode: Integer;
  DownloadPage: TDownloadWizardPage;
  TempFile: String;
begin
  Result := True;
  
  if (CurPageID = wpReady) and DotNetInstallNeeded then
  begin
    // Download and install .NET 8 Runtime
    MsgBox('Downloading Microsoft .NET 8 Runtime...', mbInformation, MB_OK);
    
    TempFile := ExpandConstant('{tmp}\dotnet-runtime-8.0-win-x64.exe');
    
    if not Exec('powershell', 
        '-Command "Invoke-WebRequest -Uri ''https://download.microsoft.com/download/8/4/8/848d28d2-5910-4fb8-9cbb-250283c79824/dotnet-runtime-8.0.11-win-x64.exe'' -OutFile ''' + TempFile + '''"',
        '', SW_HIDE, ewWaitUntilTerminated, ResultCode) then
    begin
      MsgBox('Failed to download .NET Runtime. Please install it manually from https://dotnet.microsoft.com/download/dotnet/8.0', mbError, MB_OK);
      Result := False;
      Exit;
    end;
    
    if Exec(TempFile, '/quiet', '', SW_HIDE, ewWaitUntilTerminated, ResultCode) then
    begin
      MsgBox('.NET Runtime installed successfully!', mbInformation, MB_OK);
    end
    else
    begin
      MsgBox('Failed to install .NET Runtime. Please install it manually from https://dotnet.microsoft.com/download/dotnet/8.0', mbError, MB_OK);
      Result := False;
    end;
  end;
end;

procedure CreateAgentConfig();
var
  ConfigContent: String;
  ConfigFile: String;
begin
  ConfigFile := ExpandConstant('{app}\Agent\appsettings.json');
  
  ConfigContent := '{' + #13#10 +
    '  "ApiSettings": {' + #13#10 +
    '    "BaseUrl": "http://localhost:5093",' + #13#10 +
    '    "EnableOfflineStorage": true,' + #13#10 +
    '    "OfflineStoragePath": "' + ExpandConstant('{commonappdata}\Inventory Management System\OfflineStorage') + '"' + #13#10 +
    '  },' + #13#10 +
    '  "Logging": {' + #13#10 +
    '    "LogLevel": {' + #13#10 +
    '      "Default": "Information",' + #13#10 +
    '      "Microsoft.Hosting.Lifetime": "Information"' + #13#10 +
    '    }' + #13#10 +
    '  }' + #13#10 +
    '}';
    
  SaveStringToFile(ConfigFile, ConfigContent, False);
end;

procedure CurStepChanged(CurStep: TSetupStep);
var
  ResultCode: Integer;
  ServiceCreated: Boolean;
begin
  if CurStep = ssPostInstall then
  begin
    // Create Windows Services with improved error handling
    
    // Stop existing services if they exist
    Exec('sc', 'stop InventoryManagementAgent', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
    Exec('sc', 'stop InventoryManagementApi', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
    
    // Wait a moment for services to fully stop
    Exec('timeout', '/t 3 /nobreak', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
    
    Exec('sc', 'delete InventoryManagementAgent', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
    Exec('sc', 'delete InventoryManagementApi', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
    
    // Create API Service with better configuration
    ServiceCreated := Exec('sc', 'create InventoryManagementApi binPath= "' + ExpandConstant('{app}\Api\{#MyAppExeName}') + '" start= auto DisplayName= "Inventory Management API" obj= LocalSystem', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
    
    if ServiceCreated and (ResultCode = 0) then
    begin
      // Configure API service for reliability
      Exec('sc', 'config InventoryManagementApi start= auto', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
      Exec('sc', 'failure InventoryManagementApi reset= 86400 actions= restart/5000/restart/5000/restart/5000', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
      Exec('sc', 'config InventoryManagementApi depend= ', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
    end
    else
    begin
      MsgBox('Failed to create API service. Error code: ' + IntToStr(ResultCode) + '. You may need to create it manually.', mbError, MB_OK);
    end;
    
    // Create Agent Service with dependency on API and improved settings
    ServiceCreated := Exec('sc', 'create InventoryManagementAgent binPath= "' + ExpandConstant('{app}\Agent\{#MyAgentExeName}') + ' --service" start= auto DisplayName= "Inventory Management Agent" obj= LocalSystem depend= InventoryManagementApi', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
    
    if ServiceCreated and (ResultCode = 0) then
    begin
      // Configure Agent service for reliability and delayed start
      Exec('sc', 'config InventoryManagementAgent start= delayed-auto', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
      Exec('sc', 'failure InventoryManagementAgent reset= 86400 actions= restart/10000/restart/10000/restart/10000', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
      
      // Set service description
      Exec('sc', 'description InventoryManagementAgent "Inventory Management System Agent - Collects and reports system inventory data"', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
      Exec('sc', 'description InventoryManagementApi "Inventory Management System API - Web API for inventory management"', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
    end
    else
    begin
      MsgBox('Failed to create Agent service. Error code: ' + IntToStr(ResultCode) + '. You may need to create it manually.', mbError, MB_OK);
    end;
    
    // Create Event Log sources (ignore errors if they already exist)
    Exec('reg', 'add "HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\EventLog\Application\InventoryManagementAgent" /v "EventMessageFile" /t REG_EXPAND_SZ /d "%SystemRoot%\System32\EventLogMessages.dll" /f', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
    Exec('reg', 'add "HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\EventLog\Application\InventoryManagementApi" /v "EventMessageFile" /t REG_EXPAND_SZ /d "%SystemRoot%\System32\EventLogMessages.dll" /f', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
    
    // Configure firewall rule for API
    Exec('netsh', 'advfirewall firewall delete rule name="Inventory Management API"', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
    Exec('netsh', 'advfirewall firewall add rule name="Inventory Management API" dir=in action=allow protocol=TCP localport=5093', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
    
    // Set environment variables for persistent storage
    Exec('setx', 'ApiSettings__BaseUrl "http://localhost:5093" /M', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
    Exec('setx', 'ApiSettings__EnableOfflineStorage "true" /M', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
    Exec('setx', 'ApiSettings__OfflineStoragePath "' + ExpandConstant('{commonappdata}\Inventory Management System\OfflineStorage') + '" /M', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
    Exec('setx', 'ConnectionStrings__DefaultConnection "Data Source=' + ExpandConstant('{commonappdata}\Inventory Management System\Data\inventory.db') + '" /M', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
    Exec('setx', 'INVENTORY_DATA_PATH "' + ExpandConstant('{commonappdata}\Inventory Management System\Data') + '" /M', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
    Exec('setx', 'INVENTORY_LOG_PATH "' + ExpandConstant('{commonappdata}\Inventory Management System\Logs') + '" /M', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
    
    // Servis yönetimi için batch dosyası oluştur (yönetici yetkisi kontrolü ile)
    SaveStringToFile(ExpandConstant('{app}\ServiceManagement.bat'), 
      '@echo off' + #13#10 +
      'REM Yönetici yetkisi kontrolü' + #13#10 +
      'net session >nul 2>&1' + #13#10 +
      'if %errorLevel% == 0 (' + #13#10 +
      '    goto :admin' + #13#10 +
      ') else (' + #13#10 +
      '    echo Bu program yönetici yetkisi gerektirir.' + #13#10 +
      '    echo Yönetici olarak yeniden başlatılıyor...' + #13#10 +
      '    powershell -Command "Start-Process ''%~f0'' -Verb RunAs"' + #13#10 +
      '    exit /b' + #13#10 +
      ')' + #13#10 +
      ':admin' + #13#10 +
      'title Envanter Yönetim Sistemi - Servis Yönetimi' + #13#10 +
      ':main' + #13#10 +
      'cls' + #13#10 +
      'echo ================================================' + #13#10 +
      'echo   Envanter Yönetim Sistemi - Servis Yönetimi' + #13#10 +
      'echo ================================================' + #13#10 +
      'echo.' + #13#10 +
      'echo 1. Servisleri Başlat' + #13#10 +
      'echo 2. Servisleri Durdur' + #13#10 +
      'echo 3. Servisleri Yeniden Başlat' + #13#10 +
      'echo 4. Servis Durumunu Kontrol Et' + #13#10 +
      'echo 5. Agent Loglarını Görüntüle' + #13#10 +
      'echo 6. Çıkış' + #13#10 +
      'echo.' + #13#10 +
      'set /p choice=Bir seçenek seçin (1-6): ' + #13#10 +
      'if "%choice%"=="1" goto start' + #13#10 +
      'if "%choice%"=="2" goto stop' + #13#10 +
      'if "%choice%"=="3" goto restart' + #13#10 +
      'if "%choice%"=="4" goto status' + #13#10 +
      'if "%choice%"=="5" goto logs' + #13#10 +
      'if "%choice%"=="6" goto exit' + #13#10 +
      'echo Geçersiz seçim. Lütfen 1-6 arasında bir rakam girin.' + #13#10 +
      'pause' + #13#10 +
      'goto main' + #13#10 +
      ':start' + #13#10 +
      'echo Servisler başlatılıyor...' + #13#10 +
      'net start InventoryManagementApi' + #13#10 +
      'timeout /t 5 /nobreak >nul' + #13#10 +
      'net start InventoryManagementAgent' + #13#10 +
      'goto end' + #13#10 +
      ':stop' + #13#10 +
      'echo Servisler durduruluyor...' + #13#10 +
      'net stop InventoryManagementAgent' + #13#10 +
      'net stop InventoryManagementApi' + #13#10 +
      'goto end' + #13#10 +
      ':restart' + #13#10 +
      'echo Servisler yeniden başlatılıyor...' + #13#10 +
      'net stop InventoryManagementAgent' + #13#10 +
      'net stop InventoryManagementApi' + #13#10 +
      'timeout /t 3 /nobreak >nul' + #13#10 +
      'net start InventoryManagementApi' + #13#10 +
      'timeout /t 5 /nobreak >nul' + #13#10 +
      'net start InventoryManagementAgent' + #13#10 +
      'goto end' + #13#10 +
      ':status' + #13#10 +
      'echo Servis Durumu:' + #13#10 +
      'echo ===============' + #13#10 +
      'sc query InventoryManagementApi' + #13#10 +
      'echo.' + #13#10 +
      'sc query InventoryManagementAgent' + #13#10 +
      'goto end' + #13#10 +
      ':logs' + #13#10 +
      'echo Log klasörü açılıyor...' + #13#10 +
      'explorer "' + ExpandConstant('{commonappdata}\Inventory Management System\Logs') + '"' + #13#10 +
      'goto end' + #13#10 +
      ':end' + #13#10 +
      'echo.' + #13#10 +
      'pause' + #13#10 +
      'goto main' + #13#10 +
      ':exit' + #13#10, False);
  end;
end;

[CustomMessages]
english.LaunchProgram=Launch %1
turkish.LaunchProgram=%1'i Başlat