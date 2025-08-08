# Inventory Management System

Kurumsal cihaz envanteri yönetimi, değişiklik takibi ve raporlaması için geliştirilen profesyonel bir sistem. Bu sistem kurumsal ortamlarda bilgisayar, donanım ve yazılım envanterini otomatik olarak toplar, izler ve yönetir.

## Sistem Nedir?

Bu envanter yönetim sistemi, kurumsal ağlardaki tüm cihazları otomatik olarak keşfeder, bilgilerini toplar ve merkezi bir veritabanında saklar. Sistem üç ana bileşenden oluşur:

- **API Sunucusu**: Tüm verilerin toplandığı ve yönetildiği merkezi sunucu
- **Windows Agent**: Bilgisayarlara kurularak donanım/yazılım bilgilerini toplayan servis
- **Web Uygulaması**: Envanter verilerini görüntülemek ve yönetmek için web arayüzü

## Ana Bileşenler ve Dosya Yapısı

### 🖥️ **Inventory.Api** - RESTful API Sunucusu
Sistemin kalbi olan merkezi API sunucusu. Tüm verilerin toplandığı, işlendiği ve dağıtıldığı yer.
- **Kullanım**: Cihaz bilgilerini saklama, ağ tarama, raporlama
- **Port**: Varsayılan olarak 5093 portunda çalışır
- **Swagger UI**: http://localhost:5093/swagger

### 🔍 **Inventory.Agent.Windows** - Windows Agent Servisi  
Her bilgisayara kurularak sistem bilgilerini otomatik toplayan Windows servisi.
- **Kullanım**: 30 dakikada bir donanım/yazılım envanteri toplar
- **Service Mode**: Windows servisi olarak arka planda çalışır
- **Offline Çalışma**: API'ye ulaşamadığında yerel olarak veri saklar

### 🌐 **Inventory.WebApp** - Web Yönetim Arayüzü
Envanter verilerini görüntülemek ve yönetmek için modern web uygulaması.
- **Kullanım**: Cihaz listesi, detaylı raporlar, istatistikler
- **Teknoloji**: ASP.NET Core MVC
- **Responsive**: Mobil ve masaüstü uyumlu

### 📊 **Inventory.Data** - Veri Katmanı
Entity Framework Core tabanlı veri erişim katmanı.
- **Kullanım**: Veritabanı işlemleri, migrations
- **Desteklenen**: SQLite, SQL Server, PostgreSQL

### 🏗️ **Inventory.Domain** - Domain Modelleri
İş mantığı ve veri modellerinin tanımlandığı katman.
- **Kullanım**: Entity'ler, enum'lar, business logic

### 🔧 **Inventory.Shared** - Paylaşılan Sınıflar
Projeler arası paylaşılan yardımcı sınıflar ve DTOs.
- **Kullanım**: Ortak modeller, utilities, extension'lar

## Sistem Özellikleri

- **🔄 Otomatik Envanter Toplama**: Donanım ve yazılım bilgilerinin düzenli toplanması
- **🌐 Ağ Keşfi**: IP aralığından cihazları otomatik bulma ve kaydetme  
- **📝 Değişiklik Takibi**: Hardware değişikliklerinin otomatik tespit edilmesi
- **⚙️ Windows Service**: Sistem başlangıcında otomatik başlama
- **🐳 Docker Desteği**: Konteyner ortamında kolay kurulum
- **🔒 Offline Çalışma**: Ağ bağlantısı kesildiğinde yerel veri saklama
- **📊 Çoklu Veritabanı**: SQLite, SQL Server, PostgreSQL desteği
- **🔍 RESTful API**: Swagger dokümantasyonu ile zengin API
- **📱 Web Arayüzü**: Modern ve kullanıcı dostu web interface

## Hızlı Kurulum

### 🐳 Docker ile Kurulum (Önerilen)
```bash
git clone https://github.com/hizir-ceylan/InventoryManagementSystem.git
cd InventoryManagementSystem
docker-compose up --build -d
```

### 🖥️ Windows Service Kurulumu
```powershell
# Yönetici PowerShell'de çalıştırın
cd build-tools
.\Build-Setup.ps1
```

### 🌐 Erişim Adresleri
- **API ve Swagger**: http://localhost:5093/swagger
- **Web Arayüzü**: http://localhost:5094 (WebApp çalışıyorsa)

### ⚙️ Sistem Gereksinimleri
- .NET 8.0 Runtime
- Windows 10/11 veya Linux (Ubuntu 20.04+)
- Docker (isteğe bağlı)
- RAM: 2GB (minimum), 4GB (önerilen)
- Disk: 500MB + veri için ek alan

## Kullanım Senaryoları

### 🏢 Kurumsal Envanter Yönetimi
- Tüm şirket bilgisayarlarının otomatik takibi
- Donanım değişikliklerinin anlık tespit edilmesi
- Yazılım envanterinin merkezi yönetimi
- Lisans yönetimi için veri sağlama

### 🔍 IT Yönetimi ve İzleme
- Cihazların anlık durumu ve lokasyon takibi
- Periyodik sistem sağlığı kontrolü
- Hardware upgrade planlama verisi
- Kullanıcı bazlı cihaz atama takibi

