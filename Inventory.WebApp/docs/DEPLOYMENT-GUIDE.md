# Envanter Yönetim Sistemi - Web Arayüzü Kurulum Rehberi

## Genel Bakış

Bu dokümantasyon, Çaykur Envanter Yönetim Sistemi'nin ayrı web arayüzü uygulamasının kurulumu ve çalıştırılması için hazırlanmıştır.

## Proje Yapısı

```
InventoryManagementSystem/
├── Inventory.Api/          # API servisi (backend)
├── Inventory.WebApp/       # Web arayüzü (frontend) - YENİ!
├── Inventory.Agent.Windows/# Windows agent
├── Inventory.Data/         # Veritabanı katmanı
├── Inventory.Domain/       # Domain modelleri
└── Inventory.Shared/       # Paylaşılan sınıflar
```

## Gereksinimler

### Sistem Gereksinimleri
- .NET 8.0 SDK (Development için)
- .NET 8.0 Runtime (Production için)
- Windows 10/11, Windows Server 2019/2022 veya Linux
- 2GB RAM (minimum)
- 1GB disk alanı

### Ağ Gereksinimleri
- Web arayüzü: Port 5094 (varsayılan, değiştirilebilir)
- API servisi: Port 5093 (değiştirilebilir)
- HTTPS desteği (production ortamı için önerilir)

## Yerel Test Ortamı Kurulumu

### 1. Projeyi İndirin
```bash
git clone https://github.com/hizir-ceylan/InventoryManagementSystem.git
cd InventoryManagementSystem
```

### 2. Projeyi Derleyin
```bash
dotnet build
```

### 3. API Servisini Başlatın
```bash
# Terminal 1'de
cd Inventory.Api
dotnet run
# API şu adreste çalışır: http://localhost:5093
```

### 4. Web Arayüzünü Başlatın
```bash
# Terminal 2'de
cd Inventory.WebApp
dotnet run
# Web arayüzü şu adreste çalışır: http://localhost:5094
```

### 5. Erişim
- **Web Arayüzü**: http://localhost:5094
- **API Dokümantasyonu**: http://localhost:5093/swagger

## Production Server Kurulumu

### 1. Sunucu Hazırlığı

#### Windows Server
```powershell
# .NET 8.0 Runtime indirin ve kurun
# https://dotnet.microsoft.com/download/dotnet/8.0

# IIS Role'ü ekleyin (isteğe bağlı)
Enable-WindowsOptionalFeature -Online -FeatureName IIS-WebServerRole
Enable-WindowsOptionalFeature -Online -FeatureName IIS-WebServer
Enable-WindowsOptionalFeature -Online -FeatureName IIS-ASPNET48
```

#### Linux (Ubuntu/CentOS)
```bash
# .NET 8.0 Runtime kurulumu
wget https://packages.microsoft.com/config/ubuntu/20.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
sudo apt-get update
sudo apt-get install -y aspnetcore-runtime-8.0

# Nginx kurulumu (ters proxy için)
sudo apt-get install -y nginx
```

### 2. Uygulama Deployment

#### Uygulama Dosyalarını Hazırlayın
```bash
# Her iki projeyi de production için derleyin
dotnet publish Inventory.Api -c Release -o /var/www/inventory-api
dotnet publish Inventory.WebApp -c Release -o /var/www/inventory-webapp
```

#### Windows Server'da IIS ile
1. IIS Manager'ı açın
2. İki adet site oluşturun:
   - **inventory-api** (Port: 80/443)
   - **inventory-webapp** (Port: 8080/8443)
3. Uygulama dosyalarını ilgili dizinlere kopyalayın

#### Linux'ta Systemd ile
1. API için servis dosyası: `/etc/systemd/system/inventory-api.service`
```ini
[Unit]
Description=Inventory Management API
After=network.target

[Service]
Type=notify
ExecStart=/usr/bin/dotnet /var/www/inventory-api/Inventory.Api.dll
Restart=always
RestartSec=10
SyslogIdentifier=inventory-api
User=www-data
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=ASPNETCORE_URLS=http://localhost:5000

[Install]
WantedBy=multi-user.target
```

2. Web App için servis dosyası: `/etc/systemd/system/inventory-webapp.service`
```ini
[Unit]
Description=Inventory Management Web App
After=network.target

[Service]
Type=notify
ExecStart=/usr/bin/dotnet /var/www/inventory-webapp/Inventory.WebApp.dll
Restart=always
RestartSec=10
SyslogIdentifier=inventory-webapp
User=www-data
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=ASPNETCORE_URLS=http://localhost:5001

[Install]
WantedBy=multi-user.target
```

3. Servisleri başlatın:
```bash
sudo systemctl enable inventory-api
sudo systemctl enable inventory-webapp
sudo systemctl start inventory-api
sudo systemctl start inventory-webapp
```

### 3. Nginx Ters Proxy Yapılandırması (Linux)

`/etc/nginx/sites-available/inventory-system` dosyası:
```nginx
# API Ters Proxy
server {
    listen 80;
    server_name api.company.gov.tr;  # Gerçek domain adınızı kullanın
    
    location / {
        proxy_pass http://localhost:5000;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection keep-alive;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_cache_bypass $http_upgrade;
    }
}

# Web App Ters Proxy
server {
    listen 80;
    server_name inventory.company.gov.tr;  # Gerçek domain adınızı kullanın
    
    location / {
        proxy_pass http://localhost:5001;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection keep-alive;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_cache_bypass $http_upgrade;
    }
}
```

