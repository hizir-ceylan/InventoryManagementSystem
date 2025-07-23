# Inventory Management System - Documentation Index

Bu dizinde Inventory Management System'e ait tüm dokümantasyon bulunmaktadır.

## Dokümantasyon Dizini

Bu dizinde Inventory Management System'e ait tüm dokümantasyon profesyonel bir şekilde organize edilmiştir.

## Ana Dokümantasyon

### 📖 Temel Kaynaklar
- **[COMPLETE-DOCUMENTATION.md](COMPLETE-DOCUMENTATION.md)** - Kapsamlı teknik dokümantasyon (Ana kaynak)
- **[DOCKER-GUIDE.md](DOCKER-GUIDE.md)** - Docker kurulum ve kullanım rehberi
- **[CHANGELOG.md](CHANGELOG.md)** - Güncellemeler ve değişiklik geçmişi

### 🧪 Test ve Kurulum
- **[TESTING_GUIDE.md](TESTING_GUIDE.md)** - Hızlı test rehberi
- **[server-deployment-testing.md](server-deployment-testing.md)** - Detaylı sunucu kurulumu ve test
- **[SOLUTION_SUMMARY.md](SOLUTION_SUMMARY.md)** - Çözüm özeti ve problem çözümleri

### ⚙️ Özel Konfigürasyonlar
- **[remote-server-configuration.md](remote-server-configuration.md)** - Uzak sunucu yapılandırması
- **[platform-support.md](platform-support.md)** - Platform desteği ve değişiklik takibi

### 📂 Diğer Kaynaklar
- **[legacy/](legacy/)** - Eski dokümantasyon dosyaları (referans amaçlı)
- **[img/](img/)** - Dokümantasyon görselleri

## Eski Dokümantasyon Dosyaları

Aşağıdaki dosyalar artık `legacy/` klasöründe arşivlenmiştir:

### Arşiv Dosyaları
- [legacy/technical-documentation.md](legacy/technical-documentation.md) - Eski teknik dokümantasyon
- [legacy/installation-guide.md](legacy/installation-guide.md) - Eski kurulum rehberi

*Not: Bu dosyalar COMPLETE-DOCUMENTATION.md dosyasında birleştirilmiştir.*

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
3. `./scripts/test-docker.sh test` ile otomatik test çalıştırın

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