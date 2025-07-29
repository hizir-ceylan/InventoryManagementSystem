# Değişiklik Geçmişi

## Son Güncellemeler ve Düzeltmeler

### ✅ Hafta Sonu Log Sorunu Çözüldü
**Problem**: Cuma günü oluşturulan loglar Pazartesi günü sistem çalıştığında siliniyordu.
**Çözüm**: 
- Günlük log sisteminden saatlik log sistemine geçiş
- 48 saatlik kayan pencere sistemi uygulandı
- Dosya formatı: `device-log-2024-01-15-14.json` (saat bilgisi dahil)

### ✅ Saatlik Loglama Eklendi
- Her saat log oluşturma
- 48 saatlik (2 günlük) saklama süresi
- Eski dosyaların otomatik temizliği

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

### 📚 Kurulum Rehberi
- Sistem gereksinimleri
- Adım adım kurulum (Windows/Linux)
- Agent deployment yöntemleri
- Sorun giderme rehberi

### 🚀 Sunucu Kurulumu
- Hızlı başlangıç (1 dakikalık test)
- Detaylı sunucu kurulumu
- API test senaryoları
- Gerçek veri testleri

### 🗄️ Veritabanı Kurulumu
- Komple SQL Server şeması
- Test için örnek veriler
- İndeksler ve optimizasyonlar
- Otomatik temizlik

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

### 3. API Testi
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
├── Inventory.Shared/          # Paylaşılan kütüphaneler
├── docs/
│   ├── TEKNIK-DOKUMANTASYON.md    # Teknik dokümantasyon
│   ├── WINDOWS-INSTALLATION-GUIDE.md  # Windows kurulum
│   └── DOCKER-GUIDE.md               # Docker rehberi
├── database/
│   └── setup-database.sql        # Veritabanı kurulum scripti
└── build-tools/                      # Yardımcı scriptler
    ├── quick-start.sh            # Hızlı başlangıç scripti
    ├── test-docker.sh            # Docker test
    └── test-logging.sh           # Log testleri
```

## Bağlantı String Örnekleri

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

## Important Notes

### Logging Changes
- **Old format**: `device-log-2024-01-15.json`
- **New format**: `device-log-2024-01-15-14.json`
- **Retention**: 48 hours (includes weekends)
- **Cleanup**: Automatic, hourly operation

### Agent Configuration
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

### API Endpoints
- `GET /api/device` - Device list
- `POST /api/device` - New device
- `GET /swagger` - API documentation
- `POST /api/logging` - Log submission

## Testing and Validation

### Logging Tests
```bash
./build-tools/test-logging.sh
```

### API Tests
```bash
# Automated test script available (see server-deployment-testing.md)
./build-tools/automated_test.sh http://your-server-ip
```

### Database Tests
```sql
SELECT COUNT(*) FROM Devices;
SELECT COUNT(*) FROM ChangeLogs;
```

## Next Steps

1. **Production Deployment**:
   - IIS/Nginx configuration
   - SSL certificate setup
   - Firewall rules

2. **Monitoring**:
   - Log analysis
   - Performance monitoring
   - Health checks

3. **Backup**:
   - Database backup
   - Configuration backup
   - Automated backup scripts

## Support and Troubleshooting

- **Installation issues**: `installation-guide.md` - Troubleshooting section
- **API issues**: `server-deployment-testing.md` - Test scenarios
- **Agent issues**: Check Event Viewer "InventoryAgent" logs
- **Database issues**: Check connection string and SQL Server services

## Summary

✅ **Weekend logging issue fixed** - Friday logs no longer deleted on Monday
✅ **Hourly logging added** - Hourly logs, 48-hour retention
✅ **Comprehensive documentation** - Installation, deployment, test guides
✅ **Database setup script** - One-click complete setup
✅ **Testing tools** - Automated test and validation scripts

The system is now ready for production use! 🚀