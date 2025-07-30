# Veri Kalıcılığı ve Kayıt Kılavuzu / Data Persistence and Logging Guide

## Türkçe

### Veri Depolama Konumları

Inventory Management System Agent, verilerinizi kalıcı dizinlerde depolar ve sistem yeniden başlatıldığında veriler kaybolmaz.

#### Varsayılan Depolama Konumları

1. **Windows Service Modunda (Önerilen)**: `C:\ProgramData\Inventory Management System\`
   - **Offline Veri Depolama**: `C:\ProgramData\Inventory Management System\OfflineStorage\`
   - **Yerel Loglar**: `C:\ProgramData\Inventory Management System\Logs\`
   - **Veritabanı**: `C:\ProgramData\Inventory Management System\Data\inventory.db`

2. **Kullanıcı Modunda**: `Documents/InventoryManagementSystem/`
   - **Offline Veri Depolama**: `Documents/InventoryManagementSystem/OfflineStorage/`
   - **Yerel Loglar**: `Documents/InventoryManagementSystem/LocalLogs/`

#### Platform Bazında Depolama Yerleri

**Windows (Service Mode - Recommended):**
- `C:\ProgramData\Inventory Management System\` (system-wide, persistent)

**Windows (User Mode):**
- `C:\Users\[Kullanıcı]\Documents\InventoryManagementSystem\`
- `%APPDATA%\InventoryManagementSystem\` (Documents erişimi yoksa)
- `%PROGRAMDATA%\InventoryManagementSystem\` (genel sistem klasörü)

**Linux:**
- `~/Documents/InventoryManagementSystem/`
- `~/.local/share/InventoryManagementSystem/` (Documents yoksa)
- `/var/lib/InventoryManagementSystem/` (sistem geneli)

### Özel Depolama Yolu Belirleme

Özel bir depolama yolu kullanmak için:

#### 1. Çevre Değişkenleri (Environment Variables)
```bash
# Windows
set ApiSettings__OfflineStoragePath=C:\MyCustomPath\InventoryData
set ApiSettings__LogPath=C:\MyCustomPath\Logs

# Linux
export ApiSettings__OfflineStoragePath=/opt/inventory/data
export ApiSettings__LogPath=/opt/inventory/logs
```

#### 2. Konfigürasyon Dosyası (appsettings.json)
```json
{
  "ApiSettings": {
    "OfflineStoragePath": "C:\\MyCustomPath\\InventoryData",
    "LogPath": "/opt/inventory/logs"
  }
}
```

### Veri Türleri

#### 1. Offline Cihaz Verileri
- **Dosya**: `offline_devices.json`
- **İçerik**: API'ye gönderilemediğinde offline olarak saklanan cihaz bilgileri
- **Format**: JSON array

#### 2. Cihaz Değişiklik Logları
- **Dosyalar**: `device-log-YYYY-MM-DD-HH.json`
- **İçerik**: Saatlik cihaz durumu snapshots
- **Retention**: 48 saat (otomatik temizlik)

#### 3. Değişiklik Detayları
- **Klasör**: `Changes/`
- **Dosyalar**: `device-changes-YYYY-MM-DD-HH-mm-ss.json`
- **İçerik**: Cihazda tespit edilen değişikliklerin detayları

#### 4. Merkezi Loglar
- **Dosyalar**: `centralized-log-YYYY-MM-DD-HH.log`
- **İçerik**: Agent işlemleri ve hata logları
- **Retention**: 48 saat

### Sorun Giderme

#### "Veriler Nereye Kaydediliyor?"
Agent çalıştırıldığında şu bilgileri gösterir:
```
Storage Locations:
Offline Storage: C:\Users\[User]\Documents\InventoryManagementSystem\OfflineStorage
Local Logs: C:\Users\[User]\Documents\InventoryManagementSystem\LocalLogs
```

#### "Veriler Sistem Yeniden Başlatıldığında Kayboluyor"
Bu uyarıyı görüyorsanız:
```
WARNING: Offline storage is in temporary directory and will be lost on restart
```

**Çözüm**: 
1. Belgeler klasörünün erişilebilir olduğundan emin olun
2. Özel bir kalıcı yol belirleyin (yukarıdaki örneklere bakın)
3. Agent'ı yönetici hakları ile çalıştırın

#### "Log Dosyaları Bulunamıyor"
Log dosyalarının konumunu kontrol etmek için:
```bash
# Agent çalıştırıldığında gösterilen yolu kontrol edin
# Veya manuel olarak şu klasörleri kontrol edin:
Documents/InventoryManagementSystem/LocalLogs/
```

---

## English

### Data Storage Locations

The Inventory Management System Agent stores your data in persistent directories, ensuring data survival across system restarts.

#### Default Storage Locations

1. **Windows Service Mode (Recommended)**: `C:\ProgramData\Inventory Management System\`
   - **Offline Data Storage**: `C:\ProgramData\Inventory Management System\OfflineStorage\`
   - **Local Logs**: `C:\ProgramData\Inventory Management System\Logs\`
   - **Database**: `C:\ProgramData\Inventory Management System\Data\inventory.db`

2. **User Mode**: `Documents/InventoryManagementSystem/`
   - **Offline Data Storage**: `Documents/InventoryManagementSystem/OfflineStorage/`
   - **Local Logs**: `Documents/InventoryManagementSystem/LocalLogs/`

#### Platform-Specific Storage Paths

**Windows (Service Mode - Recommended):**
- `C:\ProgramData\Inventory Management System\` (system-wide, persistent)

**Windows (User Mode):**
- `C:\Users\[Username]\Documents\InventoryManagementSystem\`
- `%APPDATA%\InventoryManagementSystem\` (if Documents is inaccessible)
- `%PROGRAMDATA%\InventoryManagementSystem\` (system-wide)

**Linux:**
- `~/Documents/InventoryManagementSystem/`
- `~/.local/share/InventoryManagementSystem/` (if Documents doesn't exist)
- `/var/lib/InventoryManagementSystem/` (system-wide)

### Custom Storage Path Configuration

To use custom storage paths:

#### 1. Environment Variables
```bash
# Windows
set ApiSettings__OfflineStoragePath=C:\MyCustomPath\InventoryData
set ApiSettings__LogPath=C:\MyCustomPath\Logs

