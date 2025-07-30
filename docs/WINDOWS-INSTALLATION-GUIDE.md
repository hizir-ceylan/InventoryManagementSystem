# Windows Tam Kurulum Rehberi - Inventory Management System

Bu rehber, Inventory Management System'in Windows ortamında sıfırdan başlayarak tam kurulumunu açıklar. Build alma, derleme ve servis kurulumu dahil tüm adımları içerir.

## İçindekiler

1. [Sistem Gereksinimleri](#sistem-gereksinimleri)
2. [Gerekli Yazılımlar](#gerekli-yazılımlar)
3. [Kaynak Kodunu İndirme](#kaynak-kodunu-indirme)
4. [Build Alma ve Derleme](#build-alma-ve-derleme)
5. [Windows Service Kurulumu](#windows-service-kurulumu)
6. [Test ve Doğrulama](#test-ve-dogrulama)
7. [Yönetim ve Kullanım](#yönetim-ve-kullanim)
8. [Sorun Giderme](#sorun-giderme)
9. [Kaldırma](#kaldirma)

## Sistem Gereksinimleri

### Minimum Gereksinimler
- **İşletim Sistemi**: Windows 10 (1903+) / Windows 11 / Windows Server 2019+
- **RAM**: 4 GB (8 GB önerilen)
- **Disk Alanı**: 2 GB boş alan
- **Network**: Internet bağlantısı (indirme için)
- **Portlar**: 5093 portu açık olmalı

### Yönetici Yetkileri
- Bu kurulum **Yönetici (Administrator)** yetkisi gerektirir
- PowerShell ve Command Prompt'u "Yönetici olarak çalıştır" seçeneği ile açın

## Gerekli Yazılımlar

### 1. .NET 8.0 Runtime Kurulumu

**Otomatik Kurulum (Önerilen):**
```powershell
# PowerShell'i yönetici olarak açın
winget install Microsoft.DotNet.Runtime.8
```

**Manuel Kurulum:**
1. [.NET 8.0 Runtime](https://dotnet.microsoft.com/download/dotnet/8.0) sayfasına gidin
2. "Run apps - Runtime" bölümünden "Download x64" butonuna tıklayın
3. İndirilen `dotnet-runtime-8.0.x-win-x64.exe` dosyasını çalıştırın
4. Kurulum sihirbazını takip edin

**Kurulum Kontrolü:**
```powershell
dotnet --version
# Çıktı: 8.0.x şeklinde olmalıdır
```

### 2. Git (İsteğe Bağlı)

Kaynak kodu GitHub'dan indirmek için:
```powershell
winget install Git.Git
```

veya [Git for Windows](https://git-scm.com/download/win) adresinden indirin.

## Kaynak Kodunu İndirme

### Seçenek 1: Git ile (Önerilen)
```powershell
# İstediğiniz dizine gidin (örn: C:\Projects)
cd C:\
mkdir Projects
cd Projects

# Repository'yi klonlayın
git clone https://github.com/hizir-ceylan/InventoryManagementSystem.git
cd InventoryManagementSystem
```

### Seçenek 2: ZIP Dosyası ile
1. [GitHub Repository](https://github.com/hizir-ceylan/InventoryManagementSystem) sayfasına gidin
2. Yeşil "Code" butonuna tıklayın
3. "Download ZIP" seçeneğini seçin
4. İndirilen dosyayı `C:\Projects\InventoryManagementSystem` dizinine açın

## Build Alma ve Derleme

### 1. Proje Dizinine Gidin
```powershell
cd C:\Projects\InventoryManagementSystem
```

### 2. Dependencies'leri Yükleyin
```powershell
# NuGet paketlerini geri yükle
dotnet restore
```

### 3. Projeyi Derleyin
```powershell
# Release modunda derle
dotnet build -c Release

# Build başarılı ise "Build succeeded" mesajı görünür
```

### 4. Publish Yapın (İsteğe Bağlı)
```powershell
# Standalone executable'lar oluştur
dotnet publish Inventory.Api -c Release -o "./publish/Api"
dotnet publish Inventory.Agent.Windows -c Release -o "./publish/Agent"
```

## Windows Service Kurulumu

### Otomatik Kurulum (Önerilen)

**PowerShell ile:**
```powershell
# Yönetici PowerShell'de proje dizininde
.\scripts\Install-WindowsServices.ps1
```

**Batch Script ile:**
```cmd
REM Yönetici Command Prompt'ta proje dizininde
scripts\install-windows-services.bat
```

### Manuel Kurulum Adımları

Otomatik kurulum çalışmazsa manuel olarak:

#### 1. Kurulum Dizini Oluştur
```powershell
$InstallDir = "C:\Program Files\InventoryManagementSystem"
New-Item -ItemType Directory -Path $InstallDir -Force
New-Item -ItemType Directory -Path "$InstallDir\Api" -Force
New-Item -ItemType Directory -Path "$InstallDir\Agent" -Force
New-Item -ItemType Directory -Path "$InstallDir\Data" -Force
New-Item -ItemType Directory -Path "$InstallDir\Logs" -Force
```

#### 2. Dosyaları Kopyala
```powershell
# API dosyaları
dotnet publish Inventory.Api -c Release -o "$InstallDir\Api"

# Agent dosyaları
dotnet publish Inventory.Agent.Windows -c Release -o "$InstallDir\Agent"
```

#### 3. API Servisi Oluştur
```powershell
sc.exe create "InventoryManagementApi" `
    binPath= "`"$InstallDir\Api\Inventory.Api.exe`"" `
    DisplayName= "Inventory Management API" `
    Description= "Inventory Management System API Service" `
    start= auto `
    obj= "LocalSystem"
```

#### 4. Agent Servisi Oluştur
```powershell
sc.exe create "InventoryManagementAgent" `
    binPath= "`"$InstallDir\Agent\Inventory.Agent.Windows.exe`" --service" `
    DisplayName= "Inventory Management Agent" `
    Description= "Inventory Management System Agent Service" `
    start= auto `
    obj= "LocalSystem" `
    depend= "InventoryManagementApi"

# Agent için başlangıç gecikmesi
sc.exe config "InventoryManagementAgent" start= delayed-auto
```

#### 5. Çevre Değişkenlerini Ayarla
```powershell
[Environment]::SetEnvironmentVariable("ApiSettings__BaseUrl", "http://localhost:5093", [EnvironmentVariableTarget]::Machine)
[Environment]::SetEnvironmentVariable("ApiSettings__EnableOfflineStorage", "true", [EnvironmentVariableTarget]::Machine)
[Environment]::SetEnvironmentVariable("ApiSettings__OfflineStoragePath", "$InstallDir\Data\OfflineStorage", [EnvironmentVariableTarget]::Machine)
```

#### 6. Firewall Kuralı Ekle
```powershell
New-NetFirewallRule -DisplayName "Inventory Management API" -Direction Inbound -Protocol TCP -LocalPort 5093 -Action Allow
```

#### 7. Servisleri Başlat
```powershell
# API'yi başlat
Start-Service -Name "InventoryManagementApi"

# 10 saniye bekle
Start-Sleep -Seconds 10

# Agent'ı başlat
Start-Service -Name "InventoryManagementAgent"
```

## Test ve Doğrulama

### 1. Servis Durumu Kontrolü
```powershell
# PowerShell ile
Get-Service -Name "InventoryManagement*"

# Command Prompt ile
sc query "InventoryManagementApi"
sc query "InventoryManagementAgent"
```

Beklenilen çıktı:
- Status: Running
- Start Type: Automatic

### 2. API Testi

**Web Tarayıcı ile:**
- http://localhost:5093/swagger adresine gidin
- Swagger UI açılmalı

**PowerShell ile:**
```powershell
# API'nin çalıştığını test et
Invoke-RestMethod -Uri "http://localhost:5093/api/device" -Method GET
```

**Command Prompt ile:**
```cmd
curl http://localhost:5093/api/device
```

### 3. Agent Testi

**Event Viewer ile:**
1. `eventvwr.msc` çalıştırın
2. Windows Logs → Application
3. Source: "InventoryManagement" kayıtlarını arayın

**Log Dosyaları ile:**
```powershell
# Log dosyalarını kontrol et
dir "C:\Program Files\InventoryManagementSystem\Logs\"
```

### 4. Network Test

**Port Kontrolü:**
```powershell
netstat -an | findstr :5093
# LISTENING durumunda olmalı
```

**Firewall Kontrolü:**
```powershell
Get-NetFirewallRule -DisplayName "*Inventory*"
```

## Yönetim ve Kullanım

### Servis Yöneticisi (Grafik Arayüz)

```
Win + R → services.msc → Enter
```

"Inventory Management" servislerini bulun ve:
- Başlat/Durdur/Yeniden Başlat
- Startup Type değiştir
- Properties ile detayları görüntüle

### PowerShell ile Yönetim

```powershell
# Durum kontrolü
Get-Service -Name "InventoryManagement*"

# Servisleri başlat
Start-Service -Name "InventoryManagementApi"
Start-Service -Name "InventoryManagementAgent"

# Servisleri durdur
Stop-Service -Name "InventoryManagementAgent"
Stop-Service -Name "InventoryManagementApi"

# Servisleri yeniden başlat
Restart-Service -Name "InventoryManagementApi"
Restart-Service -Name "InventoryManagementAgent"
```

### Script ile Yönetim

```cmd
# Proje dizininde
scripts\manage-services.bat
```

Bu script menü bazlı yönetim imkanı sunar.

### API Kullanımı

**Cihaz Ekleme:**
```powershell
$device = @{
    name = "TEST-PC-001"
    macAddress = "00:1B:44:11:3A:B7"
    ipAddress = "192.168.1.100"
    deviceType = "PC"
    model = "Dell OptiPlex"
    location = "Office-101"
    status = 0
} | ConvertTo-Json

Invoke-RestMethod -Uri "http://localhost:5093/api/device" -Method POST -Body $device -ContentType "application/json"
```

**Cihaz Listeleme:**
```powershell
Invoke-RestMethod -Uri "http://localhost:5093/api/device" -Method GET
```

## Sorun Giderme

### Servis Başlamıyor

**1. Event Logs Kontrol:**
```
eventvwr.msc → Windows Logs → Application
Source: InventoryManagement*
```

**2. Manuel Test:**
```powershell
# Servis dosyasını konsol modunda çalıştır
cd "C:\Program Files\InventoryManagementSystem\Api"
.\Inventory.Api.exe

# Hata mesajlarını gözlemle
```

**3. Port Çakışması:**
```powershell
# Port 5093'ü kullanan process'i bul
netstat -ano | findstr :5093

# Process'i sonlandır (PID ile)
taskkill /PID <PID> /F
```

### API'ye Erişilemiyor

**1. Firewall Kontrolü:**
```powershell
# Firewall kuralını kontrol et
Get-NetFirewallRule -DisplayName "*Inventory*"

# Yeniden ekle
New-NetFirewallRule -DisplayName "Inventory Management API" -Direction Inbound -Protocol TCP -LocalPort 5093 -Action Allow
```

**2. Service URL Kontrolü:**
```powershell
# appsettings.json'da URL'leri kontrol et
Get-Content "C:\Program Files\InventoryManagementSystem\Api\appsettings.json"
```

### Agent Veri Göndermiyor

**1. API Bağlantısı:**
```powershell
# Agent'tan API'ye erişimi test et
Invoke-RestMethod -Uri "http://localhost:5093/api/device" -Method GET
```

**2. Offline Storage:**
```powershell
# Offline storage dosyalarını kontrol et
dir "C:\Program Files\InventoryManagementSystem\Data\OfflineStorage"
```

**3. Dependency Kontrolü:**
```powershell
# Agent'ın API'ye bağımlılığını kontrol et
sc qc "InventoryManagementAgent"
```

### Genel Sorunlar

**1. .NET Runtime Eksik:**
```powershell
# .NET runtime'ı kontrol et
dotnet --version

# Eksikse yeniden kur
winget install Microsoft.DotNet.Runtime.8
```

**2. Yetkisiz Erişim:**
- PowerShell/CMD'yi "Yönetici olarak çalıştır"
- UAC'nin açık olduğundan emin olun

**3. Antivirus Engelleme:**
- Kurulum dizinini antivirus'ten hariç tutun
- Real-time protection'ı geçici olarak kapatın

## Kaldırma

### Otomatik Kaldırma
```cmd
REM Yönetici Command Prompt'ta
scripts\uninstall-windows-services.bat
```

### Manuel Kaldırma

**1. Servisleri Durdur ve Kaldır:**
```powershell
# Servisleri durdur
Stop-Service -Name "InventoryManagementAgent", "InventoryManagementApi" -Force

# Servisleri kaldır
sc.exe delete "InventoryManagementAgent"
sc.exe delete "InventoryManagementApi"
```

**2. Dosyaları Kaldır:**
```powershell
# Kurulum dizinini kaldır
Remove-Item -Path "C:\Program Files\InventoryManagementSystem" -Recurse -Force
```

**3. Firewall Kuralını Kaldır:**
```powershell
Remove-NetFirewallRule -DisplayName "Inventory Management API"
```

**4. Çevre Değişkenlerini Kaldır:**
```powershell
[Environment]::SetEnvironmentVariable("ApiSettings__BaseUrl", $null, [EnvironmentVariableTarget]::Machine)
[Environment]::SetEnvironmentVariable("ApiSettings__EnableOfflineStorage", $null, [EnvironmentVariableTarget]::Machine)
[Environment]::SetEnvironmentVariable("ApiSettings__OfflineStoragePath", $null, [EnvironmentVariableTarget]::Machine)
[Environment]::SetEnvironmentVariable("ConnectionStrings__DefaultConnection", $null, [EnvironmentVariableTarget]::Machine)
[Environment]::SetEnvironmentVariable("INVENTORY_DATA_PATH", $null, [EnvironmentVariableTarget]::Machine)
[Environment]::SetEnvironmentVariable("INVENTORY_LOG_PATH", $null, [EnvironmentVariableTarget]::Machine)
```

## Destek ve Gelişmiş Ayarlar

### Gelişmiş Yapılandırma

**HTTPS Aktivasyonu:**
```json
// appsettings.json'da
{
  "Kestrel": {
    "Endpoints": {
      "Https": {
        "Url": "https://localhost:5094"
      }
    }
  }
}
```

**Özel Service Account:**
```powershell
# Yeni kullanıcı oluştur
net user "InventoryService" "ComplexPassword123!" /add
net localgroup "Log on as a service" "InventoryService" /add

# Servis hesabını değiştir
sc.exe config "InventoryManagementApi" obj= ".\InventoryService" password= "ComplexPassword123!"
```

**Performance Tuning:**
```powershell
# Agent tarama aralığı (dakika)
[Environment]::SetEnvironmentVariable("InventoryAgent__ScanInterval", "30", [EnvironmentVariableTarget]::Machine)

# API response cache süresi (saniye)
[Environment]::SetEnvironmentVariable("ApiSettings__CacheTimeout", "300", [EnvironmentVariableTarget]::Machine)
```

### Monitoring

**Performance Counters:**
```powershell
# CPU ve Memory kullanımı
Get-Counter "\Process(Inventory.Api)\% Processor Time"
Get-Counter "\Process(Inventory.Api)\Working Set"
```

**Health Check:**
```powershell
# API health endpoint'i
Invoke-RestMethod -Uri "http://localhost:5093/health"
```

### Backup ve Restore

**Database Backup:**
```powershell
# SQLite database backup
Copy-Item "C:\Program Files\InventoryManagementSystem\Data\*.db" "C:\Backup\InventoryDB\"
```

**Configuration Backup:**
```powershell
# Konfigürasyon dosyalarını backup al
Copy-Item "C:\Program Files\InventoryManagementSystem\Api\appsettings.json" "C:\Backup\Config\"
```

---

## Özet

Bu rehberi takip ederek Inventory Management System'i Windows'ta başarıyla kurabilirsiniz. Herhangi bir sorun yaşarsanız:

1. **Önce Sorun Giderme** bölümünü kontrol edin
2. **Event Viewer**'da detaylı hata mesajlarını inceleyin
3. **GitHub Issues** sayfasında benzer sorunları arayın
4. Yeni bir issue açarak destek isteyin

**Önemli**: Kurulum sonrası sistem yeniden başlatıldığında servisler otomatik olarak başlayacaktır.

**Erişim Adresleri**:
- API: http://localhost:5093
- Swagger UI: http://localhost:5093/swagger
- Kurulum Dizini: C:\Program Files\InventoryManagementSystem