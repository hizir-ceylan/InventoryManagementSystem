# Inventory Management System - Documentation Index

Bu dizinde Inventory Management System'e ait tüm dokümantasyon bulunmaktadır.

## Birleştirilmiş Dokümantasyon

**Ana dokümantasyon artık tek dosyada birleştirilmiştir:**
- 📖 **[COMPLETE-DOCUMENTATION.md](COMPLETE-DOCUMENTATION.md)** - Tüm teknik dokümantasyon tek dosyada

## Docker Kullanımı

**Docker ile test etmek için:**
- 🐳 **[DOCKER-GUIDE.md](DOCKER-GUIDE.md)** - Docker kurulum ve test rehberi

## Eski Dokümantasyon Dosyaları

Aşağıdaki dosyalar artık birleştirilmiştir ve referans amaçlı tutulmaktadır:

### Teknik Dokümantasyon
- [technical-documentation.md](technical-documentation.md) - Eski teknik dokümantasyon
- [installation-guide.md](installation-guide.md) - Eski kurulum rehberi
- [server-deployment-testing.md](server-deployment-testing.md) - Eski sunucu kurulum rehberi
- [platform-support.md](platform-support.md) - Platform desteği bilgisi

### Yeni Docker Özellikleri

Bu projede yeni eklenen Docker özellikleri:

#### 🎯 Docker Desteği
- **Multi-stage Docker build** ile optimize edilmiş containerlar
- **Docker Compose** ile kolay setup
- **SQLite ve SQL Server** desteği
- **Network isolation** ile güvenli çalışma

#### 🧪 Test Araçları
- **Otomatik test scripti** (`test-docker.sh`)
- **API endpoint testleri**
- **Performans testleri**
- **Veri kalıcılığı testleri**

#### 📊 Monitoring
- **Container health checks**
- **Log aggregation**
- **Resource monitoring**
- **Error tracking**

## Kullanım Önerileri

### Hızlı Başlangıç
1. [DOCKER-GUIDE.md](DOCKER-GUIDE.md) dosyasını okuyun
2. `docker-compose.simple.yml` ile test edin
3. `./test-docker.sh test` ile otomatik test çalıştırın

### Detaylı Bilgi
1. [COMPLETE-DOCUMENTATION.md](COMPLETE-DOCUMENTATION.md) dosyasını inceleyin
2. API dokümantasyonu için Swagger UI kullanın
3. Troubleshooting bölümünden yardım alın

### Production Kurulum
1. Docker Compose production setup kullanın
2. SQL Server ile database setup yapın
3. Nginx reverse proxy konfigüre edin
4. SSL sertifikalarını ayarlayın

## Katkıda Bulunma

Dokümantasyonu geliştirmek için:
1. COMPLETE-DOCUMENTATION.md dosyasını güncelleyin
2. Yeni özellikler için Docker rehberini güncelleyin
3. Test scriptlerini iyileştirin

---

**Not**: Eski dokümantasyon dosyaları referans amaçlı tutulmuş olup, güncel bilgiler için birleştirilmiş dokümantasyonu kullanın.