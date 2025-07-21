# InventoryManagementSystem

Kurumsal cihaz envanteri yönetimi, değişiklik takibi ve raporlaması için geliştirilen bir sistemdir.

## ✨ Yeni Özellikler - Docker Desteği

🐳 **Docker ile Kolay Test ve Deployment!**
Bu proje artık Docker desteği ile birlikte gelir ve çok kolay test edilebilir.

### Hızlı Docker Başlangıcı

```bash
# 1. Projeyi indirin
git clone https://github.com/hizir-ceylan/InventoryManagementSystem.git
cd InventoryManagementSystem

# 2. Hızlı başlangıç scripti ile başlatın
./quick-start.sh

# 3. Otomatik test çalıştırın
./test-docker.sh test
```

**Erişim:**
- API: http://localhost:5000
- Swagger UI: http://localhost:5000/swagger

## Özellikler

### 🔧 Temel Özellikler
- Cihaz ekleme, listeleme, güncelleme ve silme
- Donanım & yazılım bilgilerini toplama
- Kullanıcı ve lokasyon yönetimi
- Değişikliklerin otomatik takibi ve raporlanması
- API ile farklı uygulamalardan entegrasyon imkanı

### 🐳 Docker Özellikleri
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

## Başlangıç Seçenekleri

### 🚀 Docker ile Hızlı Başlangıç (Önerilen)

```bash
# Hızlı başlangıç scripti
./quick-start.sh

# Manuel Docker Compose
docker-compose -f docker-compose.simple.yml up --build -d

# Production setup
docker-compose up --build -d
```

### 💻 Manuel Kurulum

1. **Gereksinimler:**  
   - .NET 8 SDK
   - SQL Server (veya uygun connection string ile desteklenen diğer veritabanları)

2. **Projeyi Çalıştırma:**
   ```bash
   git clone https://github.com/hizir-ceylan/InventoryManagementSystem.git
   cd InventoryManagementSystem/Inventory.Api
   dotnet run
   ```

## 📖 Dokümantasyon

### Docker Kullanımı
- 🐳 **[Docker Rehberi](docs/DOCKER-GUIDE.md)** - Docker kurulum ve test rehberi
- 📋 **[Tüm Dokümantasyon](docs/COMPLETE-DOCUMENTATION.md)** - Kapsamlı teknik dokümantasyon

### Test ve Doğrulama
```bash
# Otomatik test suite
./test-docker.sh test

# Container durumu
./quick-start.sh status

# Logları görüntüleme
./quick-start.sh logs
```

## 🧪 API Test Örnekleri

### Cihaz Ekleme
```bash
curl -X POST "http://localhost:5000/api/device" \
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
curl http://localhost:5000/api/device
```

### Network Scan Başlatma
```bash
curl -X POST "http://localhost:5000/api/networkscan/start" \
  -H "Content-Type: application/json" \
  -d '{"networkRange": "192.168.1.0/24"}'
```

## 🎯 Hızlı Test Senaryoları

### Docker Environment Test
```bash
# 1. Sistem başlat
./quick-start.sh

# 2. API test et
curl http://localhost:5000/api/device

# 3. Tam test suite çalıştır
./test-docker.sh test

# 4. Performans testi
ab -n 100 -c 10 http://localhost:5000/api/device
```

### Manuel Test
1. **Swagger UI**: http://localhost:5000/swagger adresine gidin
2. **Device endpoints**'ini test edin
3. **Logging endpoints**'ini test edin
4. **Network scan**'i başlatın

## 🛠️ Geliştirme

### Development Environment
```bash
# Development mode
docker run -p 5000:5000 -e ASPNETCORE_ENVIRONMENT=Development inventory-api:latest

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
curl http://localhost:5000/api/device
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

### 🆘 Destek

- **Docker problemleri**: [Docker Rehberi](docs/DOCKER-GUIDE.md)
- **API kullanımı**: http://localhost:5000/swagger
- **Tüm dokümantasyon**: [Tam Dokümantasyon](docs/COMPLETE-DOCUMENTATION.md)

Her türlü soru ve öneriniz için lütfen [issue açın](https://github.com/hizir-ceylan/InventoryManagementSystem/issues) veya iletişime geçin.