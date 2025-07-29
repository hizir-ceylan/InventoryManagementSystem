# Inventory Management System - Teknik Dokümantasyon

## İçindekiler

1. [Sistem Genel Bakış](#sistem-genel-bakış)
2. [Mimari Yapı](#mimari-yapı)
3. [Veritabanı Yapısı](#veritabanı-yapısı)
4. [Kurulum Rehberi](#kurulum-rehberi)
5. [Build ve Deployment](#build-ve-deployment)
6. [Konfigürasyon](#konfigürasyon)
7. [API Dokümantasyonu](#api-dokümantasyonu)
8. [Windows Service Kurulumu](#windows-service-kurulumu)
9. [Docker Deployment](#docker-deployment)
10. [Geliştirici Rehberi](#geliştirici-rehberi)
11. [Sorun Giderme](#sorun-giderme)

---

## Sistem Genel Bakış

Inventory Management System, kurumsal cihaz envanteri yönetimi için geliştirilmiş .NET 8.0 tabanlı bir sistemdir. Sistem, agent tabanlı ve ağ keşfi yöntemlerini kullanarak cihaz bilgilerini toplar ve merkezi olarak yönetir.

### Temel Bileşenler

- **Inventory.Api**: RESTful Web API servisi
- **Inventory.Agent.Windows**: Windows için agent uygulaması
- **Inventory.Data**: Entity Framework Core veri katmanı
- **Inventory.Domain**: Domain modelleri ve iş mantığı
- **Inventory.Shared**: Paylaşılan sınıflar ve yardımcı fonksiyonlar

### Teknoloji Stack

- **.NET 8.0**: Ana framework
- **ASP.NET Core**: Web API framework
- **Entity Framework Core**: ORM
- **SQLite/SQL Server/PostgreSQL**: Veritabanı seçenekleri
- **Docker**: Konteyner teknolojisi
- **Swagger/OpenAPI**: API dokümantasyonu
- **WMI (Windows Management Instrumentation)**: Windows sistem bilgileri

---

## Mimari Yapı

### Clean Architecture Katmanları

```
┌─────────────────────────────────────────────────────────────┐
│                    Presentation Layer                       │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────┐ │
│  │ Web API         │  │ Windows Agent   │  │ Swagger UI  │ │
│  │ (Controllers)   │  │                 │  │             │ │
│  └─────────────────┘  └─────────────────┘  └─────────────┘ │
├─────────────────────────────────────────────────────────────┤
│                   Application Layer                         │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────┐ │
│  │ Services        │  │ Handlers        │  │ Validators  │ │
│  └─────────────────┘  └─────────────────┘  └─────────────┘ │
├─────────────────────────────────────────────────────────────┤
│                     Domain Layer                           │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────┐ │
│  │ Entities        │  │ Value Objects   │  │ Interfaces  │ │
│  └─────────────────┘  └─────────────────┘  └─────────────┘ │
├─────────────────────────────────────────────────────────────┤
│                  Infrastructure Layer                      │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────┐ │
│  │ Entity Framework│  │ Repositories    │  │ External    │ │
│  │ DbContext       │  │                 │  │ Services    │ │
│  └─────────────────┘  └─────────────────┘  └─────────────┘ │
└─────────────────────────────────────────────────────────────┘
```

### Veri Akışı

```
Agent/UI → API Controller → Service → Repository → Database
                ↓
            Change Logging → Log Files
                ↓
            Network Scan → Device Discovery
```

---

## Veritabanı Yapısı

### Ana Tablolar

#### Devices Tablosu
```sql
CREATE TABLE Devices (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    Name NVARCHAR(200) NOT NULL,
    MacAddress NVARCHAR(17),
    IpAddress NVARCHAR(15),
    DeviceType INT NOT NULL,
    Model NVARCHAR(200),
    Location NVARCHAR(200),
    Status INT NOT NULL,
    AgentInstalled BIT NOT NULL,
    ManagementType INT NOT NULL,
    DiscoveryMethod INT NOT NULL,
    LastSeen DATETIME2,
    
    -- Hardware Info (Owned Entity)
    HardwareInfo_Cpu NVARCHAR(200),
    HardwareInfo_Motherboard NVARCHAR(200),
    HardwareInfo_MotherboardSerial NVARCHAR(100),
    HardwareInfo_BiosManufacturer NVARCHAR(100),
    HardwareInfo_BiosVersion NVARCHAR(100),
    HardwareInfo_BiosSerial NVARCHAR(100),
    HardwareInfo_TotalRamGB INT,
    
    -- Software Info (Owned Entity)
    SoftwareInfo_OperatingSystem NVARCHAR(200),
    SoftwareInfo_OsVersion NVARCHAR(100),
    SoftwareInfo_OsArchitecture NVARCHAR(50),
    SoftwareInfo_RegisteredUser NVARCHAR(200),
    SoftwareInfo_SerialNumber NVARCHAR(100),
    SoftwareInfo_ActiveUser NVARCHAR(200)
);
```

#### ChangeLogs Tablosu
```sql
CREATE TABLE ChangeLogs (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    DeviceId UNIQUEIDENTIFIER,
    ChangeType NVARCHAR(100),
    OldValue NVARCHAR(MAX),
    NewValue NVARCHAR(MAX),
    ChangeDate DATETIME2 NOT NULL,
    ChangedBy NVARCHAR(200),
    
    FOREIGN KEY (DeviceId) REFERENCES Devices(Id)
);
```

#### Nested Collections (JSON veya ayrı tablolar)

**RAM Modülleri:**
```sql
CREATE TABLE DeviceRamModules (
    DeviceId UNIQUEIDENTIFIER,
    Slot NVARCHAR(50),
    SizeGB INT,
    SpeedMHz NVARCHAR(50),
    Manufacturer NVARCHAR(100),
    PartNumber NVARCHAR(100),
    SerialNumber NVARCHAR(100)
);
```

**Diskler:**
```sql
CREATE TABLE DeviceDisks (
    DeviceId UNIQUEIDENTIFIER,
    DeviceId NVARCHAR(200),
    SizeGB DECIMAL(18,2),
    FreeSpaceGB DECIMAL(18,2)
);
```

### Entity İlişkileri

```
Device (1) ←→ (Many) ChangeLog
Device (1) ←→ (1) HardwareInfo
Device (1) ←→ (1) SoftwareInfo
HardwareInfo (1) ←→ (Many) RamModule
HardwareInfo (1) ←→ (Many) Disk
HardwareInfo (1) ←→ (Many) Gpu
HardwareInfo (1) ←→ (Many) NetworkAdapter
```

### Enum Değerleri

```csharp
public enum DeviceType
{
    PC = 0,
    Laptop = 1,
    Server = 2,
    Printer = 3,
    Router = 4,
    Switch = 5,
    Unknown = 99
}

public enum DeviceStatus
{
    Active = 0,
    Inactive = 1,
    Maintenance = 2,
    Retired = 3
}

public enum ManagementType
{
    Agent = 0,
    Agentless = 1
}

public enum DiscoveryMethod
{
    Agent = 0,
    NetworkScan = 1,
    Manual = 2
}
```

---

## Kurulum Rehberi

### Sistem Gereksinimleri

**Minimum Gereksinimler:**
- İşletim Sistemi: Windows 10/11 veya Linux (Ubuntu 20.04+)
- .NET 8.0 Runtime
- RAM: 2GB (minimum), 4GB (önerilen)
- Disk: 500MB (uygulama) + veritabanı için ek alan
- Ağ: HTTP/HTTPS portları (varsayılan: 5093)

**Önerilen Gereksinimler:**
- RAM: 8GB+
- CPU: 4 Core+
- SSD depolama
- Dedicated SQL Server

### Adım 1: Repository'yi İndirme

```bash
git clone https://github.com/hizir-ceylan/InventoryManagementSystem.git
cd InventoryManagementSystem
```

### Adım 2: Gerekli Araçları Kurma

**Windows:**
```powershell
# .NET 8.0 SDK kurulumu
winget install Microsoft.DotNet.SDK.8

# Docker Desktop (isteğe bağlı)
winget install Docker.DockerDesktop
```

**Linux:**
```bash
# .NET 8.0 SDK kurulumu
wget https://packages.microsoft.com/config/ubuntu/20.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
sudo apt-get update
sudo apt-get install -y dotnet-sdk-8.0

# Docker kurulumu
sudo apt-get install -y docker.io docker-compose
```

### Adım 3: Veritabanı Kurulumu

**SQLite (Basit kurulum):**
```bash
# Otomatik olarak oluşturulur, ek kurulum gerekmez
```

**SQL Server:**
```bash
# Docker ile SQL Server
docker run -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=YourStrong@Password123" \
   -p 1433:1433 --name sql-server \
   -d mcr.microsoft.com/mssql/server:2022-latest
```

**PostgreSQL:**
```bash
# Docker ile PostgreSQL
docker run --name postgres-db \
   -e POSTGRES_PASSWORD=YourStrong@Password123 \
   -e POSTGRES_DB=inventorydb \
   -p 5432:5432 \
   -d postgres:15-alpine
```

---

## Build ve Deployment

### Development Build

```bash
# Solution'ı restore et
dotnet restore

# Tüm projeleri build et
dotnet build

# API'yi çalıştır
dotnet run --project Inventory.Api

# Windows Agent'ı çalıştır (Windows'ta)
dotnet run --project Inventory.Agent.Windows
```

### Production Build

```bash
# Release build
dotnet build --configuration Release

# Publish (self-contained)
dotnet publish Inventory.Api --configuration Release --self-contained --runtime win-x64 --output ./publish/api
dotnet publish Inventory.Agent.Windows --configuration Release --self-contained --runtime win-x64 --output ./publish/agent
```

### Automated Build Scripts

**Windows:**
```powershell
# Otomatik build ve kurulum
cd build-tools
.\Build-Setup.ps1
```

**Linux:**
```bash
# Docker build
./build-tools/build-and-deploy.sh

# Test build
./build-tools/test-build.sh
```

### Docker Build

```bash
# API için Docker image
docker build -t inventory-api:latest .

# Agent için Docker image  
docker build -f Dockerfile.agent -t inventory-agent:latest .

# Docker Compose ile tam build
docker-compose build
```

---

## Konfigürasyon

### appsettings.json (API)

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=./Data/inventory.db",
    "SqlServerConnection": "Server=localhost;Database=InventoryDB;Trusted_Connection=true;",
    "PostgreSqlConnection": "Host=localhost;Database=inventorydb;Username=inventoryuser;Password=password"
  },
  "DatabaseProvider": "SQLite", // SQLite, SqlServer, PostgreSQL
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    },
    "File": {
      "Path": "./Data/ApiLogs/api-{Date}.log",
      "LogLevel": "Information"
    }
  },
  "ApiSettings": {
    "EnableSwagger": true,
    "AllowedOrigins": ["http://localhost:3000", "https://yourdomain.com"],
    "MaxRequestSize": 104857600,
    "DefaultPageSize": 50,
    "MaxPageSize": 1000
  },
  "NetworkScan": {
    "DefaultTimeout": 5000,
    "MaxConcurrentScans": 50,
    "EnableNetworkDiscovery": true
  }
}
```

### Agent Configuration

```json
{
  "AgentSettings": {
    "ApiBaseUrl": "http://localhost:5093",
    "ScanIntervalMinutes": 30,
    "LogPath": "./Data/AgentLogs/",
    "EnableHardwareMonitoring": true,
    "EnableSoftwareMonitoring": true,
    "EnableChangeTracking": true
  },
  "WindowsAgent": {
    "UseWMI": true,
    "CollectInstalledSoftware": true,
    "CollectRunningProcesses": false,
    "CollectEventLogs": false
  }
}
```

### Docker Environment Variables

```bash
# .env dosyası
ASPNETCORE_ENVIRONMENT=Production
DB_CONNECTION_STRING=Server=sqlserver;Database=InventoryDB;User Id=sa;Password=YourStrong@Password123;
API_PORT=5093
DB_PASSWORD=YourStrong@Password123
CORS_ORIGINS=*
```

---

## Detaylı Konfigürasyon Rehberi

### Agent Servis Konfigürasyonu

#### Veri Toplama Aralığı (30 Dakika)

Agent servis **varsayılan olarak her 30 dakikada bir** sistem envanterini toplar ve API'ye gönderir.

**Kodda Tanımlandığı Yer:**
```csharp
// Dosya: Inventory.Agent.Windows/Services/InventoryAgentService.cs
private readonly int _inventoryIntervalMinutes = 30; // Her 30 dakikada bir
```

**Nasıl Değiştirilir:**

1. **Kod Değişikliği (Kalıcı):**
   ```csharp
   // InventoryAgentService.cs dosyasında 18. satırı düzenleyin
   private readonly int _inventoryIntervalMinutes = 60; // 60 dakikada bir için
   ```

2. **Environment Variable ile (Önerilen):**
   ```bash
   # Windows
   set AgentSettings__ScanIntervalMinutes=60
   
   # Linux/PowerShell
   export AgentSettings__ScanIntervalMinutes=60
   ```

3. **Service Konfigürasyonu ile:**
   ```json
   // appsettings.json dosyasına ekleyin
   {
     "AgentSettings": {
       "ScanIntervalMinutes": 60
     }
   }
   ```

#### Yerel Veri Depolama Konumları

**Agent Logları:**
```bash
# Windows
C:\Users\[Kullanıcı]\Documents\InventoryManagementSystem\LocalLogs\
%USERPROFILE%\Documents\InventoryManagementSystem\LocalLogs\

# Linux
~/Documents/InventoryManagementSystem/LocalLogs/
```

**Offline Storage (Bağlantı Kesildiğinde):**
```bash
# Windows
C:\Users\[Kullanıcı]\Documents\InventoryManagementSystem\OfflineStorage\
%USERPROFILE%\Documents\InventoryManagementSystem\OfflineStorage\

# Linux
~/Documents/InventoryManagementSystem/OfflineStorage/
```

**API Logları:**
```bash
# Development
./Data/ApiLogs/api-{Date}.log

# Service Mode (Windows)
C:\InventoryManagement\Logs\api-{Date}.log

# Docker
/app/ApiLogs/api-{Date}.log
```

**Veritabanı (SQLite):**
```bash
# Development/Local
./Data/inventory.db

# Docker
/app/Data/inventory.db
```

### Port Konfigürasyonu

#### API Port Değiştirme

**1. Development Ortamı:**
```bash
# launchSettings.json dosyasını düzenleyin
"Inventory.Api": {
  "applicationUrl": "http://localhost:5094;https://localhost:7094"
}
```

**2. Production/Service Modu:**
```bash
# appsettings.Production.json
{
  "Urls": "http://localhost:5094"
}
```

**3. Docker Ortamı:**
```yaml
# docker-compose.yml
services:
  inventory-api:
    ports:
      - "5094:5093"  # Host:Container
    environment:
      - ASPNETCORE_URLS=http://+:5093
```

**4. Environment Variable:**
```bash
set ASPNETCORE_URLS=http://localhost:5094
```

#### Agent API Bağlantı Adresi

**Environment Variable ile:**
```bash
set ApiSettings__BaseUrl=http://localhost:5094
```

**appsettings.json ile:**
```json
{
  "AgentSettings": {
    "ApiBaseUrl": "http://localhost:5094"
  }
}
```

### Özelleştirilebilir Özellikler

#### Agent Konfigürasyon Seçenekleri

**Dosya:** `Inventory.Agent.Windows/Configuration/ApiSettings.cs`

| Ayar | Varsayılan | Açıklama | Environment Variable |
|------|------------|----------|----------------------|
| BaseUrl | http://localhost:5093 | API sunucu adresi | `ApiSettings__BaseUrl` |
| Timeout | 30 saniye | HTTP timeout süresi | `ApiSettings__Timeout` |
| RetryCount | 3 | Başarısız istekler için yeniden deneme | `ApiSettings__RetryCount` |
| EnableOfflineStorage | true | Bağlantı kesildiğinde offline saklama | `ApiSettings__EnableOfflineStorage` |
| BatchUploadInterval | 300 saniye | Offline verilerin toplu gönderim aralığı | `ApiSettings__BatchUploadInterval` |
| MaxOfflineRecords | 10000 | Maksimum offline kayıt sayısı | `ApiSettings__MaxOfflineRecords` |

**Örnek Kullanım:**
```bash
# Windows CMD
set ApiSettings__BaseUrl=http://192.168.1.100:5093
set ApiSettings__Timeout=60
set ApiSettings__RetryCount=5
set ApiSettings__BatchUploadInterval=600

# PowerShell
$env:ApiSettings__BaseUrl="http://192.168.1.100:5093"
$env:ApiSettings__Timeout="60"

# Linux
export ApiSettings__BaseUrl=http://192.168.1.100:5093
export ApiSettings__Timeout=60
```

#### API Konfigürasyon Seçenekleri

**Dosya:** `Inventory.Api/appsettings.json`

| Ayar | Varsayılan | Açıklama |
|------|------------|----------|
| DatabaseProvider | SQLite | Veritabanı türü (SQLite/SqlServer/PostgreSQL) |
| DefaultPageSize | 50 | API sayfalama varsayılan boyutu |
| MaxPageSize | 1000 | API sayfalama maksimum boyutu |
| EnableSwagger | true | Swagger UI aktif/pasif |
| NetworkScan.DefaultTimeout | 5000ms | Ağ tarama timeout süresi |
| NetworkScan.MaxConcurrentScans | 50 | Eşzamanlı ağ tarama sayısı |

#### Logging Konfigürasyonu

**API Logging Seviyesi:**
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore": "Error"
    },
    "File": {
      "Enabled": true,
      "RetentionHours": 48,
      "LogLevel": {
        "Default": "Information"
      }
    }
  }
}
```

**Agent Logging:**
```bash
# Service için Windows Event Log
# Uygulama: "InventoryManagementAgent"
# Log: "Application"

# File logging
# Konum: %ProgramData%\Inventory Management System\Logs\
```

#### Network Scanning Konfigürasyonu

**Dosya:** `Inventory.Agent.Windows/appsettings.json`

```json
{
  "NetworkScan": {
    "Enabled": true,
    "Interval": "01:00:00",
    "NetworkRange": "192.168.1.0/24",
    "Timeout": 5000,
    "MaxConcurrentScans": 50
  }
}
```

**Environment Variables:**
```bash
set NetworkScan__Enabled=true
set NetworkScan__Interval=02:00:00
set NetworkScan__NetworkRange=10.0.0.0/24
```

#### Windows Service Konfigürasyonu

**Service Özellikleri:**
- **Service Adı:** InventoryManagementAgent
- **Başlatma Türü:** Automatic
- **Bağımlılık:** InventoryManagementApi
- **Recovery:** 3 yeniden başlatma denemesi

**Service Parametreleri:**
```bash
# Service kurulum parametreleri
sc create "InventoryManagementAgent" binpath="C:\InventoryManagement\Agent\Inventory.Agent.Windows.exe" start=auto depend="InventoryManagementApi"
sc description "InventoryManagementAgent" "Inventory Management System Windows Agent - 30 dakikada bir sistem envanteri toplar"
```

#### Docker Konfigürasyon Örnekleri

**Çevre Değişkenleri ile:**
```yaml
environment:
  - ApiSettings__BaseUrl=http://inventory-api:5093
  - ApiSettings__Timeout=60
  - ApiSettings__BatchUploadInterval=300
  - Logging__LogLevel__Default=Debug
```

**Volume Mount'lar:**
```yaml
volumes:
  - ./agent-data:/app/Data
  - ./agent-logs:/app/Logs
  - ./offline-storage:/app/OfflineStorage
```

### Performans ve Güvenlik Optimizasyonları

#### Memory ve CPU Limitleri (Docker)

```yaml
deploy:
  resources:
    limits:
      memory: 512M
      cpus: '0.5'
    reservations:
      memory: 256M
      cpus: '0.25'
```

#### Güvenlik Ayarları

**HTTPS Konfigürasyonu:**
```json
{
  "Urls": "https://localhost:5093",
  "Kestrel": {
    "Endpoints": {
      "Https": {
        "Url": "https://localhost:5093",
        "Certificate": {
          "Path": "certificates/aspnetapp.pfx",
          "Password": "YourCertPassword"
        }
      }
    }
  }
}
```

**CORS Konfigürasyonu:**
```json
{
  "ApiSettings": {
    "AllowedOrigins": [
      "http://localhost:3000",
      "https://yourdomain.com",
      "https://inventory.yourcompany.com"
    ]
  }
}
```

### Build ve Deploy Araçları Konfigürasyonu

#### Otomatik Kurulum Scriptleri

**Windows Otomatik Kurulum:**
```powershell
# build-tools/Build-Setup.ps1
# Bu script şunları yapar:
# 1. Projeleri build eder
# 2. Windows Service'leri kurar
# 3. Konfigürasyon dosyalarını kopyalar
# 4. Service'leri başlatır

.\build-tools\Build-Setup.ps1
```

**Linux Quick Start:**
```bash
# build-tools/quick-start.sh
# SQLite ile hızlı başlatma
./build-tools/quick-start.sh

# build-tools/build-and-deploy.sh  
# Docker ile production build
./build-tools/build-and-deploy.sh
```

#### Geliştirici Test Araçları

**Build Test:**
```bash
# Tüm projeleri test build yapar
./build-tools/test-build.sh
```

**Docker Test:**
```bash
# Docker container'ları test eder
./build-tools/test-docker.sh
```

**Logging Test:**
```bash
# Log dosyalarını ve konfigürasyonu test eder
./build-tools/test-logging.sh
```

#### Development Launch Profiles

**Visual Studio / VS Code için:**
```json
// launchSettings.json
{
  "profiles": {
    "http": {
      "applicationUrl": "http://localhost:5093",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    },
    "https": {
      "applicationUrl": "https://localhost:7296;http://localhost:5093",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
  }
}
```

#### Advanced Konfigürasyon Örnekleri

**Multi-Environment Setup:**
```bash
# Production
set ASPNETCORE_ENVIRONMENT=Production
set ApiSettings__BaseUrl=https://api.yourcompany.com:5093
set ConnectionStrings__DefaultConnection=Server=prod-server;Database=InventoryDB;...

# Staging  
set ASPNETCORE_ENVIRONMENT=Staging
set ApiSettings__BaseUrl=https://staging-api.yourcompany.com:5093
set ConnectionStrings__DefaultConnection=Server=staging-server;Database=InventoryDB;...

# Development
set ASPNETCORE_ENVIRONMENT=Development
set ApiSettings__BaseUrl=http://localhost:5093
set ConnectionStrings__DefaultConnection=Data Source=./Data/inventory-dev.db
```

**Network Infrastructure Konfigürasyonu:**
```bash
# Büyük ağ ortamları için
set NetworkScan__MaxConcurrentScans=100
set NetworkScan__DefaultTimeout=10000
set NetworkScan__NetworkRange=10.0.0.0/8,192.168.0.0/16

# Agent'ların farklı API sunucularına bağlanması
set ApiSettings__BaseUrl=http://inventory-api-cluster:5093
set ApiSettings__Timeout=60
set ApiSettings__RetryCount=5
```

#### Container Orchestration (Kubernetes/Docker Swarm)

**Kubernetes ConfigMap örneği:**
```yaml
apiVersion: v1
kind: ConfigMap
metadata:
  name: inventory-config
data:
  ASPNETCORE_ENVIRONMENT: "Production"
  ApiSettings__BaseUrl: "http://inventory-api-service:5093"
  ApiSettings__Timeout: "60"
  ConnectionStrings__DefaultConnection: "Server=sqlserver-service;Database=InventoryDB;..."
```

**Docker Swarm secrets:**
```bash
# Database password'ü secret olarak sakla
echo "YourStrong@Password123" | docker secret create db_password -

# Service'e secret'i bağla
docker service create \
  --name inventory-api \
  --secret db_password \
  --env DB_PASSWORD_FILE=/run/secrets/db_password \
  inventory-api:latest
```

### Sorun Giderme ve Monitoring

#### Log Dosyası Konumları ve İçerikleri

**Agent Service Logları:**
```bash
# Windows Event Log
Get-EventLog -LogName Application -Source "InventoryManagementAgent" -After (Get-Date).AddHours(-1)

# File Log (Service mode)
type "%ProgramData%\Inventory Management System\Logs\agent-*.log"

# File Log (User mode)  
type "%USERPROFILE%\Documents\InventoryManagementSystem\LocalLogs\agent-*.log"
```

**API Service Logları:**
```bash
# Development
tail -f ./Data/ApiLogs/api-$(date +%Y%m%d).log

# Production (Docker)
docker logs -f inventory-api

# Production (Windows Service)
type "C:\InventoryManagement\Logs\api-*.log"
```

#### Performance Monitoring Queries

**Veritabanı Performance:**
```sql
-- En son güncellenen cihazlar
SELECT Name, LastSeen, 
       DATEDIFF(minute, LastSeen, GETDATE()) as MinutesAgo
FROM Devices 
ORDER BY LastSeen DESC;

-- Offline cihazlar (30 dakikadan fazla güncellenmemiş)
SELECT Name, IpAddress, LastSeen
FROM Devices 
WHERE LastSeen < DATEADD(minute, -35, GETDATE())
ORDER BY LastSeen;

-- Değişiklik istatistikleri
SELECT COUNT(*) as TotalChanges, 
       CAST(ChangeDate as DATE) as Date
FROM ChangeLogs 
GROUP BY CAST(ChangeDate as DATE)
ORDER BY Date DESC;
```

#### Sistem Gereksinimleri ve Optimizasyon

**Minimum Sistem Kaynakları:**
```bash
# Agent için (her cihazda)
RAM: 128MB
CPU: %5 (tarama sırasında %20)
Disk: 50MB (loglar için +100MB)
Network: 1Mbps upload

# API Server için
RAM: 2GB (1000 cihaz için 4GB)
CPU: 2 core (1000+ cihaz için 4+ core)  
Disk: 500MB + 1MB/cihaz (SQLite), 10GB+ (SQL Server)
Network: 10Mbps (1000 cihaz için 100Mbps)
```

**Performans Optimizasyonu:**
```json
// API appsettings.json
{
  "ApiSettings": {
    "DefaultPageSize": 100,
    "MaxPageSize": 500,
    "EnableCaching": true,
    "CacheExpirationMinutes": 30
  },
  "ConnectionStrings": {
    "DefaultConnection": "Server=...;Command Timeout=60;Connection Timeout=30;"
  }
}
```

Bu kapsamlı konfigürasyon rehberi, sistem yöneticilerinin ve geliştiricilerin tüm özelleştirilebilir seçenekleri anlayarak sistemi kendi ihtiyaçlarına göre yapılandırabilmelerini sağlar.

---

## API Dokümantasyonu

### Base URL
```
http://localhost:5093/api
```

### Authentication
Şu anda basic authentication kullanılmaktadır. İleride JWT implementasyonu planlanmaktadır.

### Ana Endpoint'ler

#### Device Management

**GET /api/device**
- Tüm cihazları listeler
- Query parametreler: page, pageSize, search, deviceType, status

```bash
curl "http://localhost:5093/api/device?page=1&pageSize=10&search=PC"
```

**GET /api/device/{id}**
- Belirli bir cihazın detaylarını getirir

```bash
curl "http://localhost:5093/api/device/12345678-1234-1234-1234-123456789012"
```

**POST /api/device**
- Yeni cihaz ekler

```bash
curl -X POST "http://localhost:5093/api/device" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "TEST-PC-001",
    "macAddress": "00:1B:44:11:3A:B7",
    "ipAddress": "192.168.1.100",
    "deviceType": 0,
    "model": "Dell OptiPlex 7090",
    "location": "Office-101",
    "status": 0,
    "managementType": 0,
    "discoveryMethod": 2
  }'
```

**PUT /api/device/{id}**
- Mevcut cihazı günceller

**DELETE /api/device/{id}**
- Cihazı siler

#### Network Scanning

**POST /api/networkscan/start**
- Ağ taramayı başlatır

```bash
curl -X POST "http://localhost:5093/api/networkscan/start" \
  -H "Content-Type: application/json" \
  -d '{
    "networkRange": "192.168.1.0/24",
    "timeout": 5000,
    "includeHostnames": true
  }'
```

**GET /api/networkscan/status/{scanId}**
- Tarama durumunu kontrol eder

#### Change Logging

**GET /api/changelog**
- Değişiklik loglarını listeler

**GET /api/changelog/device/{deviceId}**
- Belirli cihazın değişiklik geçmişini getirir

### Response Formatları

**Başarılı Response:**
```json
{
  "success": true,
  "data": { ... },
  "message": "İşlem başarıyla tamamlandı",
  "timestamp": "2024-01-01T10:00:00Z"
}
```

**Hata Response:**
```json
{
  "success": false,
  "error": {
    "code": "VALIDATION_ERROR",
    "message": "Gerekli alanlar eksik",
    "details": { ... }
  },
  "timestamp": "2024-01-01T10:00:00Z"
}
```

### Swagger UI
Detaylı API dokümantasyonu için: `http://localhost:5093/swagger`

---

## Windows Service Kurulumu

### Otomatik Kurulum (Önerilen)

```powershell
# Yönetici olarak PowerShell açın
cd build-tools
.\Build-Setup.ps1
```

Bu script:
1. Projeyi build eder
2. Setup dosyası oluşturur
3. Windows Service'lerini kaydeder
4. Servisleri başlatır

### Manuel Kurulum

#### Adım 1: Service Dosyalarını Hazırlama

```powershell
# API'yi publish et
dotnet publish Inventory.Api -c Release -o "C:\InventoryManagement\API"

# Agent'ı publish et
dotnet publish Inventory.Agent.Windows -c Release -o "C:\InventoryManagement\Agent"
```

#### Adım 2: Windows Service Kaydı

```powershell
# API Service
sc create "InventoryManagementApi" binpath="C:\InventoryManagement\API\Inventory.Api.exe" start=auto
sc description "InventoryManagementApi" "Inventory Management System API Service"

# Agent Service
sc create "InventoryManagementAgent" binpath="C:\InventoryManagement\Agent\Inventory.Agent.Windows.exe" start=auto depend="InventoryManagementApi"
sc description "InventoryManagementAgent" "Inventory Management System Windows Agent"
```

#### Adım 3: Servisleri Başlatma

```powershell
# API'yi önce başlat
Start-Service -Name "InventoryManagementApi"

# Sonra Agent'ı başlat
Start-Service -Name "InventoryManagementAgent"
```

### Service Yönetimi

```powershell
# Servis durumunu kontrol et
Get-Service -Name "InventoryManagement*"

# Servisleri durdur
Stop-Service -Name "InventoryManagementAgent"
Stop-Service -Name "InventoryManagementApi"

# Servisleri yeniden başlat
Restart-Service -Name "InventoryManagementApi"
Restart-Service -Name "InventoryManagementAgent"

# Event loglarını kontrol et
Get-EventLog -LogName Application -Source "InventoryManagementAgent" -Newest 10
```

### Service Configuration

Service'ler için özel konfigürasyon dosyaları:

**API Service Config (appsettings.Production.json):**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.;Database=InventoryDB;Trusted_Connection=true;"
  },
  "Urls": "http://localhost:5093",
  "Logging": {
    "File": {
      "Path": "C:\\InventoryManagement\\Logs\\api-{Date}.log"
    }
  }
}
```

---

## Docker Deployment

### Basit Deployment (SQLite)

```bash
# Basit konfigürasyon ile başlat
docker-compose -f docker-compose.simple.yml up -d

# Logları takip et
docker-compose -f docker-compose.simple.yml logs -f
```

### Production Deployment (SQL Server)

```bash
# Tam production setup
docker-compose up -d

# Database migration çalıştır
docker-compose exec inventory-api dotnet ef database update

# Health check
curl http://localhost:5093/api/device
```

### Docker Images

**API Image Build:**
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 5093

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["Inventory.Api/Inventory.Api.csproj", "Inventory.Api/"]
COPY ["Inventory.Data/Inventory.Data.csproj", "Inventory.Data/"]
COPY ["Inventory.Domain/Inventory.Domain.csproj", "Inventory.Domain/"]
COPY ["Inventory.Shared/Inventory.Shared.csproj", "Inventory.Shared/"]
RUN dotnet restore "Inventory.Api/Inventory.Api.csproj"

COPY . .
WORKDIR "/src/Inventory.Api"
RUN dotnet build "Inventory.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Inventory.Api.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Inventory.Api.dll"]
```

### Container Management

```bash
# Container'ları listele
docker ps

# Logs'ları görüntüle
docker logs inventory-api

# Container'a bağlan
docker exec -it inventory-api bash

# Resource kullanımını kontrol et
docker stats

# Volume'ları kontrol et
docker volume ls
```

### Production Optimizations

**docker-compose.prod.yml:**
```yaml
version: '3.8'
services:
  inventory-api:
    build: .
    restart: unless-stopped
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
    deploy:
      resources:
        limits:
          memory: 1G
          cpus: '0.5'
        reservations:
          memory: 512M
          cpus: '0.25'
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:5093/health"]
      interval: 30s
      timeout: 10s
      retries: 3
      start_period: 40s
```

---

## Geliştirici Rehberi

### Development Environment Setup

```bash
# Geliştirme ortamını hazırla
git clone https://github.com/hizir-ceylan/InventoryManagementSystem.git
cd InventoryManagementSystem

# Dependencies yükle
dotnet restore

# Database migrations çalıştır
dotnet ef database update --project Inventory.Data --startup-project Inventory.Api

# Development server başlat
dotnet run --project Inventory.Api --environment Development
```

### Hot Reload Development

```bash
# API ile hot reload
dotnet watch run --project Inventory.Api

# Agent ile hot reload (Windows)
dotnet watch run --project Inventory.Agent.Windows
```

### Testing

```bash
# Unit testleri çalıştır
dotnet test

# Integration testleri çalıştır
dotnet test --filter Category=Integration

# Coverage raporu oluştur
dotnet test --collect:"XPlat Code Coverage"
```

### Database Migrations

```bash
# Yeni migration oluştur
dotnet ef migrations add NewMigrationName --project Inventory.Data --startup-project Inventory.Api

# Database güncelle
dotnet ef database update --project Inventory.Data --startup-project Inventory.Api

# Migration scriptini oluştur
dotnet ef migrations script --project Inventory.Data --startup-project Inventory.Api
```

### Code Quality

```bash
# Format code
dotnet format

# Static analysis
dotnet build --verbosity normal

# Security scan
dotnet list package --vulnerable
```

### Debugging

**Visual Studio / VS Code:**
- `F5` ile debug mode başlat
- Breakpoint'ler koy
- Watch window kullan

**CLI Debugging:**
```bash
# Verbose logging ile çalıştır
dotnet run --project Inventory.Api --environment Development --verbosity detailed

# Process'e attach et
dotnet attach <process-id>
```

---

## Sorun Giderme

### Yaygın Problemler

#### 1. API Başlamıyor

**Belirti:** API servisi başlamıyor veya port hatası
**Çözüm:**
```bash
# Port kullanımını kontrol et
netstat -an | findstr :5093

# Alternatif port kullan
dotnet run --project Inventory.Api --urls "http://localhost:5094"
```

#### 2. Database Connection Hatası

**Belirti:** "Cannot connect to database" hatası
**Çözüm:**
```bash
# Connection string kontrol et
# SQLite için
ls -la ./Data/
# SQL Server için
sqlcmd -S localhost -U sa -P YourPassword

# Migration çalıştır
dotnet ef database update --project Inventory.Data --startup-project Inventory.Api
```

#### 3. Windows Agent Çalışmıyor

**Belirti:** Agent bilgi göndermiyor
**Çözüm:**
```powershell
# Event logs kontrol et
Get-EventLog -LogName Application -Source "InventoryManagementAgent" -Newest 10

# API erişimini test et
Test-NetConnection -ComputerName localhost -Port 5093

# Agent'ı manuel çalıştır
dotnet run --project Inventory.Agent.Windows --environment Development
```

#### 4. Docker Container Sorunları

**Belirti:** Container başlamıyor veya çalışmıyor
**Çözüm:**
```bash
# Container logs'a bak
docker logs inventory-api

# Container içine gir
docker exec -it inventory-api bash

# Health check yap
docker inspect inventory-api

# Restart container
docker restart inventory-api
```

### Log Analizi

**API Logs:**
```bash
# Windows
type "Data\ApiLogs\api-$(Get-Date -Format 'yyyyMMdd').log"

# Linux
cat Data/ApiLogs/api-$(date +%Y%m%d).log

# Real-time monitoring
tail -f Data/ApiLogs/api-$(date +%Y%m%d).log
```

**Agent Logs:**
```bash
# Windows Agent logs
type "Data\AgentLogs\agent-$(Get-Date -Format 'yyyyMMdd').log"

# Change logs
ls Data/AgentLogs/Changes/
```

### Performance Monitoring

```bash
# API performance
curl -w "@curl-format.txt" -o /dev/null -s "http://localhost:5093/api/device"

# Database performance
sqlite3 Data/inventory.db ".timer on" "SELECT COUNT(*) FROM Devices;"

# Memory usage
docker stats inventory-api
```

### Network Troubleshooting

```bash
# API erişim testi
curl -v http://localhost:5093/api/device

# Network scan testi
nmap -sn 192.168.1.0/24

# Port forwarding kontrol (Docker)
docker port inventory-api
```

---

## Appendix

### Useful Commands Cheat Sheet

```bash
# Build Commands
dotnet restore
dotnet build
dotnet run --project Inventory.Api
dotnet publish -c Release

# Docker Commands
docker-compose up -d
docker-compose logs -f
docker ps
docker exec -it container_name bash

# Database Commands
dotnet ef migrations add MigrationName --project Inventory.Data
dotnet ef database update --project Inventory.Data

# Service Commands (Windows)
sc start InventoryManagementApi
sc stop InventoryManagementApi
Get-Service InventoryManagement*

# Network Commands
netstat -an | findstr :5093
nmap -sn 192.168.1.0/24
curl http://localhost:5093/api/device
```

### Configuration Templates

**Development appsettings.json:**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=./Data/inventory-dev.db"
  },
  "DatabaseProvider": "SQLite",
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft": "Information"
    }
  }
}
```

**Production appsettings.json:**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=prod-server;Database=InventoryDB;Trusted_Connection=true;"
  },
  "DatabaseProvider": "SqlServer",
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning"
    }
  }
}
```

Bu dokümantasyon, Inventory Management System'in tüm teknik yönlerini kapsamaktadır. Daha detaylı bilgi için kaynak kodları ve ilgili dosyaları inceleyebilirsiniz.