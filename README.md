# Inventory Management System

**Versiyon**: 2.1 | **Durum**: Production Ready ✅

Kurumsal cihaz envanteri yönetimi, değişiklik takibi ve raporlaması için geliştirilen profesyonel bir sistem.

## 🚀 Sistem v2.1 Özellikleri

- ✅ **Modüler Web Mimarisi**: 6 ayrı sayfa, 8 JS modülü ile profesyonel web sitesi yapısı
- ✅ **VMware Entegrasyonu**: vSphere sunucuları ile sanal makine yönetimi
- ✅ **MSI Enterprise Deployment**: Group Policy ile kurumsal dağıtım
- ✅ **Gelişmiş Update Detection**: Windows Update Agent entegrasyonu
- ✅ **Dashboard Homepage**: Sistem özeti ve hızlı işlemler
- ✅ **Offline Çalışma**: API bağlantısı olmadığında yerel veri depolama

## 📋 Ana Bileşenler

- **🖥️ Inventory.Api**: RESTful API Sunucusu (Port: 5093)
- **🔍 Inventory.Agent.Windows**: Windows Agent Servisi (30 dakikalık envanter toplama)
- **🌐 Inventory.WebApp**: Modern Web Yönetim Arayüzü
- **📊 Inventory.Data**: Entity Framework Core veri katmanı
- **🏗️ Inventory.Domain**: Domain modelleri ve business logic

## ⚡ Hızlı Kurulum

### 🐳 Docker Kurulumu (Önerilen)
```bash
git clone https://github.com/hizir-ceylan/InventoryManagementSystem.git
cd InventoryManagementSystem
docker-compose up --build -d
```

### 🏢 MSI Enterprise Kurulumu
```powershell
# WiX MSI paketleme
cd build-tools
.\Build-MSI.ps1
# Output: InventoryManagementSystem.msi
```

### 🖥️ Windows Service Kurulumu
```powershell
# Yönetici PowerShell'de
cd build-tools
.\Build-Setup.ps1
```

## 🌐 Erişim Adresleri

- **API ve Swagger**: http://localhost:5093/swagger
- **Web Arayüzü**: http://localhost:5094

## 📚 Dokümantasyon

**📖 Kapsamlı Teknik Dokümantasyon**: [`docs/TEKNIK-DOKUMANTASYON.md`](docs/TEKNIK-DOKUMANTASYON.md)

Tüm teknik detaylar, konfigürasyon seçenekleri, kurulum rehberleri, API dokümantasyonu, VMware entegrasyonu, MSI paketleme, sorun giderme ve best practices için yukarıdaki bağlantıyı takip edin.

## ⚙️ Sistem Gereksinimleri

- .NET 8.0 Runtime
- Windows 10/11 veya Linux (Ubuntu 20.04+)
- RAM: 2GB (minimum), 4GB (önerilen)
- Disk: 500MB + veri için ek alan

## 🤝 Katkıda Bulunma

1. Repository'yi fork edin
2. Feature branch oluşturun (`git checkout -b feature/amazing-feature`)
3. Değişikliklerinizi commit edin (`git commit -m 'Add amazing feature'`)
4. Branch'inizi push edin (`git push origin feature/amazing-feature`)
5. Pull Request açın

## 📄 Lisans

Bu proje MIT lisansı altında lisanslanmıştır. Detaylar için [LICENSE](LICENSE) dosyasına bakın.
