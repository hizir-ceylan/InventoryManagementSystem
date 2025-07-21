# Inventory Management System - Güncellemeler ve Kurulum Özeti

## Çözülen Sorunlar

### ✅ Hafta Sonu Logging Sorunu Çözüldü
**Problem**: Cuma günü oluşturulan loglar, pazartesi çalıştığında siliniyordu.
**Çözüm**: 
- Günlük loglamadan saatlik loglamaya geçildi
- 48 saatlik sürgülü pencere ile doğru saklama
- Dosya formatı: `device-log-2024-01-15-14.json` (saat dahil)

### ✅ Saatlik Loglama Eklendi
- Her saat başı log oluşturma
- 2 günlük (48 saat) saklama süresi
- Otomatik eski dosya temizleme

### ✅ Konfigürasyon Güncellendi
```json
{
  "Agent": {
    "LoggingInterval": "01:00:00",
    "LogRetentionHours": 48,
    "EnableHourlyLogging": true
  }
}
```

## Yeni Dokümantasyon

### 📚 Kurulum Rehberi (`docs/installation-guide.md`)
- Sistem gereksinimleri
- Adım adım kurulum (Windows/Linux)
- Agent dağıtım yöntemleri
- Sorun giderme rehberi

### 🚀 Sunucu Kurulumu (`docs/server-deployment-testing.md`)
- Hızlı başlangıç (1 dakikada test)
- Detaylı sunucu kurulumu
- API test senaryoları
- Gerçek veri testi

### 🗄️ Veritabanı Kurulumu (`database/setup-database.sql`)
- Tam SQL Server şeması
- Örnek veri ile test
- İndeksler ve optimizasyonlar
- Otomatik temizleme

## Hızlı Başlangıç

### 1. Test Ortamı (SQLite ile)
```bash
git clone https://github.com/hizir-ceylan/InventoryManagementSystem.git
cd InventoryManagementSystem/Inventory.Api
dotnet run --urls="http://0.0.0.0:5000"
```
Swagger UI: `http://localhost:5000/swagger`

### 2. Veritabanı Kurulumu
```sql
-- SQL Server'da çalıştır
sqlcmd -S localhost -U SA -P 'StrongPassword123!' -i database/setup-database.sql
```

### 3. API Test
```bash
# Cihaz listesi
curl http://localhost:5000/api/device

# Test cihazı ekle
curl -X POST http://localhost:5000/api/device \
  -H "Content-Type: application/json" \
  -d '{"name":"Test-PC","macAddress":"00:1B:44:11:3A:B7","ipAddress":"192.168.1.100","deviceType":"PC"}'
```

### 4. Agent Kurulumu
```bash
cd Inventory.Agent.Windows
dotnet build --configuration Release
# Çıktıyı C:\InventoryAgent'a kopyala
# Windows Service olarak kur
sc create "InventoryAgent" binPath="C:\InventoryAgent\Inventory.Agent.Windows.exe"
```

## Dosya Yapısı

```
InventoryManagementSystem/
├── Inventory.Api/              # Web API
├── Inventory.Agent.Windows/    # Windows Agent
├── Inventory.Domain/           # Entity modelleri
├── Inventory.Data/            # Veri erişim katmanı
├── Inventory.Shared/          # Ortak kütüphaneler
├── docs/
│   ├── installation-guide.md     # Kurulum rehberi
│   ├── server-deployment-testing.md  # Sunucu kurulumu
│   └── technical-documentation.md    # Teknik dokümantasyon
├── database/
│   └── setup-database.sql        # Veritabanı kurulum scripti
└── test-logging.sh              # Logging test scripti
```

## Connection String Örnekleri

### SQL Server
```
Server=localhost;Database=InventoryDB;User Id=inventoryuser;Password=StrongPassword123!;TrustServerCertificate=true;
```

### SQLite (Test)
```
Data Source=inventory.db
```

### PostgreSQL
```
Server=localhost;Database=inventorydb;User Id=inventoryuser;Password=StrongPassword123!;
```

## Önemli Notlar

### Logging Değişiklikleri
- **Eski format**: `device-log-2024-01-15.json`
- **Yeni format**: `device-log-2024-01-15-14.json`
- **Saklama**: 48 saat (hafta sonları dahil)
- **Temizlik**: Otomatik, saatlik çalışma

### Agent Konfigürasyonu
```json
{
  "ApiSettings": {
    "BaseUrl": "https://your-api-server.com",
    "Timeout": 30
  },
  "Agent": {
    "ScanInterval": "01:00:00",
    "LogRetentionHours": 48
  }
}
```

### API Endpoint'leri
- `GET /api/device` - Cihaz listesi
- `POST /api/device` - Yeni cihaz
- `GET /swagger` - API dokümantasyonu
- `POST /api/logging` - Log gönderimi

## Test ve Doğrulama

### Logging Testi
```bash
./test-logging.sh
```

### API Testi
```bash
# Otomatik test scripti mevcuttur (server-deployment-testing.md içinde)
./automated_test.sh http://your-server-ip
```

### Veritabanı Testi
```sql
SELECT COUNT(*) FROM Devices;
SELECT COUNT(*) FROM ChangeLogs;
```

## Sonraki Adımlar

1. **Production Deployment**:
   - IIS/Nginx yapılandırması
   - SSL sertifikası kurulumu
   - Firewall kuralları

2. **Monitoring**:
   - Log analizi
   - Performance izleme
   - Health check'ler

3. **Backup**:
   - Veritabanı yedekleme
   - Konfigürasyon yedekleme
   - Otomatik backup scriptleri

## Destek ve Sorun Giderme

- **Kurulum sorunları**: `docs/installation-guide.md` - Sorun Giderme bölümü
- **API sorunları**: `docs/server-deployment-testing.md` - Test senaryoları
- **Agent sorunları**: Event Viewer'da "InventoryAgent" loglarını kontrol edin
- **Veritabanı sorunları**: Connection string ve SQL Server servislerini kontrol edin

## Özet

✅ **Hafta sonu logging sorunu çözüldü** - Cuma logları artık pazartesi silinmiyor
✅ **Saatlik loglama eklendi** - Her saat log, 48 saat saklama
✅ **Kapsamlı dokümantasyon** - Kurulum, deployment, test rehberleri
✅ **Veritabanı kurulum scripti** - Tek tıkla tam kurulum
✅ **Test araçları** - Otomatik test ve doğrulama scriptleri

Sistem artık production ortamında kullanıma hazır! 🚀