# InventoryManagementSystem

Kurumsal cihaz envanteri yönetimi, değişiklik takibi ve raporlaması için geliştirilen bir sistemdir.

## ✨ Özellikler

### 🔧 Windows Service Desteği
**Problem**: Agent çalıştırıldığında "Hedef makine etkin olarak reddettiğinden bağlantı kurulamadı" hatası  
**Çözüm**: API ve Agent'ın Windows servisi olarak otomatik başlatılması

✅ **Windows başlangıcında otomatik start**  
✅ **API önce, Agent sonra başlar**  
✅ **Arka planda sürekli çalışır**  
✅ **Event Log entegrasyonu**  

### 🔧 Temel Özellikler
- Cihaz ekleme, listeleme, güncelleme ve silme
- Donanım & yazılım bilgilerini toplama
- Kullanıcı ve lokasyon yönetimi
- Değişikliklerin otomatik takibi ve raporlanması
- API ile farklı uygulamalardan entegrasyon imkanı

### 🐳 Docker Desteği
- **Multi-stage Docker build** ile optimize edilmiş containerlar
- **SQLite** ve **SQL Server** database desteği
- **Nginx reverse proxy** ile production-ready setup
- **Otomatik test scriptleri** ile kolay doğrulama
- **Cross-platform** çalıştırma (Windows/Linux)

### 🌐 Platform Desteği
- **Windows Agent**: WMI tabanlı detaylı sistem bilgisi
- **Linux Agent**: Proc filesystem ve system commands
- **Network Discovery**: Otomatik cihaz keşfi
- **Change Logging**: Ayrı dosyalarda değişiklik takibi

## 🚀 Kurulum

### Windows için Otomatik Kurulum (Önerilen)

**Gereksinimler:**
- Windows 10/11 veya Windows Server 2016 veya daha yeni
- Yönetici yetkileri
- .NET 8 Runtime (otomatik yüklenecek)

**Kurulum Adımları:**

1. **Repository'yi klonlayın:**
   ```bash
   git clone https://github.com/hizir-ceylan/InventoryManagementSystem.git
   cd InventoryManagementSystem
   ```

