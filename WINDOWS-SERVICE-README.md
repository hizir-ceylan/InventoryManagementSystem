# Windows Service Kurulumu - Hızlı Başlangıç

## Problem Çözümü

**SORUN**: Agent çalıştırıldığında "Hedef makine etkin olarak reddettiğinden bağlantı kurulamadı (localhost:5093)" hatası alınıyor.

**ÇÖZÜM**: API ve Agent'ın Windows servisi olarak otomatik başlatılması.

## Hızlı Kurulum (3 Adım)

### 1. PowerShell'i Yönetici Olarak Açın
- Windows tuşu + X
- "Windows PowerShell (Administrator)" seçin

### 2. Kurulum Scriptini Çalıştırın
```powershell
cd "C:\YourProject\InventoryManagementSystem"
.\scripts\Install-WindowsServices.ps1
```

### 3. Test Edin
- Tarayıcıda açın: http://localhost:5093/swagger
- Servis durumunu kontrol edin: `services.msc`

## Ne Olur?

✅ **API Servisi**: Windows açıldığında otomatik başlar (Port 5093)  
✅ **Agent Servisi**: API'den 10 saniye sonra başlar  
✅ **Dependency**: Agent, API'ye bağımlı olarak yapılandırılır  
✅ **Otomatik Yeniden Başlatma**: Hata durumunda servisler otomatik yeniden başlar  
✅ **Event Logging**: Sistem logları Windows Event Viewer'da görünür  

## Servisleri Yönet

### Grafik Arayüz
```
Win + R → services.msc
```
"Inventory Management" servislerini bulun.

### PowerShell
```powershell
# Durum kontrol
Get-Service -Name "InventoryManagement*"

# Başlat
Start-Service -Name "InventoryManagementApi"
Start-Service -Name "InventoryManagementAgent"

# Durdur  
Stop-Service -Name "InventoryManagementAgent"
Stop-Service -Name "InventoryManagementApi"
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
# Firewall kuralını kontrol et
Get-NetFirewallRule -DisplayName "*Inventory*"
```

## Kaldırma

```powershell
# Yönetici olarak
.\scripts\uninstall-windows-services.bat
```

## Detaylı Bilgi

Tam dokümantasyon: `docs/windows-service-setup.md`

---

**Sonuç**: Artık bilgisayar her açıldığında API ve Agent otomatik olarak arka planda çalışacak!