Nginx'i etkinleştirin:
```bash
sudo ln -s /etc/nginx/sites-available/inventory-system /etc/nginx/sites-enabled/
sudo nginx -t
sudo systemctl reload nginx
```

### 4. SSL/HTTPS Yapılandırması (Önerilir)

Let's Encrypt ile:
```bash
sudo apt install certbot python3-certbot-nginx
sudo certbot --nginx -d inventory.company.gov.tr -d api.company.gov.tr
```

### 5. Yapılandırma Dosyaları

#### API Yapılandırması (`appsettings.Production.json`)
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=InventoryManagement;Trusted_Connection=true;"
  },
  "ServerSettings": {
    "Mode": "Remote",
    "RemoteDatabaseConnectionString": "Server=db.company.gov.tr;Database=InventoryManagement;User Id=inventory_user;Password=your_secure_password;"
  }
}
```

#### Web App Yapılandırması (`appsettings.Production.json`)
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "inventory.company.gov.tr",
  "ApiSettings": {
    "BaseUrl": "https://api.company.gov.tr"
  }
}
```

#### Web App JavaScript Yapılandırması
`wwwroot/js/config.js` dosyasını production için güncelleyin:
```javascript
window.INVENTORY_CONFIG = {
    API_BASE_URL: 'http://localhost:5093',
    PRODUCTION_API_URL: 'https://api.company.gov.tr', // Production API URL'sini buraya yazın
    // ... diğer ayarlar
};
```

### 6. Güvenlik Ayarları

#### Firewall Kuralları
```bash
# Linux (UFW)
sudo ufw allow 80/tcp
sudo ufw allow 443/tcp
sudo ufw deny 5000/tcp  # API'ye doğrudan erişimi engelle
sudo ufw deny 5001/tcp  # Web App'e doğrudan erişimi engelle

# Windows Firewall
New-NetFirewallRule -DisplayName "HTTP" -Direction Inbound -Protocol TCP -LocalPort 80 -Action Allow
New-NetFirewallRule -DisplayName "HTTPS" -Direction Inbound -Protocol TCP -LocalPort 443 -Action Allow
```

#### Erişim Kontrolü
Web arayüzüne sadece yöneticilerin erişebilmesi için:
1. VPN/IP beyazlama
2. Active Directory entegrasyonu
3. Nginx'te IP kısıtlaması

## Sorun Giderme

### Yaygın Sorunlar

#### 1. API Bağlantı Sorunu
```bash
# API'nin çalışıp çalışmadığını kontrol edin
curl -I http://localhost:5093/api/device

# Logları kontrol edin
sudo journalctl -u inventory-api -f
```

#### 2. Veritabanı Bağlantı Sorunu
- Connection string'leri kontrol edin
- Veritabanı servisinin çalışıp çalışmadığını kontrol edin
- Firewall kurallarını kontrol edin

#### 3. CORS Sorunları
API'de CORS ayarlarını kontrol edin ve gerekirse domain'leri güncelleyin.

### Log Dosyaları
- **Linux**: `/var/log/inventory-*`
- **Windows**: Event Viewer > Application Logs
- **IIS**: `C:\inetpub\logs\LogFiles`

## Performans Optimizasyonu

### 1. Uygulama Ayarları
```json
{
  "Kestrel": {
    "Limits": {
      "MaxConcurrentConnections": 100,
      "MaxConcurrentUpgradedConnections": 100
    }
  }
}
```

### 2. Nginx Ayarları
```nginx
# nginx.conf'a ekleyin
worker_processes auto;
worker_connections 1024;

# Gzip sıkıştırma
gzip on;
gzip_vary on;
gzip_min_length 1024;
gzip_types text/plain text/css application/json application/javascript text/xml application/xml application/xml+rss text/javascript;
```

## Güncelleme ve Bakım

### Uygulama Güncellemesi
```bash
# Servisleri durdurun
sudo systemctl stop inventory-api inventory-webapp

# Yeni sürümü deploy edin
dotnet publish --configuration Release --output /var/www/inventory-api
dotnet publish --configuration Release --output /var/www/inventory-webapp

# Servisleri başlatın
sudo systemctl start inventory-api inventory-webapp
```

### Veritabanı Backup
```bash
# Otomatik backup script'i oluşturun
#!/bin/bash
BACKUP_DIR="/backup/inventory"
DATE=$(date +%Y%m%d_%H%M%S)
mkdir -p $BACKUP_DIR
pg_dump inventory_db > $BACKUP_DIR/inventory_backup_$DATE.sql
```

## Destek ve İletişim

Herhangi bir sorun yaşadığınızda:
1. Log dosyalarını kontrol edin
2. GitHub Issues'da sorun bildirin
3. Dokümantasyonu gözden geçirin

---

**Not**: Bu kurulum production ortamı için hazırlanmıştır. Güvenlik ayarlarını kuruluşunuzun politikalarına göre düzenleyin.