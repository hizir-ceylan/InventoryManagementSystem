# Hızlı Başlangıç - Yerel Test

Bu rehber, Envanter Yönetim Sistemi'ni kendi bilgisayarınızda test etmek için hazırlanmıştır.

## Önkoşullar

1. **.NET 8.0 SDK** indirin ve kurun: https://dotnet.microsoft.com/download/dotnet/8.0
2. **Git** kurulu olmalı
3. **Terminal/Command Prompt** erişimi

## Kurulum Adımları

### 1. Projeyi İndirin
```bash
git clone https://github.com/hizir-ceylan/InventoryManagementSystem.git
cd InventoryManagementSystem
```

### 2. Projeyi Derleyin
```bash
dotnet build
```

### 3. API ve Web Arayüzünü Başlatın

#### Windows (PowerShell)
```powershell
# İki terminal penceresi açın

# Terminal 1 - API Servisi
cd Inventory.Api
dotnet run

# Terminal 2 - Web Arayüzü  
cd Inventory.WebApp
dotnet run --urls="http://localhost:5094"
```

#### Linux/Mac (Bash)
```bash
# İki terminal penceresi açın

# Terminal 1 - API Servisi
cd Inventory.Api
dotnet run

# Terminal 2 - Web Arayüzü
cd Inventory.WebApp
dotnet run --urls="http://localhost:5094"
```

### 4. Erişim Adresleri

- **🌐 Web Arayüzü**: http://localhost:5094
- **📊 API Dokümantasyonu**: http://localhost:5093/swagger
- **🔧 API Servisi**: http://localhost:5093

## Test Etme

1. **Web arayüzünü açın**: http://localhost:5094
2. **Cihazlar sekmesine** gidin
3. **Cihaz detayları butonunu** test edin
4. **Mobil uyumluluk** için tarayıcı geliştirici araçlarında mobil görünümünü test edin

## Sorunlar ve Çözümler

### Port Kullanımda Hatası
Portlar kullanımdaysa farklı portlar kullanın:
```bash
# API için
dotnet run --urls="http://localhost:5095"

# Web App için  
dotnet run --urls="http://localhost:5096"
```

### API Bağlantı Sorunu
Web arayüzünün API adresini güncelleyin:
`Inventory.WebApp/wwwroot/js/config.js` dosyasında:
```javascript
window.INVENTORY_CONFIG = {
    API_BASE_URL: 'http://localhost:5095', // Yeni API portunu buraya yazın
    // ...
};
```

### Veritabanı Sorunu
İlk çalıştırmada SQLite veritabanı otomatik oluşturulur. Sorun yaşarsanız:
```bash
cd Inventory.Api
rm -rf Data/  # Windows'ta: rmdir /s Data
dotnet run     # Veritabanı yeniden oluşturulur
```

## Özellikler

✅ **Ayrı Web Arayüzü**: API'den bağımsız çalışır
✅ **Responsive Tasarım**: Mobil ve tablet uyumlu
✅ **Cihaz Detayları**: Functional butonlar
✅ **Otomatik Yenileme**: 30 saniyede bir güncellenir
✅ **Mock Veri Desteği**: API olmadan da demo çalışır

## Üretim Ortamı İçin

Gerçek sunucuya kurmak için **DEPLOYMENT-GUIDE.md** dosyasını inceleyin.

---

**İpucu**: Ctrl+C ile servisleri durdurabilirsiniz.