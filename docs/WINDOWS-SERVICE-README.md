# Windows Service Kurulum - Hızlı Başlangıç

> **Detaylı Kurulum Rehberi**: Build alma ve derleme dahil tüm adımlar için [Windows Tam Kurulum Rehberi](docs/WINDOWS-INSTALLATION-GUIDE.md) sayfasına bakın.

> **Service Sorun Giderme**: Servis başlatma sorunları için [Windows Service Troubleshooting](WINDOWS-SERVICE-TROUBLESHOOTING.md) rehberine bakın.

## Problem Çözümü

**SORUN**: Agent çalıştırıldığında "Hedef makine etkin olarak reddettiğinden bağlantı kurulamadı (localhost:5093)" hatası alınıyor.

**ÇÖZÜM**: API ve Agent'ın Windows servisi olarak otomatik başlatılması.

## Service Geliştirmeleri (v1.1)

✅ **Hızlı Başlatma**: Service 5 saniye içinde başlar (önceden 30+ saniye)  
✅ **Gelişmiş Hata Yönetimi**: Service başlatma hatalarında otomatik kurtarma  
✅ **Çoklu Loglama**: Event Log + Dosya tabanlı log sistemi  
✅ **Otomatik Yeniden Başlatma**: Service çökme durumunda otomatik restart  
✅ **Arka Plan İşlemleri**: Ağır işlemler service startup'ı engellemez  
✅ **Service Yönetim Aracı**: Kolay service yönetimi için araç eklendi

## Hızlı Kurulum (Hazır Build ile)

### Gereksinimler
- .NET 8.0 Runtime
- Yönetici yetkileri
- Port 5093'ün açık olması

### 1. .NET 8 Runtime Kurulumu
```powershell
# Otomatik kurulum
winget install Microsoft.DotNet.Runtime.8

# Manuel kurulum: https://dotnet.microsoft.com/download/dotnet/8.0
# "Run apps - Runtime" bölümünden "Download x64" seçin
```

### 2. Projeyi İndirin
```powershell
git clone https://github.com/hizir-ceylan/InventoryManagementSystem.git
cd InventoryManagementSystem
```

### 3. PowerShell'i Yönetici Olarak Açın
- Windows tuşu + X
- "Windows PowerShell (Administrator)" seçin

### 4. Kurulum Scriptini Çalıştırın
```powershell
cd "C:\YourProject\InventoryManagementSystem"
.\scripts\Install-WindowsServices.ps1
```

### 5. Test Edin
- Tarayıcıda açın: http://localhost:5093/swagger
- Servis durumunu kontrol edin: `services.msc`

## Ne Olur?

✅ **API Servisi**: Windows açıldığında otomatik başlar (Port 5093)  
✅ **Agent Servisi**: API'den 10 saniye sonra başlar  
✅ **Dependency**: Agent, API'ye bağımlı olarak yapılandırılır  
✅ **Otomatik Yeniden Başlatma**: Hata durumunda servisler otomatik yeniden başlar  
✅ **Event Logging**: Sistem logları Windows Event Viewer'da görünür  

## Yönetim

### Grafik Arayüz
```
Win + R → services.msc
```
"Inventory Management" servislerini bulun.

### PowerShell
```powershell
# Durum kontrol
Get-Service -Name "InventoryManagement*"

# Başlat/Durdur
Start-Service -Name "InventoryManagementApi"
Stop-Service -Name "InventoryManagementAgent"
```

### Script ile Yönetim
```cmd
scripts\manage-services.bat
```

## Sorun Giderme

### Servis Başlamıyor?
1. Event Viewer'ı açın: `eventvwr.msc`
2. Windows Logs → Application 
3. "InventoryManagement" kaynaklı hataları kontrol edin

### Port Çakışması?
```powershell
netstat -an | findstr :5093
```

### Firewall Sorunu?
```powershell
Get-NetFirewallRule -DisplayName "*Inventory*"
```

## Kaldırma

```cmd
# Yönetici olarak
.\scripts\uninstall-windows-services.bat
```

---

## Detaylı Rehber

Bu sayfa sadece hızlı kurulum için. **Detaylı adımlar**, **troubleshooting**, **build alma** ve **gelişmiş yapılandırma** için:

➡️ **[Windows Tam Kurulum Rehberi](docs/WINDOWS-INSTALLATION-GUIDE.md)**

**Sonuç**: Artık bilgisayar her açıldığında API ve Agent otomatik olarak arka planda çalışacak!