# Linux
export ApiSettings__OfflineStoragePath=/opt/inventory/data
export ApiSettings__LogPath=/opt/inventory/logs
```

#### 2. Configuration File (appsettings.json)
```json
{
  "ApiSettings": {
    "OfflineStoragePath": "C:\\MyCustomPath\\InventoryData",
    "LogPath": "/opt/inventory/logs"
  }
}
```

### Data Types

#### 1. Offline Device Data
- **File**: `offline_devices.json`
- **Content**: Device information stored offline when API is unavailable
- **Format**: JSON array

#### 2. Device Change Logs
- **Files**: `device-log-YYYY-MM-DD-HH.json`
- **Content**: Hourly device state snapshots
- **Retention**: 48 hours (automatic cleanup)

#### 3. Change Details
- **Folder**: `Changes/`
- **Files**: `device-changes-YYYY-MM-DD-HH-mm-ss.json`
- **Content**: Detailed device change information

#### 4. Centralized Logs
- **Files**: `centralized-log-YYYY-MM-DD-HH.log`
- **Content**: Agent operations and error logs
- **Retention**: 48 hours

### Troubleshooting

#### "Where is Data Being Saved?"
When the agent runs, it displays:
```
Storage Locations:
Offline Storage: C:\Users\[User]\Documents\InventoryManagementSystem\OfflineStorage
Local Logs: C:\Users\[User]\Documents\InventoryManagementSystem\LocalLogs
```

#### "Data is Lost After System Restart"
If you see this warning:
```
WARNING: Offline storage is in temporary directory and will be lost on restart
```

**Solution**: 
1. Ensure Documents folder is accessible
2. Set a custom persistent path (see examples above)
3. Run agent with administrator rights

#### "Log Files Not Found"
To check log file location:
```bash
# Check the path displayed when agent runs
# Or manually check these folders:
Documents/InventoryManagementSystem/LocalLogs/
```

### Configuration Examples

#### Windows Service with Custom Paths
```cmd
sc create "InventoryAgent" binPath="C:\Path\To\Inventory.Agent.Windows.exe --service"
sc config "InventoryAgent" start=auto
set ApiSettings__OfflineStoragePath=C:\ProgramData\InventoryManagement\Data
set ApiSettings__LogPath=C:\ProgramData\InventoryManagement\Logs
sc start "InventoryAgent"
```

#### Docker with Volume Mapping
```yaml
services:
  inventory-agent:
    image: inventory-agent:latest
    volumes:
      - /opt/inventory/data:/app/data
      - /opt/inventory/logs:/app/logs
    environment:
      - ApiSettings__OfflineStoragePath=/app/data
      - ApiSettings__LogPath=/app/logs
```