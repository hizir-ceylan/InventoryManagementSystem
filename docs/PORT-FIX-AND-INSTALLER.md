# Port Tutarlılığı ve Kolay Kurulum Güncellemesi

## 🎯 Çözülen Problemler

### 1. Port Tutarsızlığı Problemi ✅
**Problem**: API geliştirme ortamında 5093 portunda çalışıyor ama Docker'da 5000 portunda erişiliyor.

**Çözüm**: Tüm sistem 5093 portunu kullanacak şekilde güncellendi:
- ✅ Docker Compose dosyaları güncellendi
- ✅ Dockerfile portu düzeltildi  
- ✅ Tüm dokümantasyon güncellendi
- ✅ Test scriptleri güncellendi
- ✅ Agent konfigürasyonu zaten 5093 kullanıyordu

### 2. Çoklu Bilgisayar Kurulum Zorluğu ✅
**Problem**: GitHub'dan kod çekip PowerShell komutları girmek zor.

**Çözüm**: Tek tıkla kurulum sistemi oluşturuldu:
- ✅ `Quick-Install.bat` - Tek tıkla kurulum dosyası
- ✅ `Install-InventorySystem.ps1` - Tam otomatik PowerShell kurulumu
- ✅ Otomatik bağımlılık yönetimi (Git, .NET 8 SDK)
- ✅ Windows Service olarak otomatik kurulum
- ✅ Desktop kısayolları
- ✅ Kolay kaldırma scripti

## 🚀 Artık Nasıl Kurulur

### Süper Kolay Yöntem (Önerilen)
1. **[Quick-Install.bat](Quick-Install.bat)** dosyasını indirin
2. Sağ tıklayıp **"Yönetici olarak çalıştır"** seçin  
3. 5-10 dakika bekleyin
4. Tamamlandı! → http://localhost:5093/swagger

### PowerShell ile Tek Komut
```powershell
# Yönetici PowerShell'de:
iex ((New-Object System.Net.WebClient).DownloadString('https://raw.githubusercontent.com/hizir-ceylan/InventoryManagementSystem/main/build-tools/Install-InventorySystem.ps1'))
```

## 📊 Kurulum Özellikleri

### Otomatik Yapılanlar
- 🔧 **Git ve .NET 8 SDK** otomatik indirilir ve kurulur
- 📥 **GitHub'dan en son kod** otomatik çekilir
- 🔨 **Proje derlenir** ve Release modunda yayınlanır
- ⚙️ **Windows Servisleri** kurulur ve başlatılır
- 🖥️ **Desktop kısayolları** oluşturulur
- 🗑️ **Kaldırma scripti** hazırlanır

### Servis Yapılandırması
- **API Servisi**: `InventoryManagementApi` 
- **Agent Servisi**: `InventoryManagementAgent`
- **Otomatik Başlatma**: Windows başlangıcında
- **Bağımlılık**: Agent, API'nin başlamasını bekler

## 🌐 Çoklu Bilgisayar Kurulumu

### IT Yöneticileri İçin
```powershell
# Birden çok bilgisayara uzaktan kurulum
$computers = @("PC001", "PC002", "PC003", "PC004")

Invoke-Command -ComputerName $computers -ScriptBlock {
    iex ((New-Object System.Net.WebClient).DownloadString('https://raw.githubusercontent.com/hizir-ceylan/InventoryManagementSystem/main/build-tools/Install-InventorySystem.ps1'))
}
```

### Network Share Kullanımı
1. **Quick-Install.bat**'ı network share'e koyun
2. Grup Politikası ile dağıtın
3. Veya uzak bilgisayarlarda çalıştırın

## 🔧 Port Değişiklikleri Detayı

### Değiştirilen Dosyalar
- `docker-compose.yml`: Port mapping 5000:5000 → 5093:5093
- `docker-compose.simple.yml`: Port ve environment variables
- `Dockerfile`: EXPOSE 5000 → EXPOSE 5093  
- `README.md`: Tüm URL örnekleri güncellendi
- `build-tools/test-docker.sh`: API URL güncellendi

### Artık Her Yerde 5093 Portu
- **Development**: http://localhost:5093
- **Docker**: http://localhost:5093  
- **Production**: http://localhost:5093
- **Agent**: http://localhost:5093 (zaten böyleydi)

## 🆘 Sorun Giderme

### Kurulum Sorunları
```powershell
# PowerShell execution policy hatası
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser

# Yönetici hakları gerekli
# Sağ tıklayıp "Yönetici olarak çalıştır" kullanın
```

### Servis Sorunları
```powershell
# Servisleri kontrol et
Get-Service -Name "InventoryManagement*"

# Servisleri yeniden başlat
Restart-Service -Name "InventoryManagementApi"
Restart-Service -Name "InventoryManagementAgent"

# Log dosyalarını kontrol et
Get-EventLog -LogName Application -Source "InventoryManagement*" -Newest 10
```

### Port Sorunları
```powershell
# Port 5093'ü kim kullanıyor?
netstat -aon | findstr :5093

# Windows Firewall'ı kontrol et
New-NetFirewallRule -DisplayName "Inventory API" -Direction Inbound -Port 5093 -Protocol TCP -Action Allow
```

## 📋 Test Checklist

### Kurulum Sonrası Test
- [ ] Servisler çalışıyor mu? → `Get-Service -Name "InventoryManagement*"`
- [ ] API erişilebilir mi? → http://localhost:5093/swagger
- [ ] Agent veri gönderiyor mu? → API'de cihaz var mı kontrol et
- [ ] Desktop kısayolları çalışıyor mu?

### Port Tutarlılığı Test
- [ ] Development: `dotnet run` → http://localhost:5093
- [ ] Docker: `docker-compose up` → http://localhost:5093  
- [ ] Agent: 5093 portuna bağlanıyor mu?

## 🎉 Sonuç

✅ **Port tutarlılığı problemi çözüldü**  
✅ **Tek tıkla kurulum sistemi hazır**  
✅ **Çoklu bilgisayar kurulumu çok kolay**  
✅ **Otomatik servis yönetimi**  
✅ **Kolay kaldırma imkanı**

**Ana erişim adresi artık her zaman**: http://localhost:5093/swagger