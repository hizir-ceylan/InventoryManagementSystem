# Inventory Management System - Kapsamlı Teknik Dokümantasyon

## İçindekiler

1. [Sistem Genel Bakış ve Mimari](#sistem-genel-bakış-ve-mimari)
2. [Ana Bileşenler ve Kod Yapısı](#ana-bileşenler-ve-kod-yapısı)
3. [Kurulum ve Deployment Seçenekleri](#kurulum-ve-deployment-seçenekleri)
4. [Konfigürasyon ve Özelleştirme](#konfigürasyon-ve-özelleştirme)
5. [API Dokümantasyonu ve Endpoint'ler](#api-dokümantasyonu-ve-endpointler)
6. [Veritabanı Yapısı ve Yönetimi](#veritabanı-yapısı-ve-yönetimi)
7. [Network ve Sunucu Konfigürasyonu](#network-ve-sunucu-konfigürasyonu)
8. [Geliştirici Rehberi ve Best Practices](#geliştirici-rehberi-ve-best-practices)
9. [Sorun Giderme ve Monitoring](#sorun-giderme-ve-monitoring)
10. [Güvenlik ve Production Optimizasyonları](#güvenlik-ve-production-optimizasyonları)

---

## Sistem Genel Bakış ve Mimari

### 🏗️ Sistem Mimarisi

Inventory Management System, modern .NET 8.0 tabanlı modüler bir envanter yönetim sistemidir. Sistem Clean Architecture prensiplerine göre tasarlanmış olup, üç ana bileşenden oluşur:

```
┌───────────────────────────────────────────────────────────────────┐
│                         PRESENTATION LAYER                        │
├──────────────────────┬──────────────────────┬─────────────────────┤
│        Web App       │      API Server      │    Windows Agent    │
│    (localhost:X)     │   (localhost:5093)   │ (Background Service)│
└──────────────────────┴──────────────────────┴─────────────────────┘
                                   ↓
┌───────────────────────────────────────────────────────────────────┐
│                         APPLICATION LAYER                         │
├──────────────────────┬──────────────────────┬─────────────────────┤
│       Services       │       Handlers       │      Validators     │
│    Business Logic    │   Request/Response   │   Input Validation  │
└──────────────────────┴──────────────────────┴─────────────────────┘
                                   ↓
┌───────────────────────────────────────────────────────────────────┐
│                           DOMAIN LAYER                            │
├──────────────────────┬──────────────────────┬─────────────────────┤
│       Entities       │     Value Objects    │  Domain Interfaces  │
│      Core Models     │    Business Rules    │      Contracts      │
└──────────────────────┴──────────────────────┴─────────────────────┘
                                   ↓
┌───────────────────────────────────────────────────────────────────┐
│                       INFRASTRUCTURE LAYER                        │
├──────────────────────┬──────────────────────┬─────────────────────┤
│        EF Core       │     Repositories     │  External Services  │
│       Database       │      Data Access     │ WMI, Network Scanner│
└──────────────────────┴──────────────────────┴─────────────────────┘


```

### 🔄 Veri Akışı ve İletişim

```
🔍 Agent (WMI) → 📊 System Info → 🌐 HTTP API → 🗄️ Database
                      ↓                ↓
               📝 Local Logs    🌐 Web App View
                      ↓                ↓  
               💾 Offline       📊 Reports & Analytics
                  Storage
```

### 🎯 Temel Özellikler

- **🔄 Real-time Monitoring**: 30 dakikada bir otomatik envanter toplama
- **🌐 Network Discovery**: IP aralığından cihaz keşfi (ARP, ICMP, SNMP)
- **💾 Offline Capability**: Internet bağlantısı olmadan da çalışma
- **🔒 Security**: JWT authentication, role-based authorization (gelecek sürüm)
- **📊 Multi-Database**: SQLite (dev), SQL Server/PostgreSQL (production)
- **🐳 Containerization**: Docker ve Kubernetes desteği
- **📱 Cross-Platform**: Windows, Linux server desteği

---

## Ana Bileşenler ve Kod Yapısı

### 🖥️ **Inventory.Api** - RESTful Web API Sunucusu

Sistemin kalbi olan merkezi API sunucusu. Tüm cihaz verilerini toplar, işler ve dağıtır.

#### 📁 **Controllers/** - API Endpoint Controllers
**Dosyalar ve İşlevleri:**
- `DeviceController.cs`: Cihaz CRUD işlemleri, envanter yönetimi
- `UpdateController.cs`: Windows/Office güncelleme takibi ve raporlama **[YENİ ÖZELLIK]**
- `NetworkScanController.cs`: Ağ tarama ve keşif işlemleri  
- `ChangeLogController.cs`: Değişiklik geçmişi ve audit logları
- `LocationController.cs`: Lokasyon bazlı cihaz gruplandırma
- `LoggingController.cs`: Sistem log görüntüleme ve filtreleme

**🆕 UpdateController.cs Yeni Özellikleri:**
- `GET /api/update/{deviceId}`: Basitleştirilmiş endpoint - gereksiz parametreler kaldırıldı
- `POST /api/update/scan/{deviceId}`: Güncelleme tarama başlatma
- `POST /api/update/report`: Agent'dan güncelleme raporu alma
- `GET /api/update/statistics`: Güncelleme istatistikleri

#### 📁 **Services/** - Business Logic Katmanı
**Ana Servisler:**
- `DeviceService.cs`: Cihaz iş mantığı, validation, rules
- `NetworkScanService.cs`: Ağ tarama algoritmaları, IP range işleme
- `ChangeTrackingService.cs`: Hardware değişiklik takibi
- `DataSyncService.cs`: Agent-API arası veri senkronizasyonu

#### 📁 **DTOs/** - Data Transfer Objects
**Veri Transfer Modelleri:**
- `DeviceDto.cs`: Cihaz bilgi transferi için optimize edilmiş model
- `NetworkScanDto.cs`: Ağ tarama sonuç modeli
- `HardwareInfoDto.cs`: Donanım bilgi aktarım modeli

#### ⚙️ **Konfigürasyon Dosyaları:**
```json
// appsettings.json - Temel konfigürasyon
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=./Data/inventory.db"
  },
  "DatabaseProvider": "SQLite", // SQLite|SqlServer|PostgreSQL
  "ApiSettings": {
    "BaseUrl": "http://localhost:5093",
    "EnableSwagger": true,
    "DefaultPageSize": 50,
    "MaxPageSize": 1000
  }
}
```

### 🔍 **Inventory.Agent.Windows** - Windows Agent Servisi

Her Windows bilgisayarına kurularak sistem bilgilerini otomatik toplayan servis.

#### 📁 **Services/** - Agent Background Services
**Temel Servisler:**
- `InventoryAgentService.cs`: Ana agent servisi, 30 dakikalık cycle
- `HardwareMonitoringService.cs`: Hardware değişiklik tespit sistemi
- `NetworkReportingService.cs`: Network connectivity ve status reporting
- `OfflineStorageService.cs`: API erişimi olmadığında yerel veri saklama

#### 📁 **Models/** - Agent Veri Modelleri  
**Veri Modelleri:**
- `DeviceHardwareInfoDto.cs`: WMI'dan toplanan hardware bilgileri
- `SystemStateModel.cs`: Sistem durumu ve performans metrikleri
- `OfflineDataModel.cs`: Offline modda saklanan veri yapısı

#### 📁 **Configuration/** - Agent Ayarları
```json
// appsettings.json - Agent konfigürasyonu  
{
  "AgentSettings": {
    "ApiBaseUrl": "http://localhost:5093",
    "ScanIntervalMinutes": 30,
    "EnableHardwareMonitoring": true,
    "EnableOfflineStorage": true,
    "MaxOfflineRecords": 10000
  },
  "WindowsAgent": {
    "UseWMI": true,
    "CollectInstalledSoftware": true,
    "CollectNetworkInfo": true
  }
}
```

#### 🔧 **CrossPlatformSystemInfo.cs** - Sistem Bilgisi Toplama
**Ana İşlevler:**
- WMI sorguları ile hardware bilgisi toplama
- Registry okuma işlemleri
- Network adapter bilgileri
- Kurulu yazılım listesi çıkarma
- Performance counter'lar ile sistem metrikleri

### 🌐 **Inventory.WebApp** - Web Yönetim Arayüzü

Modern ASP.NET Core MVC tabanlı web arayüzü.

#### 📁 **Pages/** - Razor Pages
**Ana Sayfalar:**
- `Index.cshtml`: Dashboard ve genel istatistikler
- `Devices/`: Cihaz listesi, detay görünüm, düzenleme sayfaları
- `Reports/`: Raporlama ve analytics sayfaları
- `Settings/`: Sistem ayarları ve konfigürasyon

#### 📁 **wwwroot/** - Static Dosyalar
**Frontend Dosyaları:**
- `css/`: Bootstrap tabanlı custom CSS stilleri
- `js/`: jQuery ve custom JavaScript fonksiyonları
- `lib/`: Third-party libraries (jQuery, Bootstrap)

**🆕 UI İyileştirmeleri (v2.1):**
- **1920x1080 Navbar Düzeltmesi**: Son güncelleme göstergesi için genişletilmiş alan
- **Merkezi Cihaz Detay Tasarımı**: Cihaz adı/durumu ortada, işlem butonları altında
- **Mobil Navbar İyileştirmeleri**: Çaykur logosu sol hizalama, düzenli hamburger menü
- **Tablo Sütun Geri Yükleme**: Desktop'ta tüm sütunlar (MAC, Model, Konum) görünür
- **Değişiklik Logları Genişletildi**: Eski/Yeni değer sütunları geri getirildi
- **Ağ Tarama Düzeni**: Grid layout düzeltmeleri, hizalama sorunları giderildi
- **Cache Busting**: Cihaz detay sayfasında önbellekleme sorunları çözüldü

### 📊 **Inventory.Data** - Entity Framework Veri Katmanı

Veritabanı işlemleri ve ORM katmanı.

#### 📁 **Contexts/** - Database Context'leri
- `InventoryDbContext.cs`: Ana database context, entity configurations
- `DbInitializer.cs`: Veritabanı ilk kurulum ve seed data

#### 📁 **Migrations/** - EF Core Migrations
- Veritabanı şema güncellemeleri
- Version kontrollü veritabanı yapısı
- SQL Server, PostgreSQL migration dosyaları

#### 📁 **Repositories/** - Repository Pattern
- `DeviceRepository.cs`: Cihaz veri erişim katmanı
- `ChangeLogRepository.cs`: Değişiklik log veri erişimi
- `GenericRepository.cs`: Genel CRUD operasyonları

### 🏗️ **Inventory.Domain** - Domain Models ve Business Logic

İş mantığı ve domain modelleri.

#### 📁 **Entities/** - Database Entities
**Ana Entity'ler:**
- `Device.cs`: Cihaz ana modeli (ID, Name, IP, MAC, vb.)
- `HardwareInfo.cs`: Donanım bilgileri (CPU, RAM, GPU, vb.)
- `SoftwareInfo.cs`: Yazılım bilgileri (OS, Version, vb.)
- `DeviceChangeLog.cs`: Değişiklik geçmişi modeli
- `NetworkAdapter.cs`, `RamModule.cs`, `Disk.cs`: Hardware detay modelleri

**🆕 Device Entity Yeni Alanları:**
- `BarcodeNumber`: Manuel barkod numarası (agent tarafından değiştirilemez)
- `Notes`: Cihaz notları (manuel giriş, agent korumalı)
- Bu alanlar sadece manuel işlemlerle güncellenebilir, otomatik agent işlemlerinden korunmuştur

#### 📁 **Enums/** - System Enumerations
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
```

#### 📁 **ValueObjects/** - Domain Value Objects
- `IpAddress.cs`: IP adresi value object
- `MacAddress.cs`: MAC adresi validation ve formatting
- `SerialNumber.cs`: Seri numarası value object

### 🔧 **Inventory.Shared** - Paylaşılan Sınıflar

Projeler arası paylaşılan utility sınıfları.

#### 📁 **DTOs/** - Shared Data Transfer Objects
- `ApiResponseDto.cs`: Standart API response wrapper
- `PagedResultDto.cs`: Sayfalama için generic wrapper
- `ValidationResultDto.cs`: Validation sonuçları

#### 📁 **Extensions/** - Extension Methods
- `StringExtensions.cs`: String utility methods
- `DateTimeExtensions.cs`: DateTime formatting helpers
- `CollectionExtensions.cs`: Collection utility methods

#### 📁 **Helpers/** - Utility Classes
- `NetworkHelper.cs`: IP range, subnet hesaplamaları
- `FileHelper.cs`: Dosya işlemleri utility'leri
- `CryptoHelper.cs`: Şifreleme ve hash işlemleri

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

## Kurulum ve Deployment Seçenekleri

### 🏠 **Lokal Development Kurulumu**

#### Gereksinimler
- .NET 8.0 SDK
- Git
- Visual Studio 2022 / VS Code (opsiyonel)
- Docker Desktop (opsiyonel)

#### Adım Adım Kurulum
```bash
# 1. Repository'yi klonla
git clone https://github.com/hizir-ceylan/InventoryManagementSystem.git
cd InventoryManagementSystem

# 2. NuGet paketlerini restore et
dotnet restore

# 3. Database migration'ları çalıştır
dotnet ef database update --project Inventory.Data --startup-project Inventory.Api

# 4. API'yi başlat
dotnet run --project Inventory.Api --environment Development

# 5. Web App'i başlat (isteğe bağlı, ayrı terminal)
dotnet run --project Inventory.WebApp --environment Development

# 6. Agent'ı test için başlat (Windows'ta, ayrı terminal)
dotnet run --project Inventory.Agent.Windows --environment Development
```

**Erişim URL'leri:**
- API Swagger: http://localhost:5093/swagger
- Web App: http://localhost:5094

### 🖥️ **Windows Server Production Kurulumu**

#### Sunucu Gereksinimleri
- Windows Server 2019+ / Windows 10+
- .NET 8.0 Runtime
- IIS (Web App için)
- SQL Server (production için önerilen)
- RAM: 4GB+ 
- Disk: 10GB+ (log ve database için)

#### Otomatik Kurulum (Önerilen)
```powershell
# Yönetici PowerShell'de çalıştır
cd build-tools
.\Build-Setup.ps1
```

Bu script şunları yapar:
1. .NET runtime kontrolü ve kurulumu
2. SQL Server bağlantı testi
3. API ve Agent projelerini Release modunda build
4. Windows Service'leri oluşturur ve başlatır
5. IIS site'ini configure eder (WebApp için)
6. Firewall kurallarını açar

#### Manuel Production Kurulumu

**1. Servisleri Build Et:**
```powershell
# API için
dotnet publish Inventory.Api -c Release -o "C:\InventoryManagement\API" --self-contained

# Agent için  
dotnet publish Inventory.Agent.Windows -c Release -o "C:\InventoryManagement\Agent" --self-contained

# Web App için
dotnet publish Inventory.WebApp -c Release -o "C:\InventoryManagement\WebApp" --self-contained
```

**2. Windows Service'leri Oluştur:**
```powershell
# API Service
sc create "InventoryManagementApi" binpath="C:\InventoryManagement\API\Inventory.Api.exe" start=auto
sc description "InventoryManagementApi" "Inventory Management System API Service"

# Agent Service  
sc create "InventoryManagementAgent" binpath="C:\InventoryManagement\Agent\Inventory.Agent.Windows.exe" start=auto depend="InventoryManagementApi"
sc description "InventoryManagementAgent" "Inventory Management System Windows Agent"
```

**3. IIS Site Kurulumu (Web App):**
```powershell
# IIS ve ASP.NET Core Hosting Bundle yükle
Enable-WindowsOptionalFeature -Online -FeatureName IIS-WebServerRole, IIS-WebServer, IIS-CommonHttpFeatures, IIS-HttpErrors, IIS-HttpRedirect, IIS-ApplicationDevelopment, IIS-NetFxExtensibility45, IIS-HealthAndDiagnostics, IIS-HttpLogging, IIS-Security, IIS-RequestFiltering, IIS-Performance, IIS-WebServerManagementTools, IIS-ManagementConsole, IIS-IIS6ManagementCompatibility, IIS-Metabase, IIS-ASPNET45

# IIS site oluştur
New-IISSite -Name "InventoryWebApp" -PhysicalPath "C:\InventoryManagement\WebApp" -Port 5094
```

### 🐳 **Docker Production Deployment**

#### Docker Compose ile Tam Stack

**docker-compose.production.yml:**
```yaml
version: '3.8'

services:
  # SQL Server Database
  sqlserver:
    image: mcr.microsoft.com/mssql/server:2022-latest
    environment:
      SA_PASSWORD: "YourStrong@Password123"
      ACCEPT_EULA: "Y"
    ports:
      - "1433:1433"
    volumes:
      - sqlserver_data:/var/opt/mssql
    restart: unless-stopped

  # API Service
  inventory-api:
    build: 
      context: .
      dockerfile: Dockerfile
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ConnectionStrings__DefaultConnection=Server=sqlserver;Database=InventoryDB;User Id=sa;Password=YourStrong@Password123;TrustServerCertificate=true;
      - DatabaseProvider=SqlServer
    ports:
      - "5093:5093"
    depends_on:
      - sqlserver
    restart: unless-stopped
    volumes:
      - api_data:/app/Data
      - api_logs:/app/Logs

  # Web Application
  inventory-webapp:
    build:
      context: .
      dockerfile: Dockerfile.webapp
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ApiSettings__BaseUrl=http://inventory-api:5093
    ports:
      - "5094:5094"
    depends_on:
      - inventory-api
    restart: unless-stopped

  # NGINX Reverse Proxy
  nginx:
    image: nginx:alpine
    ports:
      - "80:80"
      - "443:443"
    volumes:
      - ./nginx/nginx.conf:/etc/nginx/nginx.conf
      - ./ssl:/etc/ssl/certs
    depends_on:
      - inventory-api
      - inventory-webapp
    restart: unless-stopped

volumes:
  sqlserver_data:
  api_data:
  api_logs:
```

**Kurulum Komutları:**
```bash
# Production environment'ı başlat
docker-compose -f docker-compose.production.yml up -d

# Database migration çalıştır
docker-compose exec inventory-api dotnet ef database update

# Health check
curl http://localhost/api/device
```

### ☁️ **Cloud Deployment (Azure/AWS)**

#### Azure App Service Deployment

**Azure CLI ile:**
```bash
# Resource group oluştur
az group create --name InventoryManagementRG --location "East US"

# App Service Plan oluştur  
az appservice plan create --name InventoryPlan --resource-group InventoryManagementRG --sku B1 --is-linux

# Web App oluştur (API için)
az webapp create --resource-group InventoryManagementRG --plan InventoryPlan --name inventory-api-app --runtime "DOTNETCORE|8.0"

# Web App oluştur (WebApp için)
az webapp create --resource-group InventoryManagementRG --plan InventoryPlan --name inventory-webapp-app --runtime "DOTNETCORE|8.0"

# SQL Database oluştur
az sql server create --name inventory-sql-server --resource-group InventoryManagementRG --location "East US" --admin-user sqladmin --admin-password "YourStrong@Password123"
az sql db create --resource-group InventoryManagementRG --server inventory-sql-server --name InventoryDB --service-objective Basic

# Deploy
dotnet publish Inventory.Api -c Release -o ./publish
az webapp deployment source config-zip --resource-group InventoryManagementRG --name inventory-api-app --src ./publish.zip
```

#### AWS ECS Deployment

**ECS Task Definition örneği:**
```json
{
  "family": "inventory-management",
  "networkMode": "awsvpc",
  "requiresCompatibilities": ["FARGATE"],
  "cpu": "512",
  "memory": "1024",
  "executionRoleArn": "arn:aws:iam::account:role/ecsTaskExecutionRole",
  "containerDefinitions": [
    {
      "name": "inventory-api",
      "image": "your-ecr-repo/inventory-api:latest",
      "portMappings": [{"containerPort": 5093}],
      "environment": [
        {"name": "ASPNETCORE_ENVIRONMENT", "value": "Production"},
        {"name": "ConnectionStrings__DefaultConnection", "value": "your-rds-connection-string"}
      ]
    }
  ]
}
```

### 🔗 **Network Sunucu Konfigürasyonu**

#### Kurumsal Ağda API Sunucusu Kurulumu

**Senaryo**: API'yi merkezi sunucuda, Agent'ları client bilgisayarlarda çalıştırma

**1. Sunucu Tarafı (API):**
```json
// appsettings.Production.json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=SQL-SERVER-01;Database=InventoryDB;Integrated Security=true;"
  },
  "Urls": "http://0.0.0.0:5093", // Tüm interface'lerden erişim
  "ApiSettings": {
    "AllowedOrigins": ["http://webapp-server:5094", "https://inventory.company.com"],
    "EnableSwagger": false // Production'da kapatılabilir
  }
}
```

**2. Client Tarafı (Agent):**
```json
// Her client'ta appsettings.json
{
  "AgentSettings": {
    "ApiBaseUrl": "http://inventory-server.company.local:5093",
    "ScanIntervalMinutes": 30,
    "EnableOfflineStorage": true,
    "RetryCount": 5,
    "Timeout": 60
  }
}
```

**3. Firewall Kuralları:**
```powershell
# Sunucu firewall - API port açma
New-NetFirewallRule -DisplayName "Inventory Management API" -Direction Inbound -Protocol TCP -LocalPort 5093 -Action Allow

# Client firewall - Outbound connection
New-NetFirewallRule -DisplayName "Inventory Agent API Connection" -Direction Outbound -Protocol TCP -RemotePort 5093 -Action Allow
```

#### Web App'i Farklı Sunucuda Çalıştırma

**Web App Sunucusu Konfigürasyonu:**
```json
// appsettings.Production.json (Web App)
{
  "ApiSettings": {
    "BaseUrl": "http://api-server.company.local:5093",
    "Timeout": 30,
    "ApiKey": "your-api-key-if-implemented"
  },
  "Urls": "http://0.0.0.0:5094"
}
```

#### Load Balancing ve High Availability

**NGINX Load Balancer Konfigürasyonu:**
```nginx
upstream inventory_api {
    server api-server-01:5093;
    server api-server-02:5093;
    server api-server-03:5093;
}

server {
    listen 80;
    server_name inventory.company.com;
    
    location /api/ {
        proxy_pass http://inventory_api;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
    }
    
    location / {
        proxy_pass http://webapp-server:5094;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
    }
}
```

### 📊 **Database Production Konfigürasyonu**

#### SQL Server Production Setup

```sql
-- Database oluşturma
CREATE DATABASE InventoryDB
ON (
    NAME = 'InventoryDB_Data',
    FILENAME = 'C:\Data\InventoryDB.mdf',
    SIZE = 1GB,
    MAXSIZE = 10GB,
    FILEGROWTH = 100MB
)
LOG ON (
    NAME = 'InventoryDB_Log',
    FILENAME = 'C:\Logs\InventoryDB.ldf',
    SIZE = 100MB,
    MAXSIZE = 1GB,
    FILEGROWTH = 10MB
);

-- Backup job oluşturma
EXEC sp_add_job 
    @job_name = 'InventoryDB Daily Backup',
    @enabled = 1;

EXEC sp_add_jobstep
    @job_name = 'InventoryDB Daily Backup',
    @step_name = 'Backup Database',
    @command = 'BACKUP DATABASE InventoryDB TO DISK = ''C:\Backups\InventoryDB_$(ESCAPE_SQUOTE(STRTDT))_$(ESCAPE_SQUOTE(STRTTM)).bak''';
```

#### PostgreSQL Production Setup

```sql
-- Database ve user oluşturma
CREATE DATABASE inventorydb;
CREATE USER inventoryuser WITH PASSWORD 'StrongPassword123!';
GRANT ALL PRIVILEGES ON DATABASE inventorydb TO inventoryuser;

-- Connection string
"Host=postgres-server;Database=inventorydb;Username=inventoryuser;Password=StrongPassword123!"
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

## Konfigürasyon ve Özelleştirme

### ⚙️ **Agent Konfigürasyon Seçenekleri**

#### Veri Toplama Sıklığı ve Monitoring
```json
{
  "AgentSettings": {
    "ScanIntervalMinutes": 30,        // Her 30 dakikada envanter toplama
    "EnableHardwareMonitoring": true, // Hardware değişiklik takibi
    "EnableSoftwareMonitoring": true, // Yazılım değişiklik takibi  
    "EnableChangeTracking": true,     // Değişiklik loglaması
    "EnableNetworkDiscovery": false   // Ağ keşif özelliği
  }
}
```

**Environment Variable ile Değiştirme:**
```bash
# Windows
set AgentSettings__ScanIntervalMinutes=60
set AgentSettings__EnableHardwareMonitoring=true

# Linux/Docker
export AgentSettings__ScanIntervalMinutes=60
export AgentSettings__EnableHardwareMonitoring=true
```

#### API Bağlantı ve Network Ayarları
```json
{
  "ApiSettings": {
    "BaseUrl": "http://localhost:5093",     // API sunucu adresi
    "Timeout": 30,                          // HTTP timeout (saniye)
    "RetryCount": 3,                        // Başarısız istek yeniden deneme
    "EnableOfflineStorage": true,           // Offline veri saklama
    "BatchUploadInterval": 300,             // Offline veri gönderim aralığı (saniye)
    "MaxOfflineRecords": 10000,            // Maksimum offline kayıt
    "EnableCompression": true,              // HTTP response compression
    "ApiKey": "",                          // API anahtarı (gelecek sürüm)
    "UseHttps": false                      // HTTPS zorunluluğu
  }
}
```

#### Windows Özel Ayarları
```json
{
  "WindowsAgent": {
    "UseWMI": true,                         // WMI kullanımı
    "CollectInstalledSoftware": true,       // Kurulu yazılım listesi
    "CollectRunningProcesses": false,       // Çalışan process'ler
    "CollectEventLogs": false,              // Windows event log'ları
    "CollectNetworkInfo": true,             // Network adapter bilgileri
    "CollectPerformanceCounters": false,    // Performance metrikleri
    "EnableServiceMode": true,              // Windows service modu
    "ServiceName": "InventoryManagementAgent"
  }
}
```

#### Dosya ve Log Yönetimi
```json
{
  "FileSettings": {
    "DataPath": "./Data",                   // Veri dosyaları dizini
    "LogPath": "./Logs",                    // Log dosyaları dizini
    "OfflineStoragePath": "./OfflineStorage", // Offline veri dizini
    "MaxLogFileSize": "10MB",               // Maksimum log dosya boyutu
    "LogRetentionDays": 30,                 // Log dosya saklama süresi
    "EnableFileCompression": true,          // Eski log dosyalarını sıkıştırma
    "BackupInterval": "24:00:00"            // Backup alma aralığı
  }
}
```

### 🖥️ **API Server Konfigürasyonu**

#### Database Bağlantıları
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=./Data/inventory.db",
    "SqlServerConnection": "Server=localhost;Database=InventoryDB;Trusted_Connection=true;TrustServerCertificate=true;",
    "PostgreSqlConnection": "Host=localhost;Database=inventorydb;Username=inventoryuser;Password=password;",
    "MySqlConnection": "Server=localhost;Database=inventorydb;Uid=root;Pwd=password;"
  },
  "DatabaseProvider": "SQLite"  // SQLite|SqlServer|PostgreSQL|MySQL
}
```

#### API Server Davranış Ayarları
```json
{
  "ApiSettings": {
    "EnableSwagger": true,                  // Swagger UI aktif/pasif
    "SwaggerRoutePrefix": "swagger",        // Swagger URL prefix
    "DefaultPageSize": 50,                  // Sayfalama varsayılan boyut
    "MaxPageSize": 1000,                    // Sayfalama maksimum boyut  
    "EnableCaching": true,                  // Response caching
    "CacheExpirationMinutes": 30,           // Cache süre sonu
    "EnableCors": true,                     // CORS aktif/pasif
    "AllowedOrigins": ["*"],                // İzinli origin'ler
    "MaxRequestSize": 104857600,            // Maksimum request boyutu (100MB)
    "EnableApiKey": false,                  // API key zorunluluğu
    "RateLimitPerMinute": 1000              // Dakika başına istek limiti
  }
}
```

#### Network Scanning Ayarları
```json
{
  "NetworkScan": {
    "DefaultTimeout": 5000,                 // Ağ tarama timeout (ms)
    "MaxConcurrentScans": 50,               // Eşzamanlı tarama sayısı
    "EnableNetworkDiscovery": true,         // Otomatik ağ keşfi
    "DiscoveryInterval": "01:00:00",        // Keşif çalıştırma aralığı
    "DefaultNetworkRange": "192.168.1.0/24", // Varsayılan IP aralığı
    "EnableHostnameResolution": true,       // DNS hostname çözümleme
    "EnableMacAddressDiscovery": true,      // MAC adresi keşfi
    "ScanMethods": ["Ping", "ARP", "SNMP"], // Kullanılacak keşif yöntemleri
    "SnmpCommunity": "public",              // SNMP community string
    "ExcludeRanges": ["192.168.1.1", "192.168.1.255"] // Hariç tutulacak IP'ler
  }
}
```

#### Logging ve Monitoring
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",             // Genel log seviyesi
      "Microsoft.AspNetCore": "Warning",    // ASP.NET Core logları
      "Microsoft.EntityFrameworkCore": "Error", // EF Core logları  
      "System.Net.Http": "Warning",         // HTTP client logları
      "Inventory": "Debug"                  // Uygulama özel logları
    },
    "File": {
      "Enabled": true,                      // Dosya logging aktif
      "Path": "./Logs/api-{Date}.log",      // Log dosya yolu
      "RetentionHours": 168,                // Log saklama süresi (7 gün)
      "LogLevel": "Information"             // Dosya log seviyesi
    },
    "Console": {
      "Enabled": true,                      // Konsol logging aktif
      "LogLevel": "Information"             // Konsol log seviyesi
    },
    "EventLog": {
      "Enabled": false,                     // Windows Event Log
      "SourceName": "InventoryManagementApi"
    }
  }
}
```

### 🌐 **Web App Konfigürasyonu**

#### Web Application Ayarları
```json
{
  "WebAppSettings": {
    "ApiBaseUrl": "http://localhost:5093",  // API server adresi
    "EnableRealTimeUpdates": true,          // SignalR real-time güncellemeler
    "RefreshInterval": 30,                  // Otomatik sayfa yenileme (saniye)
    "EnableDarkMode": true,                 // Dark mode desteği
    "DefaultLanguage": "tr-TR",             // Varsayılan dil
    "EnableExport": true,                   // Veri export özellikleri
    "ExportFormats": ["Excel", "CSV", "PDF"], // Desteklenen export formatları
    "MaxExportRecords": 10000,              // Maksimum export kayıt sayısı
    "EnableAuditLog": true                  // Kullanıcı işlem logları
  }
}
```

#### Authentication ve Authorization (Gelecek Sürüm)
```json
{
  "Authentication": {
    "EnableAuthentication": false,          // Kimlik doğrulama aktif
    "AuthenticationType": "JWT",            // JWT|Cookie|Windows
    "JwtSettings": {
      "SecretKey": "your-secret-key-here",
      "Issuer": "InventoryManagementSystem",
      "Audience": "InventoryUsers",
      "ExpirationMinutes": 60
    },
    "WindowsAuthentication": {
      "Enabled": false,
      "AutoLogin": true
    }
  },
  "Authorization": {
    "EnableRoleBasedAccess": false,         // Rol tabanlı erişim
    "DefaultRole": "User",
    "Roles": {
      "Admin": ["Read", "Write", "Delete", "Configure"],
      "User": ["Read"],
      "Operator": ["Read", "Write"]
    }
  }
}
```

### 🐳 **Docker ve Container Konfigürasyonu**

#### Docker Environment Variables
```bash
# API Container
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://+:5093
ConnectionStrings__DefaultConnection=Server=sqlserver;Database=InventoryDB;User Id=sa;Password=YourStrong@Password123;
DatabaseProvider=SqlServer
ApiSettings__EnableSwagger=false
ApiSettings__AllowedOrigins=["http://webapp:5094"]

# Agent Container  
AgentSettings__ApiBaseUrl=http://api:5093
AgentSettings__ScanIntervalMinutes=30
AgentSettings__EnableOfflineStorage=true

# Web App Container
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://+:5094
WebAppSettings__ApiBaseUrl=http://api:5093
```

#### Docker Compose Özel Konfigürasyonu
```yaml
services:
  inventory-api:
    environment:
      - DatabaseProvider=SqlServer
      - ConnectionStrings__DefaultConnection=${DB_CONNECTION_STRING}
      - ApiSettings__EnableSwagger=${ENABLE_SWAGGER:-false}
    volumes:
      - api_data:/app/Data
      - api_logs:/app/Logs
    deploy:
      resources:
        limits:
          memory: 1G
          cpus: '0.5'
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:5093/health"]
      interval: 30s
      timeout: 10s
      retries: 3
```

### 🔧 **Performance ve Optimizasyon Ayarları**

#### Database Performance
```json
{
  "DatabaseSettings": {
    "CommandTimeout": 60,                   // SQL command timeout (saniye)
    "ConnectionTimeout": 30,                // Bağlantı timeout (saniye)
    "MaxRetryCount": 3,                     // Bağlantı yeniden deneme
    "EnableSensitiveDataLogging": false,    // SQL parametrelerini loglama
    "EnableQuerySplitting": true,           // EF Core query splitting
    "ConnectionPoolSize": 100,              // Bağlantı havuzu boyutu
    "EnableBatchProcessing": true,          // Toplu işlem optimizasyonu
    "BatchSize": 1000                       // Toplu işlem boyutu
  }
}
```

#### Memory ve CPU Optimizasyonu
```json
{
  "PerformanceSettings": {
    "EnableResponseCompression": true,      // HTTP response sıkıştırma
    "EnableOutputCaching": true,            // Output cache
    "MaxConcurrentRequests": 1000,          // Eşzamanlı istek limiti
    "EnableBackgroundServices": true,       // Arka plan servisleri
    "GarbageCollectionMode": "Server",      // GC modu
    "ThreadPoolMinThreads": 50,             // Minimum thread sayısı
    "ThreadPoolMaxThreads": 1000            // Maksimum thread sayısı
  }
}
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