2. **Build ve kurulum scriptini çalıştırın (Yönetici PowerShell'de):**
   ```powershell
   cd build-tools
   .\Build-Setup.ps1
   ```

3. **Setup.exe ile kurulum yapın:**
   - `Setup\InventoryManagementSystem-Setup.exe` dosyasını çalıştırın
   - Kurulum sihirbazını takip edin
   - Servisler otomatik olarak başlatılacak

4. **API'ye erişin:**
   - http://localhost:5093/swagger

### Docker ile Hızlı Test

```bash
# Hızlı başlangıç scripti
./build-tools/quick-start.sh

# Manuel Docker Compose
docker-compose -f docker-compose.simple.yml up --build -d

# Production setup
docker-compose up --build -d
```

## 🧪 API Test Örnekleri

### Cihaz Ekleme
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

### Cihaz Listeleme
```bash
curl http://localhost:5093/api/device
```

### Network Scan Başlatma
```bash
curl -X POST "http://localhost:5093/api/networkscan/start" \
  -H "Content-Type: application/json" \
  -d '{"networkRange": "192.168.1.0/24"}'
```

## 🎯 Hızlı Test Senaryoları

### Docker Environment Test
```bash
# 1. Sistem başlat
./build-tools/quick-start.sh

# 2. API test et
curl http://localhost:5093/api/device

# 3. Tam test suite çalıştır
./build-tools/test-docker.sh test

# 4. Performans testi
ab -n 100 -c 10 http://localhost:5093/api/device
```

### Manuel Test
1. **Swagger UI**: http://localhost:5093/swagger adresine gidin
2. **Device endpoints**'ini test edin
3. **Logging endpoints**'ini test edin
4. **Network scan**'i başlatın

## 🛠️ Geliştirme

### Development Environment
```bash
# Development mode
docker run -p 5093:5093 -e ASPNETCORE_ENVIRONMENT=Development inventory-api:latest

# Hot reload development
dotnet watch run --project Inventory.Api --environment Development
```

### Troubleshooting
```bash
# Container logları
docker-compose logs -f inventory-api

# Database kontrol
sqlite3 ./Data/SQLite/inventory.db "SELECT * FROM Devices;"

# Network kontrol
docker network ls
docker network inspect inventory_inventory-network
```

## 📊 Monitoring ve Loglar

### Windows Service Yönetimi
```powershell
# Servis durumunu kontrol et
Get-Service -Name "InventoryManagementApi", "InventoryManagementAgent"

# Servisleri yeniden başlat
Restart-Service -Name "InventoryManagementApi"
Restart-Service -Name "InventoryManagementAgent"

# Event logları kontrol et
Get-EventLog -LogName Application -Source "InventoryManagementAgent" -Newest 10
```

### Container Monitoring
```bash
# Resource kullanımı
docker stats inventory-api-simple

# Health check
curl http://localhost:5093/api/device
```

### Log Dosyaları
- API Logs: `./Data/ApiLogs/`
- Agent Logs: `./Data/AgentLogs/`
- Change Logs: `./Data/AgentLogs/Changes/`

## 📖 Dokümantasyon

- 🐳 **[Docker Rehberi](docs/DOCKER-GUIDE.md)** - Docker kurulum ve test rehberi
- 📋 **[Tüm Dokümantasyon](docs/COMPLETE-DOCUMENTATION.md)** - Kapsamlı teknik dokümantasyon
- 🔧 **[Windows Service Kurulum](docs/windows-service-setup.md)** - Detaylı Windows service kurulum rehberi

## Katkıda Bulunmak

Projeye katkı sağlamak için:
- Fork'layın ve yeni bir branch oluşturun
- Docker setup'ını test edin
- Değişikliklerinizi ekleyin ve test edin
- Pull request açın

### Development Guidelines
- Docker build'in çalıştığından emin olun
- Test scriptlerini çalıştırın
- Dokümantasyonu güncelleyin

## Lisans

MIT lisansı ile açık kaynak olarak sunulmaktadır.

---

## 📁 Repository Structure

```
InventoryManagementSystem/
├── Inventory.Api/           # Web API projesi
├── Inventory.Agent.Windows/ # Windows Agent projesi
├── Inventory.Data/          # Data katmanı (Entity Framework)
├── Inventory.Domain/        # Domain modelleri
├── Inventory.Shared/        # Paylaşılan sınıflar
├── build-tools/            # Build, deployment ve kurulum scriptleri
├── docs/                   # Tüm dokümantasyon dosyaları
├── database/               # Database kurulum scriptleri
├── nginx/                  # NGINX konfigürasyonu
├── docker-compose.yml      # Docker compose dosyası
├── Dockerfile             # API için Docker dosyası
├── Dockerfile.agent       # Agent için Docker dosyası
└── README.md              # Bu dosya
```

### Key Directories:
- **build-tools/**: Build ve kurulum scriptleri
- **docs/**: Detaylı dokümantasyon ve kurulum rehberleri
- **database/**: Database kurulum ve başlangıç scriptleri

---

### 🆘 Destek

- **Windows kurulum problemleri**: [Windows Service Kurulum](docs/windows-service-setup.md)
- **Docker problemleri**: [Docker Rehberi](docs/DOCKER-GUIDE.md)
- **API kullanımı**: http://localhost:5093/swagger (Tüm platformlar)
- **Tüm dokümantasyon**: [Tam Dokümantasyon](docs/COMPLETE-DOCUMENTATION.md)

Her türlü soru ve öneriniz için lütfen [issue açın](https://github.com/hizir-ceylan/InventoryManagementSystem/issues) veya iletişime geçin.

## 🧪 API Test Örnekleri

### Cihaz Ekleme
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

### Cihaz Listeleme
```bash
curl http://localhost:5093/api/device
```

### Network Scan Başlatma
```bash
curl -X POST "http://localhost:5093/api/networkscan/start" \
  -H "Content-Type: application/json" \
  -d '{"networkRange": "192.168.1.0/24"}'
```

## 🎯 Hızlı Test Senaryoları

### Docker Environment Test
```bash
# 1. Sistem başlat
./build-tools/quick-start.sh

# 2. API test et
curl http://localhost:5093/api/device

# 3. Tam test suite çalıştır
./build-tools/test-docker.sh test

# 4. Performans testi
ab -n 100 -c 10 http://localhost:5093/api/device
```

### Manuel Test
1. **Swagger UI**: http://localhost:5093/swagger adresine gidin
2. **Device endpoints**'ini test edin
3. **Logging endpoints**'ini test edin
4. **Network scan**'i başlatın

## 🛠️ Geliştirme

### Development Environment
```bash
# Development mode
docker run -p 5093:5093 -e ASPNETCORE_ENVIRONMENT=Development inventory-api:latest

# Hot reload development
dotnet watch run --project Inventory.Api --environment Development
```

### Troubleshooting
```bash
# Container logları
docker-compose logs -f inventory-api

# Database kontrol
sqlite3 ./Data/SQLite/inventory.db "SELECT * FROM Devices;"

# Network kontrol
docker network ls
docker network inspect inventory_inventory-network
```

## 📊 Monitoring ve Loglar

### Container Monitoring
```bash
# Resource kullanımı
docker stats inventory-api-simple

# Health check
curl http://localhost:5093/api/device
```

### Log Dosyaları
- API Logs: `./Data/ApiLogs/`
- Agent Logs: `./Data/AgentLogs/`
- Change Logs: `./Data/AgentLogs/Changes/`

## Katkıda Bulunmak

Projeye katkı sağlamak için:
- Fork'layın ve yeni bir branch oluşturun
- Docker setup'ını test edin
- Değişikliklerinizi ekleyin ve test edin
- Pull request açın

### Development Guidelines
- Docker build'in çalıştığından emin olun
- Test scriptlerini çalıştırın
- Dokümantasyonu güncelleyin

## Lisans

MIT lisansı ile açık kaynak olarak sunulmaktadır.

---

## 📁 Repository Structure

```
InventoryManagementSystem/
├── Inventory.Api/           # Web API projesi
├── Inventory.Agent.Windows/ # Windows Agent projesi
├── Inventory.Data/          # Data katmanı (Entity Framework)
├── Inventory.Domain/        # Domain modelleri
├── Inventory.Shared/        # Paylaşılan sınıflar
├── build-tools/            # Build, deployment ve kurulum scriptleri
├── docs/                   # Tüm dokümantasyon dosyaları
├── database/               # Database kurulum scriptleri
├── nginx/                  # NGINX konfigürasyonu
├── Published/              # Yayın dosyaları
├── docker-compose.yml      # Docker compose dosyası
├── Dockerfile             # API için Docker dosyası
├── Dockerfile.agent       # Agent için Docker dosyası
└── README.md              # Bu dosya
```

### Key Directories:
- **build-tools/**: Tüm build, kurulum ve deployment scriptleri
- **docs/**: Detaylı dokümantasyon ve kurulum rehberleri
- **database/**: Database kurulum ve başlangıç scriptleri

---

### 🆘 Destek

- **Windows kurulum problemleri**: [Windows Tam Kurulum Rehberi](docs/WINDOWS-INSTALLATION-GUIDE.md)
- **Docker problemleri**: [Docker Rehberi](docs/DOCKER-GUIDE.md)
- **API kullanımı**: http://localhost:5093/swagger (Tüm platformlar)
- **Tüm dokümantasyon**: [Tam Dokümantasyon](docs/COMPLETE-DOCUMENTATION.md)

Her türlü soru ve öneriniz için lütfen [issue açın](https://github.com/hizir-ceylan/InventoryManagementSystem/issues) veya iletişime geçin.