### 📊 Raporlama ve Analiz
- Detaylı donanım envanter raporları
- Cihaz kullanım istatistikleri
- Değişiklik geçmişi ve audit logları
- Maliyet optimizasyonu için veri analizi

## Konfigürasyon Seçenekleri

### 🔧 Agent Ayarları
- **Tarama Sıklığı**: Varsayılan 30 dakika (değiştirilebilir)
- **API Sunucu Adresi**: Network'teki farklı sunuculara yönlendirilebilir
- **Offline Depolama**: Bağlantı kesildiğinde yerel veri saklama
- **Log Seviyeleri**: Debug, Info, Warning, Error

### 🌐 API Konfigürasyonu  
- **Veritabanı Türü**: SQLite (development) / SQL Server (production)
- **Port Ayarları**: Farklı portlarda çalıştırma
- **CORS Ayarları**: Web uygulaması entegrasyonu
- **Swagger UI**: Geliştirme/production ortamları için aktif/pasif

### 🐳 Docker Deployment
- **Environment Variables**: Tüm ayarlar çevre değişkenleri ile
- **Volume Mounting**: Veri kalıcılığı için docker volume'lar
- **Multi-container**: API, Database, Web uygulaması ayrı konteynerler
- **Production Ready**: Nginx reverse proxy ile

## Dosya ve Klasör Yapısı

```
InventoryManagementSystem/
│
├── 📁 Inventory.Api/                 # 🖥️ RESTful Web API Sunucusu
│   ├── Controllers/                  # API endpoint controllers
│   ├── Services/                     # Business logic services
│   ├── DTOs/                        # Data transfer objects
│   └── appsettings.json             # API konfigürasyon dosyası
│
├── 📁 Inventory.Agent.Windows/       # 🔍 Windows Agent Servisi
│   ├── Services/                     # Agent background services
│   ├── Models/                      # Agent data models
│   ├── Configuration/               # Agent settings
│   └── appsettings.json             # Agent konfigürasyon dosyası
│
├── 📁 Inventory.WebApp/              # 🌐 Web Yönetim Arayüzü
│   ├── Pages/                       # Razor pages
│   ├── wwwroot/                     # Static files (CSS, JS)
│   └── appsettings.json             # Web app konfigürasyonu
│
├── 📁 Inventory.Data/                # 📊 Entity Framework Veri Katmanı
│   ├── Contexts/                    # Database contexts
│   ├── Migrations/                  # Database migrations
│   └── Repositories/                # Data access repositories
│
├── 📁 Inventory.Domain/              # 🏗️ Domain Models ve Business Logic
│   ├── Entities/                    # Database entities
│   ├── Enums/                       # System enumerations
│   └── ValueObjects/                # Domain value objects
│
├── 📁 Inventory.Shared/              # 🔧 Paylaşılan Sınıflar
│   ├── DTOs/                        # Shared data transfer objects
│   ├── Extensions/                  # Extension methods
│   └── Helpers/                     # Utility classes
│
├── 📁 build-tools/                   # 🛠️ Build ve Deployment Scripts
│   ├── Build-Setup.ps1             # Windows otomatik kurulum
│   ├── quick-start.sh               # Linux hızlı başlatma
│   └── README.md                    # Build araçları dokümantasyonu
│
├── 📁 docs/                          # 📖 Teknik Dokümantasyon
│   └── TEKNIK-DOKUMANTASYON.md      # Kapsamlı teknik rehber
│
├── 📁 database/                      # 🗄️ Database Scripts ve Toollar
├── 📁 nginx/                         # 🌐 NGINX Konfigürasyonu
├── 📁 query/                         # 🔍 Örnek SQL Sorguları
│
├── 🐳 docker-compose.yml            # Docker orchestration
├── 🐳 Dockerfile                    # API Docker image
├── 🐳 Dockerfile.agent              # Agent Docker image  
├── 🐳 Dockerfile.webapp             # WebApp Docker image
│
├── 📄 README.md                     # Bu dosya - Genel sistem özeti
└── 📋 InventoryManagementSystem.sln # Visual Studio solution dosyası
```

## Geliştirme ve Destek

### 🔧 Geliştirici Komutları
```bash
# Projeyi build etme
dotnet build

# API'yi development modunda çalıştırma  
dotnet run --project Inventory.Api

# Agent'ı test modunda çalıştırma
dotnet run --project Inventory.Agent.Windows

# Database migration oluşturma
dotnet ef migrations add MigrationName --project Inventory.Data
```

### 📚 Daha Fazla Bilgi
- **[Kapsamlı Teknik Dokümantasyon](docs/TEKNIK-DOKUMANTASYON.md)** - Detaylı kurulum, konfigürasyon ve kullanım rehberi
- **API Dokümantasyonu**: http://localhost:5093/swagger (API çalıştıktan sonra)
- **GitHub Issues**: Sorun bildirimi ve özellik istekleri için

### 📞 Destek
Herhangi bir sorun için [GitHub Issues](https://github.com/hizir-ceylan/InventoryManagementSystem/issues) sayfasından issue açabilirsiniz.

## Lisans
MIT lisansı ile açık kaynak olarak sunulmaktadır.
