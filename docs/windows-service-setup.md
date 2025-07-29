# Windows Service Kurulum ve Kullanım Kılavuzu

Bu kılavuz, Inventory Management System'in API ve Agent bileşenlerinin Windows servis olarak nasıl kurulacağını ve kullanılacağını açıklar.

## Özellikler

✅ **Otomatik Başlatma**: Windows açıldığında servisler otomatik olarak başlar  
✅ **Dependency Yönetimi**: Agent servisi, API servisinden sonra başlar  
✅ **Arka Plan Çalışma**: Servisler arka planda sessizce çalışır  
✅ **Event Log Entegrasyonu**: Sistem logları Windows Event Viewer'da görünür  
✅ **Kolay Yönetim**: Grafik arabirimler ile servis yönetimi  

## Sistem Gereksinimleri

- Windows 10/11 veya Windows Server 2016+
- .NET 8.0 Runtime
- Yönetici yetkileri
- Port 5093'ün açık olması

## Hızlı Kurulum

### 1. PowerShell ile Kurulum (Önerilen)

```powershell
# PowerShell'i Yönetici olarak açın
cd "C:\InventoryManagementSystem"
.\scripts\Install-WindowsServices.ps1
```

### 2. Batch Script ile Kurulum

```cmd
REM Komut satırını Yönetici olarak açın
cd "C:\InventoryManagementSystem"
scripts\install-windows-services.bat
```

## Detaylı Kurulum Adımları

### Ön Gereksinimler

1. **.NET 8 Runtime Kurulumu**
   ```powershell
   # Kontrol et
   dotnet --version
   
   # Eğer kurulu değilse:
   # https://dotnet.microsoft.com/download/dotnet/8.0 adresinden indirin
   ```

2. **Projeyi Derleyin**
   ```powershell
   dotnet build -c Release
   ```

### Manuel Kurulum

1. **Yönetici Yetkisiyle PowerShell Açın**
   - Windows tuşu + X → "Windows PowerShell (Admin)"

2. **Kurulum Scriptini Çalıştırın**
   ```powershell
   cd "PROJE_DIZINI"
   Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser
   .\scripts\Install-WindowsServices.ps1
   ```

3. **Servislerin Çalıştığını Kontrol Edin**
   ```powershell
   Get-Service -Name "InventoryManagement*"
   ```

## Servis Yönetimi

### Services.msc ile Grafik Yönetim

1. **Services Manager'ı Açın**
   ```
   Win + R → services.msc → Enter
   ```

2. **Servisleri Bulun**
   - `Inventory Management API`
   - `Inventory Management Agent`

3. **Servis İşlemleri**
   - Sağ tık → Start/Stop/Restart
   - Properties → Startup Type (Automatic/Manual/Disabled)

### PowerShell ile Yönetim

```powershell
# Durum kontrolü
Get-Service -Name "InventoryManagementApi", "InventoryManagementAgent"

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
# Grafik yönetim aracını çalıştır
scripts\manage-services.bat
```

## Test ve Doğrulama

### 1. API Test

```powershell
# Web tarayıcıda açın:
http://localhost:5093/swagger

# veya PowerShell ile test edin:
Invoke-RestMethod -Uri "http://localhost:5093/api/device" -Method GET
```

### Agent Çalışma Kontrolü

```powershell
# Event Viewer'ı açın
eventvwr.msc

# Windows Logs → Application bölümünde 
# "InventoryManagement" source'u arayın
```

**Agent Modları:**
- **Servis Modu**: Arka planda sürekli çalışır (30 dakikada bir)
- **Console Modu**: Tek seferlik çalışır ve çıkar
- **Continuous Modu**: Console'da sürekli çalışır (30 dakikada bir)

```powershell
# Farklı modları test edin
cd "C:\Program Files\InventoryManagementSystem\Agent"

# Tek seferlik çalıştır
.\Inventory.Agent.Windows.exe

# Sürekli modda çalıştır
.\Inventory.Agent.Windows.exe --continuous

# Yardım göster
.\Inventory.Agent.Windows.exe --help
```

### 3. Log Dosyaları

```
C:\Program Files\InventoryManagementSystem\Logs\
C:\Program Files\InventoryManagementSystem\Data\
```

## Yapılandırma

### Çevre Değişkenleri

Servis kurulumu sırasında aşağıdaki çevre değişkenleri otomatik ayarlanır:

```
ApiSettings__BaseUrl=http://localhost:5093
ApiSettings__EnableOfflineStorage=true
ApiSettings__OfflineStoragePath=C:\Program Files\InventoryManagementSystem\Data\OfflineStorage
```

### Manuel Yapılandırma

```powershell
# Sistem çevre değişkenlerini düzenle
[Environment]::SetEnvironmentVariable("ApiSettings__BaseUrl", "http://localhost:5093", [EnvironmentVariableTarget]::Machine)
```

## Sorun Giderme

### Error 1053 - Servis Başlamıyor

**Problem**: Windows servis yöneticisinden Agent servisini başlatırken "Error 1053: The service did not respond to the start or control request in a timely manner" hatası alınıyor.

**Çözüm**: Bu sürümde düzeltilmiştir. Artık Agent:
- ✅ Servis modunu otomatik algılar (komut satırı parametresi gerektirmez)
- ✅ Uygun Background Service pattern kullanır
- ✅ Proper async/await işlemlerini destekler
- ✅ Event Log'a hataları yazar

```powershell
# Servisi yeniden kur ve başlat
.\build-tools\Install-WindowsServices.ps1

# Servis durumunu kontrol et
Get-Service -Name "InventoryManagementAgent"

# Event Log'ları kontrol et
Get-EventLog -LogName Application -Source "InventoryManagementAgent" -Newest 10
```

### Servis Başlamıyor

1. **Event Logs Kontrolü**
   ```
   eventvwr.msc → Windows Logs → Application
   Source: InventoryManagement*
   ```

2. **Port Kontrolü**
   ```powershell
   netstat -an | findstr :5093
   ```

3. **Firewall Kontrolü**
   ```powershell
   Get-NetFirewallRule -DisplayName "*Inventory*"
   ```

### API'ye Erişim Yok

1. **Servis Durumu**
   ```powershell
   Get-Service -Name "InventoryManagementApi"
   ```

2. **Manuel Test**
   ```powershell
   # Console modda çalıştır
   cd "C:\Program Files\InventoryManagementSystem\Api"
   .\Inventory.Api.exe
   ```

### Agent Veri Göndermiyor

1. **Dependency Kontrolü**
   ```powershell
   # API'nin çalıştığından emin olun
   Get-Service -Name "InventoryManagementApi"
   ```

2. **Offline Storage Kontrolü**
   ```
   C:\Program Files\InventoryManagementSystem\Data\OfflineStorage\
   ```

## Kaldırma

### Script ile Kaldırma

```cmd
# Yönetici olarak çalıştırın
scripts\uninstall-windows-services.bat
```

### Manuel Kaldırma

```powershell
# Servisleri durdur
Stop-Service -Name "InventoryManagementAgent", "InventoryManagementApi" -Force

# Servisleri kaldır
sc.exe delete "InventoryManagementAgent"
sc.exe delete "InventoryManagementApi"

# Firewall kuralını kaldır
Remove-NetFirewallRule -DisplayName "Inventory Management API"

# Dosyaları kaldır (isteğe bağlı)
Remove-Item -Path "C:\Program Files\InventoryManagementSystem" -Recurse -Force
```

## Güvenlik Notları

- Servisler `LocalSystem` hesabı ile çalışır
- Port 5093 firewall'da açılır
- Hassas veriler için HTTPS kullanımı önerilir
- Production ortamında özel servis hesabı kullanımı önerilir

## İleri Düzey Yapılandırma

### Özel Servis Hesabı

```powershell
# Yeni kullanıcı oluştur
net user "InventoryService" "GüçlüŞifre123!" /add
net localgroup "Log on as a service" "InventoryService" /add

# Servis hesabını değiştir
sc.exe config "InventoryManagementApi" obj= ".\InventoryService" password= "GüçlüŞifre123!"
```

### HTTPS Yapılandırması

appsettings.json'da:
```json
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

### Performans Ayarları

```powershell
# Agent tarama sıklığını ayarla (dakika cinsinden)
[Environment]::SetEnvironmentVariable("InventoryAgent__ScanInterval", "30", [EnvironmentVariableTarget]::Machine)
```

## Destek ve Katkı

Sorunlar ve öneriler için GitHub Issues kullanın:
https://github.com/hizir-ceylan/InventoryManagementSystem/issues