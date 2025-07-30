# Inventory Management System

Kurumsal cihaz envanteri yönetimi, değişiklik takibi ve raporlaması için geliştirilen profesyonel bir sistem.

## Özellikler

- **Cihaz Yönetimi**: Donanım ve yazılım bilgilerinin otomatik toplama ve takibi
- **Çoklu Platform Desteği**: Windows ve Linux ortamlarında çalışma
- **RESTful API**: Swagger/OpenAPI dokümantasyonu ile gelişmiş API
- **Windows Service**: Otomatik başlangıç ve arka plan çalışma desteği
- **Docker Desteği**: Konteyner tabanlı kolay deployment
- **Ağ Keşfi**: Otomatik cihaz bulma ve kaydetme
- **Değişiklik Takibi**: Sistem değişikliklerinin otomatik loglanması
- **Çoklu Veritabanı**: SQLite, SQL Server ve PostgreSQL desteği

## Kurulum

### Hızlı Başlangıç

**Docker ile (Önerilen):**
```bash
git clone https://github.com/hizir-ceylan/InventoryManagementSystem.git
cd InventoryManagementSystem
docker-compose up --build -d
```

**Windows Service Kurulumu:**
```powershell
# Yönetici PowerShell'de
cd build-tools
.\Build-Setup.ps1
```

**API Erişimi:**
- http://localhost:5093/swagger

### Sistem Gereksinimleri
- .NET 8.0 Runtime
- Windows 10/11 veya Linux
- Docker (isteğe bağlı)
- 2GB RAM (minimum)

## API Kullanımı

### Örnek İstekler

**Cihaz Ekleme:**
```bash
curl -X POST "http://localhost:5093/api/device" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "TEST-PC-001",
    "macAddress": "00:1B:44:11:3A:B7",
    "ipAddress": "192.168.1.100",
    "deviceType": "PC",
    "model": "Dell OptiPlex",
    "location": "Office-101",
    "status": 0
  }'
```

**Cihaz Listeleme:**
```bash
curl http://localhost:5093/api/device
```

**Ağ Tarama:**
```bash
curl -X POST "http://localhost:5093/api/networkscan/start" \
  -H "Content-Type: application/json" \
  -d '{"networkRange": "192.168.1.0/24"}'
```

### Swagger UI
Detaylı API dokümantasyonu için: http://localhost:5093/swagger

## Proje Yapısı

```
InventoryManagementSystem/
├── Inventory.Api/           # Web API projesi
├── Inventory.Agent.Windows/ # Windows Agent
├── Inventory.Data/          # Entity Framework Data katmanı
├── Inventory.Domain/        # Domain modelleri
├── Inventory.Shared/        # Paylaşılan sınıflar
├── build-tools/            # Build ve deployment scriptleri
├── docs/                   # Teknik dokümantasyon
├── database/               # Database kurulum scriptleri
├── nginx/                  # NGINX konfigürasyonu
└── docker-compose.yml      # Docker orchestration
```

## Dokümantasyon

- **[Teknik Dokümantasyon](docs/TEKNIK-DOKUMANTASYON.md)** - Kapsamlı teknik rehber
- **[Docker Rehberi](docs/DOCKER-GUIDE.md)** - Docker kurulum ve kullanım
- **[Windows Kurulum](docs/WINDOWS-INSTALLATION-GUIDE.md)** - Windows service kurulum
- **[Veri Kalıcılığı Kılavuzu](docs/DATA-PERSISTENCE-GUIDE.md)** - Veri depolama ve log yönetimi

## Agent verilerinizi kalıcı dizinlerde depolar:

**Windows Service Modunda (Önerilen - Yönetici Kurulumu):**
- **Veritabanı**: `C:\ProgramData\Inventory Management System\Data\inventory.db`
- **Offline Veriler**: `C:\ProgramData\Inventory Management System\OfflineStorage\`
- **Yerel Loglar**: `C:\ProgramData\Inventory Management System\Logs\`

**Manuel Kullanıcı Modunda:**
- **Offline Veriler**: `Documents/InventoryManagementSystem/OfflineStorage/`
- **Yerel Loglar**: `Documents/InventoryManagementSystem/LocalLogs/`

Detaylı bilgi için [Veri Kalıcılığı Kılavuzu](docs/DATA-PERSISTENCE-GUIDE.md)'na bakın.

## Lisans

MIT lisansı ile açık kaynak olarak sunulmaktadır.

## Destek

Herhangi bir sorun için [issue açabilirsiniz](https://github.com/hizir-ceylan/InventoryManagementSystem/issues).