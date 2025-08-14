# Windows Update Detection ve Service Installation Troubleshooting

## Windows Güncelleme Tespiti Sorunu

### Problem
Kullanıcı Windows güncellemesi taraması başlattığında, bekleyen güncellemeler (KB5063878 - restart bekleyen, KB890830 - yüklenmek için hazır) sistem tarafından tespit edilmiyor.

### Çözüm
Windows Update detection servisi geliştirildi ve aşağıdaki iyileştirmeler yapıldı:

#### 1. Kapsamlı Güncelleme Tarama Sorguları
```csharp
// Önceki sorgular (yetersiz)
"IsInstalled=0 and Type='Software'"
"IsInstalled=0 and IsDownloaded=1 and Type='Software'"

// Yeni kapsamlı sorgular
"IsInstalled=0 and Type='Software' and IsHidden=0"          // Mevcut güncellemeler
"IsInstalled=0 and IsDownloaded=1 and Type='Software' and IsHidden=0"  // İndirilmiş güncellemeler
"RebootRequired=1 and Type='Software' and IsHidden=0"       // RESTART BEKLEYEN GÜNCELLEMELER
"IsInstalled=0 and Type='Driver' and IsHidden=0"            // Driver güncellemeleri
"RebootRequired=1 and Type='Driver' and IsHidden=0"         // Restart bekleyen driver güncellemeleri
```

#### 2. Gelişmiş Status Belirleme
```csharp
// Yeni status logic
if (update.RebootRequired && update.IsInstalled)
    status = UpdateStatus.PendingRestart;  // KB5063878 gibi durumlar için
else if (update.IsDownloaded && !update.IsInstalled)
    status = UpdateStatus.Downloaded;      // KB890830 gibi durumlar için
else if (!update.IsInstalled)
    status = UpdateStatus.Available;
else
    status = UpdateStatus.Installed;
```

## Windows Service Kurulumu ve Gereksinimler

### Otomatik Kurulum (Önerilen)
1. **InnoSetup Installer** kullanın:
   ```bash
   # Build-Setup.ps1 ile otomatik setup oluşturma
   .\build-tools\Build-Setup.ps1
   ```

2. **Oluşturulan setup.exe** şu servisleri otomatik olarak kurar:
   - `InventoryManagementApi` (API Servisi)
   - `InventoryManagementAgent` (Agent Servisi - Update detection burada çalışır)

### Manuel Service Kurulumu
Eğer otomatik kurulum çalışmıyorsa:

```cmd
# Yönetici olarak Command Prompt açın

# API Service
sc create InventoryManagementApi binPath= "C:\Program Files\Inventory Management System\Api\Inventory.Api.exe" start= auto DisplayName= "Inventory Management API" obj= LocalSystem

# Agent Service (Update detection için gerekli)
sc create InventoryManagementAgent binPath= "C:\Program Files\Inventory Management System\Agent\Inventory.Agent.Windows.exe --service" start= delayed-auto DisplayName= "Inventory Management Agent" obj= LocalSystem depend= InventoryManagementApi

# Service konfigürasyonu
sc config InventoryManagementAgent start= delayed-auto
sc failure InventoryManagementAgent reset= 86400 actions= restart/10000/restart/10000/restart/10000
sc description InventoryManagementAgent "Inventory Management System Agent - Collects and reports system inventory data including Windows Updates"
```

### Service Kontrol Komutları
```cmd
# Servisleri başlatma
sc start InventoryManagementApi
sc start InventoryManagementAgent

# Servis durumu kontrol
sc query InventoryManagementApi
sc query InventoryManagementAgent

# Servisleri durdurma
sc stop InventoryManagementAgent
sc stop InventoryManagementApi

# Event log kontrol
eventvwr.msc
# Application log > InventoryManagementAgent source'unu kontrol edin
```

### Güncelleme Tarama Çalışma Düzeni

1. **Otomatik Tarama**: Agent servisi her 30 dakikada bir Windows Update taraması yapar
2. **Manuel Tarama**: API üzerinden `/api/Update/scan/{deviceId}` endpoint'i ile tetiklenir
3. **Detection Timing**: Service başladıktan 5 dakika sonra ilk tarama, sonra 30 dakikada bir
4. **Log Lokasyonu**: `%PROGRAMDATA%\Inventory Management System\Logs\`

### Timezone Sorunu Çözümü

#### Problem
API logları UTC zamanında, sistem GMT+3 (Türkiye saati) kullanmalı.

#### Çözüm
1. **Agent Side**: Tüm timestamp'ler `TimeZoneHelper.GetUtcNowForStorage()` kullanıyor
2. **API Side**: Update service Turkey timezone kullanıyor
3. **Database**: Tüm kayıtlar GMT+3 olarak saklanıyor

### Troubleshooting

#### Agent Servisi Çalışmıyor
```cmd
# Service log kontrolü
type "%PROGRAMDATA%\Inventory Management System\Logs\*agent*.log"

# Service yeniden başlatma
sc stop InventoryManagementAgent
sc start InventoryManagementAgent
```

#### Windows Update Taraması Çalışmıyor
1. **Windows Update Service** çalıştığından emin olun:
   ```cmd
   sc query wuauserv
   sc start wuauserv
   ```

2. **Agent servisi permissions** kontrol edin:
   - Service `LocalSystem` olarak çalışmalı
   - Windows Update API erişim yetkisi olmalı

3. **Manual test** yapın:
   ```cmd
   cd "C:\Program Files\Inventory Management System\Agent"
   Inventory.Agent.Windows.exe --test-updates
   ```

#### Belirli Güncellemeler Görünmüyor
- KB5063878 (restart bekleyen) → `PendingRestart` status'unda görünmeli
- KB890830 (hazır yükleme) → `Available` veya `Downloaded` status'unda görünmeli

Eğer hala görünmüyorsa:
1. Windows Update'i manuel çalıştırın
2. Agent servisini yeniden başlatın
3. 30 dakika bekleyin (otomatik tarama)
4. API'den `/api/Update/device/{deviceId}` endpoint'ini kontrol edin

### Environment Variables
Service kurulumu sonrası şu environment variable'lar set edilir:
```
ApiSettings__BaseUrl=http://localhost:5093
ApiSettings__EnableOfflineStorage=true
ApiSettings__OfflineStoragePath=%PROGRAMDATA%\Inventory Management System\OfflineStorage
ConnectionStrings__DefaultConnection=Data Source=%PROGRAMDATA%\Inventory Management System\Data\inventory.db
INVENTORY_DATA_PATH=%PROGRAMDATA%\Inventory Management System\Data
INVENTORY_LOG_PATH=%PROGRAMDATA%\Inventory Management System\Logs
```

### API Endpoints - Update Detection
```http
# Güncelleme taraması başlat
POST /api/Update/scan/{deviceId}

# Cihaz güncellemelerini listele
GET /api/Update/device/{deviceId}

# Tüm güncellemeleri listele
GET /api/Update?status=PendingRestart

# Güncelleme istatistikleri
GET /api/Update/statistics
```