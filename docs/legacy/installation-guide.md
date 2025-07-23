# Inventory Management System - Kurulum Rehberi

## İçindekiler
1. [Sistem Gereksinimleri](#sistem-gereksinimleri)
2. [Sunucu Kurulumu](#sunucu-kurulumu)
3. [Veritabanı Kurulumu](#veritabanı-kurulumu)
4. [API Kurulumu](#api-kurulumu)
5. [Agent Kurulumu](#agent-kurulumu)
6. [Test ve Doğrulama](#test-ve-doğrulama)
7. [Sorun Giderme](#sorun-giderme)

## Sistem Gereksinimleri

### Sunucu Gereksinimleri
- **İşletim Sistemi**: Windows Server 2019/2022 veya Ubuntu 20.04/22.04
- **RAM**: Minimum 4GB, Önerilen 8GB
- **Disk**: Minimum 20GB boş alan
- **Network**: İnternet bağlantısı ve internal network erişimi

### İstemci Gereksinimleri
- **İşletim Sistemi**: Windows 10/11, Windows Server 2016+
- **RAM**: Minimum 2GB
- **Disk**: Minimum 500MB boş alan
- **.NET Runtime**: .NET 8.0 Runtime (otomatik kurulacak)

### Yazılım Gereksinimleri
- **.NET 8.0 SDK** (geliştirme için)
- **.NET 8.0 Runtime** (çalıştırma için)
- **SQL Server 2019+** veya **PostgreSQL 13+** veya **SQLite** (test için)
- **IIS 10+** (Windows üzerinde production için)

## Sunucu Kurulumu

### 1. .NET 8.0 Runtime Kurulumu

#### Windows:
```powershell
# PowerShell'i yönetici olarak çalıştırın
# .NET 8.0 Runtime'ı indirin ve kurun
Invoke-WebRequest -Uri "https://download.microsoft.com/download/3/a/b/3ab97e7f-8c83-4930-8e1e-eeebba5e5f9a/dotnet-runtime-8.0.0-win-x64.exe" -OutFile "dotnet-runtime-8.0.0-win-x64.exe"
.\dotnet-runtime-8.0.0-win-x64.exe /S
```

#### Ubuntu:
```bash
# Ubuntu paket deposunu güncelle
sudo apt update

# Microsoft paket deposunu ekle
wget https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
sudo apt update

# .NET 8.0 Runtime kurulumu
sudo apt install -y dotnet-runtime-8.0 aspnetcore-runtime-8.0
```

### 2. Proje Dosyalarını İndirme

```bash
# Git ile projeyi klonlayın
git clone https://github.com/hizir-ceylan/InventoryManagementSystem.git
cd InventoryManagementSystem

# Veya ZIP dosyasını indirip açın
wget https://github.com/hizir-ceylan/InventoryManagementSystem/archive/refs/heads/main.zip
unzip main.zip
cd InventoryManagementSystem-main
```

## Veritabanı Kurulumu

### Seçenek 1: SQL Server (Önerilen - Production)

#### Windows'ta SQL Server Express Kurulumu:
```powershell
# SQL Server Express 2022 indirin ve kurun
Invoke-WebRequest -Uri "https://go.microsoft.com/fwlink/p/?linkid=2216019" -OutFile "SQLEXPR_x64_ENU.exe"
.\SQLEXPR_x64_ENU.exe /Q /ACTION=Install /FEATURES=SQL /INSTANCENAME=SQLEXPRESS /TCPENABLED=1 /SECURITYMODE=SQL /SAPWD="StrongPassword123!"
```

#### Ubuntu'da SQL Server 2022 Kurulumu:
```bash
# Microsoft SQL Server 2022 repository ekle
curl -fsSL https://packages.microsoft.com/keys/microsoft.asc | sudo gpg --dearmor -o /usr/share/keyrings/microsoft-prod.gpg
echo "deb [arch=amd64,armhf,arm64 signed-by=/usr/share/keyrings/microsoft-prod.gpg] https://packages.microsoft.com/ubuntu/22.04/mssql-server-2022 jammy main" | sudo tee /etc/apt/sources.list.d/mssql-server-2022.list

# SQL Server kurulumu
sudo apt update
sudo apt install -y mssql-server

# SQL Server konfigürasyonu
sudo /opt/mssql/bin/mssql-conf setup
```

#### Veritabanı Oluşturma:
```sql
-- SQL Server Management Studio veya sqlcmd ile bağlanın
-- Connection String: Server=localhost\SQLEXPRESS;Database=InventoryDB;Trusted_Connection=true;

CREATE DATABASE InventoryDB;
GO

USE InventoryDB;
GO

-- Devices tablosu
CREATE TABLE Devices (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    Name NVARCHAR(200) NOT NULL,
    MacAddress NVARCHAR(17),
    IpAddress NVARCHAR(15),
    DeviceType NVARCHAR(50),
    Model NVARCHAR(200),
    Location NVARCHAR(200),
    Status INT DEFAULT 0,
    CreatedDate DATETIME2 DEFAULT GETUTCDATE(),
    UpdatedDate DATETIME2 DEFAULT GETUTCDATE()
);

-- DeviceHardwareInfo tablosu
CREATE TABLE DeviceHardwareInfo (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    DeviceId UNIQUEIDENTIFIER FOREIGN KEY REFERENCES Devices(Id),
    Cpu NVARCHAR(200),
    CpuCores INT,
    CpuLogical INT,
    CpuClockMHz INT,
    Motherboard NVARCHAR(200),
    MotherboardSerial NVARCHAR(100),
    BiosManufacturer NVARCHAR(100),
    BiosVersion NVARCHAR(100),
    BiosSerial NVARCHAR(100),
    RamGB INT,
    DiskGB INT,
    CreatedDate DATETIME2 DEFAULT GETUTCDATE()
);

-- DeviceSoftwareInfo tablosu
CREATE TABLE DeviceSoftwareInfo (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    DeviceId UNIQUEIDENTIFIER FOREIGN KEY REFERENCES Devices(Id),
    OperatingSystem NVARCHAR(200),
    OsVersion NVARCHAR(100),
    OsArchitecture NVARCHAR(50),
    RegisteredUser NVARCHAR(200),
    SerialNumber NVARCHAR(100),
    ActiveUser NVARCHAR(200),
    CreatedDate DATETIME2 DEFAULT GETUTCDATE()
);

-- ChangeLogs tablosu
CREATE TABLE ChangeLogs (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    DeviceId UNIQUEIDENTIFIER FOREIGN KEY REFERENCES Devices(Id),
    ChangeDate DATETIME2 DEFAULT GETUTCDATE(),
    ChangeType NVARCHAR(100),
    OldValue NVARCHAR(MAX),
    NewValue NVARCHAR(MAX),
    ChangedBy NVARCHAR(200)
);

-- Indexes
CREATE INDEX IX_Devices_MacAddress ON Devices(MacAddress);
CREATE INDEX IX_Devices_IpAddress ON Devices(IpAddress);
CREATE INDEX IX_ChangeLogs_DeviceId ON ChangeLogs(DeviceId);
CREATE INDEX IX_ChangeLogs_ChangeDate ON ChangeLogs(ChangeDate);
```

### Seçenek 2: SQLite (Test/Geliştirme)
```bash
# SQLite kurulumu (Ubuntu)
sudo apt install -y sqlite3

# Veya Windows'ta SQLite Browser indirin
# https://sqlitebrowser.org/dl/
```

### Seçenek 3: PostgreSQL
```bash
# Ubuntu'da PostgreSQL kurulumu
sudo apt update
sudo apt install -y postgresql postgresql-contrib

# PostgreSQL konfigürasyonu
sudo -u postgres psql
CREATE DATABASE inventorydb;
CREATE USER inventoryuser WITH PASSWORD 'StrongPassword123!';
GRANT ALL PRIVILEGES ON DATABASE inventorydb TO inventoryuser;
\q
```

## API Kurulumu

### 1. API Projesini Build Etme

```bash
cd InventoryManagementSystem
dotnet restore
dotnet build Inventory.Api --configuration Release
```

### 2. Konfigürasyon Dosyasını Düzenleme

`Inventory.Api/appsettings.Production.json` dosyasını oluşturun:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "Microsoft.AspNetCore": "Warning"
    },
    "File": {
      "Enabled": true,
      "RetentionHours": 48,
      "LogLevel": {
        "Default": "Information"
      }
    }
  },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost\\SQLEXPRESS;Database=InventoryDB;Trusted_Connection=true;TrustServerCertificate=true;"
  },
  "NetworkScan": {
    "Enabled": true,
    "Interval": "01:00:00",
    "NetworkRange": "192.168.1.0/24"
  },
  "Agent": {
    "LoggingInterval": "01:00:00",
    "LogRetentionHours": 48,
    "EnableHourlyLogging": true
  },
  "ApiSettings": {
    "BaseUrl": "https://your-domain.com",
    "RequireHttps": true,
    "Port": 443
  }
}
```

### 3. IIS'te API Kurulumu (Windows)

#### IIS ve ASP.NET Core Hosting Bundle Kurulumu:
```powershell
# IIS özelliklerini etkinleştirin
Enable-WindowsOptionalFeature -Online -FeatureName IIS-WebServerRole,IIS-WebServer,IIS-CommonHttpFeatures,IIS-HttpErrors,IIS-HttpLogging,IIS-SecurityRole,IIS-RequestFiltering,IIS-StaticContent,IIS-NetFxExtensibility45,IIS-NetFx4ExtensibilityModule,IIS-ISAPIExtensions,IIS-ISAPIFilter,IIS-AspNetCoreModule,IIS-AspNetCoreModuleV2

# ASP.NET Core Hosting Bundle indirin ve kurun
Invoke-WebRequest -Uri "https://download.microsoft.com/download/3/a/b/3ab97e7f-8c83-4930-8e1e-eeebba5e5f9a/dotnet-hosting-8.0.0-win.exe" -OutFile "dotnet-hosting-8.0.0-win.exe"
.\dotnet-hosting-8.0.0-win.exe /S
```

#### API'yi Publish Etme:
```bash
cd Inventory.Api
dotnet publish --configuration Release --output C:\inetpub\wwwroot\InventoryAPI
```

#### IIS Site Oluşturma:
```powershell
Import-Module WebAdministration

# Application Pool oluştur
New-WebAppPool -Name "InventoryAPI" -Force
Set-ItemProperty -Path "IIS:\AppPools\InventoryAPI" -Name "managedRuntimeVersion" -Value ""
Set-ItemProperty -Path "IIS:\AppPools\InventoryAPI" -Name "enable32BitAppOnWin64" -Value $false

# Web Site oluştur
New-Website -Name "InventoryAPI" -Port 80 -PhysicalPath "C:\inetpub\wwwroot\InventoryAPI" -ApplicationPool "InventoryAPI"

# HTTPS için sertifika yapılandırması (opsiyonel)
# New-WebBinding -Name "InventoryAPI" -Protocol "https" -Port 443
```

### 4. Linux'ta API Kurulumu (Systemd Service)

#### API'yi Publish Etme:
```bash
cd Inventory.Api
dotnet publish --configuration Release --output /opt/inventoryapi
```

#### Systemd Service Oluşturma:
```bash
sudo nano /etc/systemd/system/inventoryapi.service
```

Service dosyası içeriği:
```ini
[Unit]
Description=Inventory Management System API
After=network.target

[Service]
Type=notify
User=www-data
WorkingDirectory=/opt/inventoryapi
ExecStart=/usr/bin/dotnet /opt/inventoryapi/Inventory.Api.dll
Restart=on-failure
RestartSec=5
SyslogIdentifier=inventoryapi
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=ASPNETCORE_URLS=http://0.0.0.0:5000

[Install]
WantedBy=multi-user.target
```

#### Service'i Başlatma:
```bash
sudo systemctl daemon-reload
sudo systemctl enable inventoryapi
sudo systemctl start inventoryapi
sudo systemctl status inventoryapi
```

### 5. Nginx Reverse Proxy (Linux)

```bash
sudo apt install -y nginx

sudo nano /etc/nginx/sites-available/inventoryapi
```

Nginx konfigürasyonu:
```nginx
server {
    listen 80;
    server_name your-domain.com;
    
    location / {
        proxy_pass http://localhost:5000;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection keep-alive;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_cache_bypass $http_upgrade;
    }
}
```

```bash
sudo ln -s /etc/nginx/sites-available/inventoryapi /etc/nginx/sites-enabled/
sudo nginx -t
sudo systemctl restart nginx
```

## Agent Kurulumu

### 1. Agent'ı Build Etme

```bash
cd Inventory.Agent.Windows
dotnet build --configuration Release
dotnet publish --configuration Release --output C:\InventoryAgent
```

### 2. Agent Konfigürasyonu

`C:\InventoryAgent\appsettings.json` dosyasını oluşturun:

```json
{
  "ApiSettings": {
    "BaseUrl": "https://your-api-server.com",
    "Timeout": 30,
    "RetryCount": 3
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    },
    "RetentionHours": 48,
    "EnableHourlyLogging": true
  },
  "Agent": {
    "ScanInterval": "01:00:00",
    "LoggingInterval": "01:00:00",
    "EnableNetworkDiscovery": false,
    "NetworkRange": "192.168.1.0/24"
  }
}
```

### 3. Windows Service Olarak Kurulum

#### Agent Service Wrapper Oluşturma:

`C:\InventoryAgent\install-service.bat` dosyasını oluşturun:
```batch
@echo off
sc create "InventoryAgent" binPath="C:\InventoryAgent\Inventory.Agent.Windows.exe" start=auto DisplayName="Inventory Management Agent"
sc description "InventoryAgent" "Collects hardware and software inventory information and sends to central API"
sc start "InventoryAgent"
pause
```

#### Service'i Manuel Çalıştırma:
```batch
# Yönetici olarak çalıştırın
C:\InventoryAgent\install-service.bat
```

#### Service'i Kaldırma:
```batch
sc stop "InventoryAgent"
sc delete "InventoryAgent"
```

### 4. Scheduled Task Olarak Kurulum (Alternatif)

```powershell
# PowerShell'i yönetici olarak çalıştırın

# Saatlik çalışacak scheduled task oluştur
$action = New-ScheduledTaskAction -Execute "C:\InventoryAgent\Inventory.Agent.Windows.exe"
$trigger = New-ScheduledTaskTrigger -RepetitionInterval (New-TimeSpan -Hours 1) -Once -At (Get-Date)
$settings = New-ScheduledTaskSettingsSet -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries -StartWhenAvailable
$principal = New-ScheduledTaskPrincipal -UserId "SYSTEM" -LogonType ServiceAccount -RunLevel Highest

Register-ScheduledTask -TaskName "InventoryAgent" -Action $action -Trigger $trigger -Settings $settings -Principal $principal -Description "Inventory Management System Agent"
```

### 5. Toplu Agent Dağıtımı

#### Group Policy ile Dağıtım (Domain Ortamı):

1. **Group Policy Management Console** açın
2. Yeni bir **GPO** oluşturun: "Inventory Agent Deployment"
3. **Computer Configuration > Policies > Software Settings > Software Installation** altında:
   - **New > Package** seçin
   - Agent MSI paketini seçin (eğer varsa)
4. **Computer Configuration > Windows Settings > Scripts > Startup** altında:
   - Agent kurulum scriptini ekleyin

#### PowerShell DSC ile Dağıtım:

```powershell
Configuration InventoryAgentInstall
{
    Node "localhost"
    {
        File InventoryAgentFolder
        {
            Ensure = "Present"
            Type = "Directory"
            DestinationPath = "C:\InventoryAgent"
        }
        
        File InventoryAgentFiles
        {
            Ensure = "Present"
            Type = "File"
            SourcePath = "\\server\share\InventoryAgent\"
            DestinationPath = "C:\InventoryAgent\"
            Recurse = $true
            DependsOn = "[File]InventoryAgentFolder"
        }
        
        Script InstallService
        {
            SetScript = {
                & "C:\InventoryAgent\install-service.bat"
            }
            TestScript = {
                $service = Get-Service -Name "InventoryAgent" -ErrorAction SilentlyContinue
                return ($service -ne $null)
            }
            GetScript = {
                return @{ Result = (Get-Service -Name "InventoryAgent" -ErrorAction SilentlyContinue) }
            }
            DependsOn = "[File]InventoryAgentFiles"
        }
    }
}
```

## Test ve Doğrulama

### 1. API Test

#### Swagger UI ile Test:
```
http://your-server/swagger
```

#### PowerShell ile Test:
```powershell
# API endpoint'lerini test et
$baseUrl = "http://your-server"

# Sağlık kontrolü
Invoke-RestMethod -Uri "$baseUrl/api/device" -Method GET

# Test cihazı ekleme
$device = @{
    Name = "Test-PC-001"
    MacAddress = "00:1B:44:11:3A:B7"
    IpAddress = "192.168.1.100"
    DeviceType = "PC"
    Model = "Dell OptiPlex"
    Location = "Office-101"
    Status = 0
} | ConvertTo-Json

Invoke-RestMethod -Uri "$baseUrl/api/device" -Method POST -Body $device -ContentType "application/json"
```

#### Curl ile Test:
```bash
# API durumunu kontrol et
curl -X GET "http://your-server/api/device" -H "accept: application/json"

# Test cihazı ekle
curl -X POST "http://your-server/api/device" \
  -H "accept: application/json" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Test-PC-001",
    "macAddress": "00:1B:44:11:3A:B7",
    "ipAddress": "192.168.1.100",
    "deviceType": "PC",
    "model": "Dell OptiPlex",
    "location": "Office-101",
    "status": 0
  }'
```

### 2. Agent Test

#### Manual Test:
```bash
# Agent'ı manuel çalıştır
cd C:\InventoryAgent
.\Inventory.Agent.Windows.exe
```

#### Log Kontrolü:
```bash
# Agent loglarını kontrol et
type C:\InventoryAgent\LocalLogs\device-log-2024-01-15-14.json
type C:\InventoryAgent\LocalLogs\centralized-log-2024-01-15-14.log
```

#### Service Durumu Kontrolü:
```powershell
Get-Service -Name "InventoryAgent"
Get-EventLog -LogName Application -Source "InventoryAgent" -Newest 10
```

### 3. Veritabanı Test

```sql
-- Cihaz kayıtlarını kontrol et
SELECT COUNT(*) FROM Devices;
SELECT TOP 10 * FROM Devices ORDER BY CreatedDate DESC;

-- Log kayıtlarını kontrol et
SELECT COUNT(*) FROM ChangeLogs;
SELECT TOP 10 * FROM ChangeLogs ORDER BY ChangeDate DESC;

-- En son aktiviteyi kontrol et
SELECT 
    d.Name,
    d.IpAddress,
    d.UpdatedDate,
    COUNT(cl.Id) as ChangeCount
FROM Devices d
LEFT JOIN ChangeLogs cl ON d.Id = cl.DeviceId
GROUP BY d.Id, d.Name, d.IpAddress, d.UpdatedDate
ORDER BY d.UpdatedDate DESC;
```

### 4. Network Connectivity Test

```powershell
# API bağlantısını test et
Test-NetConnection -ComputerName "your-server" -Port 80
Test-NetConnection -ComputerName "your-server" -Port 443

# DNS çözümlemesini test et
Resolve-DnsName "your-server"

# API endpoint'ine ping at
Invoke-WebRequest -Uri "http://your-server/api/device" -UseBasicParsing
```

## Sorun Giderme

### Genel Sorunlar

#### 1. API Erişim Sorunu
**Belirti**: Agent API'ye bağlanamıyor
**Çözüm**:
```bash
# Firewall kurallarını kontrol et
netsh advfirewall firewall show rule name="InventoryAPI"

# Port'un açık olduğunu kontrol et
netstat -an | findstr :80
netstat -an | findstr :443

# Windows Firewall kuralı ekle
netsh advfirewall firewall add rule name="InventoryAPI" dir=in action=allow protocol=TCP localport=80
```

#### 2. Veritabanı Bağlantı Sorunu
**Belirti**: API veritabanına bağlanamıyor
**Çözüm**:
```sql
-- SQL Server bağlantısını test et
sqlcmd -S localhost\SQLEXPRESS -E -Q "SELECT @@VERSION"

-- Connection string'i test et
sqlcmd -S "localhost\SQLEXPRESS" -d "InventoryDB" -E -Q "SELECT COUNT(*) FROM Devices"
```

#### 3. Agent Service Başlamıyor
**Belirti**: Windows Service başlamıyor
**Çözüm**:
```powershell
# Event Log'u kontrol et
Get-EventLog -LogName System -Source "Service Control Manager" | Where-Object {$_.InstanceId -eq 7034 -or $_.InstanceId -eq 7031}

# Agent'ı manuel çalıştırıp hata mesajlarını gör
C:\InventoryAgent\Inventory.Agent.Windows.exe

# .NET Runtime kurulu mu kontrol et
dotnet --version
```

#### 4. Log Dosyaları Oluşmuyor
**Belirti**: LocalLogs klasöründe dosya yok
**Çözüm**:
```powershell
# Klasör izinlerini kontrol et
icacls "C:\InventoryAgent\LocalLogs"

# İzin ver
icacls "C:\InventoryAgent\LocalLogs" /grant "NT AUTHORITY\SYSTEM:(OI)(CI)F"
icacls "C:\InventoryAgent\LocalLogs" /grant "BUILTIN\Administrators:(OI)(CI)F"
```

### Platform Spesifik Sorunlar

#### Windows'ta WMI Sorunları
```powershell
# WMI servisini yeniden başlat
net stop winmgmt
net start winmgmt

# WMI repository'yi onar
winmgmt /resetrepository
```

#### Linux'ta Permission Sorunları
```bash
# Service dosyası izinlerini düzelt
sudo chown root:root /etc/systemd/system/inventoryapi.service
sudo chmod 644 /etc/systemd/system/inventoryapi.service

# Uygulama dosyası izinlerini düzelt
sudo chown -R www-data:www-data /opt/inventoryapi
sudo chmod +x /opt/inventoryapi/Inventory.Api
```

### Performans Optimizasyonu

#### 1. Logging Optimizasyonu
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "Microsoft": "Warning",
      "Microsoft.Hosting.Lifetime": "Information"
    }
  }
}
```

#### 2. Memory Optimization
```json
{
  "Agent": {
    "MaxLogFileSize": "10MB",
    "LogRetentionHours": 24,
    "EnableDetailedLogging": false
  }
}
```

#### 3. Network Optimization
```json
{
  "ApiSettings": {
    "Timeout": 10,
    "RetryCount": 2,
    "BatchSize": 50
  }
}
```

## Güvenlik Önerileri

### 1. HTTPS Konfigürasyonu
```bash
# Let's Encrypt sertifikası alma (Linux)
sudo apt install certbot python3-certbot-nginx
sudo certbot --nginx -d your-domain.com
```

### 2. Firewall Konfigürasyonu
```bash
# Ubuntu UFW
sudo ufw allow 22/tcp
sudo ufw allow 80/tcp
sudo ufw allow 443/tcp
sudo ufw enable

# Windows Firewall
netsh advfirewall firewall add rule name="HTTP" dir=in action=allow protocol=TCP localport=80
netsh advfirewall firewall add rule name="HTTPS" dir=in action=allow protocol=TCP localport=443
```

### 3. Database Security
```sql
-- SQL Server güvenlik ayarları
ALTER LOGIN sa DISABLE;
CREATE LOGIN inventoryuser WITH PASSWORD = 'ComplexPassword123!';
CREATE USER inventoryuser FOR LOGIN inventoryuser;
ALTER ROLE db_datawriter ADD MEMBER inventoryuser;
ALTER ROLE db_datareader ADD MEMBER inventoryuser;
```

## Yedekleme ve Kurtarma

### 1. Veritabanı Yedekleme
```sql
-- SQL Server Full Backup
BACKUP DATABASE InventoryDB 
TO DISK = 'C:\Backup\InventoryDB_Full.bak'
WITH FORMAT, INIT, NAME = 'InventoryDB Full Backup';

-- Otomatik yedekleme için SQL Agent Job oluşturun
```

### 2. Konfigürasyon Yedekleme
```powershell
# Konfigürasyon dosyalarını yedekle
Copy-Item "C:\InventoryAgent\appsettings.json" "C:\Backup\appsettings.json.backup"
Copy-Item "C:\inetpub\wwwroot\InventoryAPI\appsettings.Production.json" "C:\Backup\appsettings.Production.json.backup"
```

Bu kurulum rehberi ile Inventory Management System'i başarıyla kurup çalıştırabilirsiniz. Herhangi bir sorunla karşılaştığınızda sorun giderme bölümünü inceleyin veya teknik destek ile iletişime geçin.