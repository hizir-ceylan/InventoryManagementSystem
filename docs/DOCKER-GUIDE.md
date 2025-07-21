# Inventory Management System - Docker Implementation

## Quick Start Guide

Bu döküman Inventory Management System'in Docker kullanarak nasıl test edileceğini ve çalıştırılacağını açıklar.

### Ön Gereksinimler

- Docker ve Docker Compose kurulu olmalı
- Minimum 4GB RAM
- Minimum 10GB disk alanı

### Hızlı Başlangıç

#### 1. Projeyi İndirin
```bash
git clone https://github.com/hizir-ceylan/InventoryManagementSystem.git
cd InventoryManagementSystem
```

#### 2. Simple Test (SQLite ile)
```bash
# SQLite ile basit test
docker-compose -f docker-compose.simple.yml up --build -d

# Durum kontrolü
docker-compose -f docker-compose.simple.yml ps

# Logları görüntüleme
docker-compose -f docker-compose.simple.yml logs -f
```

#### 3. Production Test (SQL Server ile)
```bash
# SQL Server ile production test
docker-compose up --build -d

# Durum kontrolü
docker-compose ps

# Logları görüntüleme
docker-compose logs -f
```

#### 4. Uygulamaya Erişim
- **API**: http://localhost:5000
- **Swagger UI**: http://localhost:5000/swagger
- **Nginx ile**: http://localhost (port 80)

### Otomatik Test Scripti

```bash
# Tüm testleri çalıştır
./test-docker.sh test

# Container durumunu kontrol et
./test-docker.sh status

# Logları görüntüle
./test-docker.sh logs

# Temizlik
./test-docker.sh cleanup
```

### Manuel Test Adımları

#### API Testi
```bash
# API'nin çalışıp çalışmadığını test et
curl http://localhost:5000/api/device

# Yeni cihaz ekle
curl -X POST "http://localhost:5000/api/device" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "TEST-PC-001",
    "macAddress": "00:1B:44:11:3A:B7",
    "ipAddress": "192.168.1.100",
    "deviceType": "PC",
    "model": "Test PC",
    "location": "Test Lab",
    "status": 0
  }'

# Log gönder
curl -X POST "http://localhost:5000/api/logging" \
  -H "Content-Type: application/json" \
  -d '{
    "source": "TestAgent",
    "level": "Info",
    "message": "Test log mesajı"
  }'
```

#### Veri Kalıcılığı Testi
```bash
# SQLite veritabanını kontrol et
ls -la ./Data/SQLite/
sqlite3 ./Data/SQLite/inventory.db "SELECT * FROM Devices;"

# Log dosyalarını kontrol et
ls -la ./Data/ApiLogs/
```

### Sorun Giderme

#### Container Başlamıyor
```bash
# Container loglarını kontrol et
docker-compose logs inventory-api

# Sistem kaynaklarını kontrol et
docker system df
docker stats

# Temizle ve yeniden oluştur
docker-compose down -v
docker-compose up --build
```

#### API Yanıt Vermiyor
```bash
# API'nin dinleyip dinlemediğini kontrol et
netstat -tlnp | grep 5000
docker-compose ps

# API loglarını kontrol et
docker-compose logs -f inventory-api

# Direkt bağlantı testi
curl -v http://localhost:5000/api/device
```

### Performans Testi

```bash
# Apache Bench ile test (eğer varsa)
ab -n 100 -c 10 http://localhost:5000/api/device

# cURL ile basit test
for i in {1..10}; do
  curl -s http://localhost:5000/api/device &
done
wait
```

### Docker Compose Seçenekleri

#### Basit Setup (docker-compose.simple.yml)
- SQLite veritabanı
- Tek container
- Test için ideal

#### Production Setup (docker-compose.yml)
- SQL Server veritabanı
- Nginx reverse proxy
- Redis cache (opsiyonel)
- Production için ideal

### Konfigürasyon

Environment değişkenleri ile konfigürasyon:
```bash
# API Konfigürasyonu
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://+:5000
ConnectionStrings__DefaultConnection=...

# Agent Konfigürasyonu
ApiSettings__BaseUrl=http://localhost:5000
ApiSettings__Timeout=30
ApiSettings__RetryCount=3
```

### Geliştirme

```bash
# Development build
docker build -t inventory-api:dev .

# Development modunda çalıştır
docker run -p 5000:5000 -e ASPNETCORE_ENVIRONMENT=Development inventory-api:dev
```

Bu Docker implementasyonu sayesinde Inventory Management System'i hızlı bir şekilde test edebilir ve production ortamında çalıştırabilirsiniz.