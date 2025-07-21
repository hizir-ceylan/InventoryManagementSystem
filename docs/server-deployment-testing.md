# Sunucu Kurulumu ve API Test Rehberi

## İçindekiler
1. [Hızlı Başlangıç](#hızlı-başlangıç)
2. [Adım Adım Sunucu Kurulumu](#adım-adım-sunucu-kurulumu)
3. [Veritabanı Kurulumu](#veritabanı-kurulumu)
4. [API Test Senaryoları](#api-test-senaryoları)
5. [Gerçek Veri Testi](#gerçek-veri-testi)
6. [Monitoring ve Logging](#monitoring-ve-logging)

## Hızlı Başlangıç

### 1 Dakikada Test Ortamı (SQLite ile)

```bash
# 1. Projeyi klonla
git clone https://github.com/hizir-ceylan/InventoryManagementSystem.git
cd InventoryManagementSystem

# 2. SQLite için connection string güncelle
cd Inventory.Api
cat > appsettings.Development.json << 'EOF'
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=inventory.db"
  },
  "NetworkScan": {
    "Enabled": true,
    "Interval": "01:00:00",
    "NetworkRange": "192.168.1.0/24"
  }
}
EOF

# 3. API'yi çalıştır
dotnet run --urls="http://0.0.0.0:5000"
```

API şu adreste çalışacak: `http://localhost:5000/swagger`

## Adım Adım Sunucu Kurulumu

### Aşama 1: Sistem Hazırlığı

#### Ubuntu 22.04 Sunucu Kurulumu:
```bash
# Sistem güncellemesi
sudo apt update && sudo apt upgrade -y

# Gerekli paketleri kur
sudo apt install -y wget curl git unzip

# .NET 8.0 SDK kurulumu
wget https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
sudo apt update
sudo apt install -y dotnet-sdk-8.0 aspnetcore-runtime-8.0
```

#### Windows Server 2022 Kurulumu:
```powershell
# PowerShell'i yönetici olarak çalıştır

# Chocolatey kur (paket yöneticisi)
Set-ExecutionPolicy Bypass -Scope Process -Force
[System.Net.ServicePointManager]::SecurityProtocol = [System.Net.ServicePointManager]::SecurityProtocol -bor 3072
iex ((New-Object System.Net.WebClient).DownloadString('https://community.chocolatey.org/install.ps1'))

# .NET 8.0 SDK kur
choco install -y dotnet-8.0-sdk

# Git kur
choco install -y git

# IIS kur
Enable-WindowsOptionalFeature -Online -FeatureName IIS-WebServerRole,IIS-WebServer,IIS-CommonHttpFeatures,IIS-HttpErrors,IIS-HttpLogging,IIS-SecurityRole,IIS-RequestFiltering,IIS-StaticContent,IIS-NetFxExtensibility45,IIS-NetFx4ExtensibilityModule,IIS-ISAPIExtensions,IIS-ISAPIFilter,IIS-AspNetCoreModule,IIS-AspNetCoreModuleV2
```

### Aşama 2: Proje Kurulumu

```bash
# Projeyi indir
cd /opt
sudo git clone https://github.com/hizir-ceylan/InventoryManagementSystem.git
sudo chown -R $USER:$USER InventoryManagementSystem
cd InventoryManagementSystem

# Projeyi build et
dotnet restore
dotnet build --configuration Release

# API'yi publish et
dotnet publish Inventory.Api --configuration Release --output /opt/inventoryapi
```

### Aşama 3: Konfigürasyon

Production konfigürasyon dosyası oluştur:
```bash
sudo nano /opt/inventoryapi/appsettings.Production.json
```

İçerik:
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    },
    "File": {
      "Enabled": true,
      "RetentionHours": 48,
      "LogLevel": {
        "Default": "Information"
      }
    }
  },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=InventoryDB;User Id=inventoryuser;Password=StrongPassword123!;TrustServerCertificate=true;"
  },
  "NetworkScan": {
    "Enabled": true,
    "Interval": "01:00:00",
    "NetworkRange": "192.168.1.0/24"
  },
  "Agent": {
    "LoggingInterval": "01:00:00",
    "LogRetentionHours": 48,
    "EnableHourlyLogging": true
  },
  "Cors": {
    "AllowedOrigins": ["http://localhost:3000", "https://yourdomain.com"],
    "AllowedMethods": ["GET", "POST", "PUT", "DELETE"],
    "AllowedHeaders": ["*"]
  }
}
```

## Veritabanı Kurulumu

### Seçenek 1: SQL Server (Production)

```bash
# SQL Server 2022 kurulumu (Ubuntu)
curl -fsSL https://packages.microsoft.com/keys/microsoft.asc | sudo gpg --dearmor -o /usr/share/keyrings/microsoft-prod.gpg
echo "deb [arch=amd64,armhf,arm64 signed-by=/usr/share/keyrings/microsoft-prod.gpg] https://packages.microsoft.com/ubuntu/22.04/mssql-server-2022 jammy main" | sudo tee /etc/apt/sources.list.d/mssql-server-2022.list

sudo apt update
sudo apt install -y mssql-server

# SQL Server konfigürasyonu
sudo /opt/mssql/bin/mssql-conf setup
# Seçenekler: 2 (Developer Edition), Evet, SA password: StrongPassword123!

# SQL Server araçları kurulumu
curl https://packages.microsoft.com/keys/microsoft.asc | sudo apt-key add -
curl https://packages.microsoft.com/config/ubuntu/22.04/prod.list | sudo tee /etc/apt/sources.list.d/msprod.list
sudo apt update
sudo apt install -y mssql-tools unixodbc-dev

# PATH'e ekle
echo 'export PATH="$PATH:/opt/mssql-tools/bin"' >> ~/.bash_profile
source ~/.bash_profile
```

Veritabanı ve tablo oluşturma:
```bash
# SQL Server'a bağlan ve veritabanını oluştur
sqlcmd -S localhost -U SA -P 'StrongPassword123!'

# SQL komutları
CREATE DATABASE InventoryDB;
GO

USE InventoryDB;
GO

CREATE LOGIN inventoryuser WITH PASSWORD = 'StrongPassword123!';
CREATE USER inventoryuser FOR LOGIN inventoryuser;
ALTER ROLE db_owner ADD MEMBER inventoryuser;
GO
```

SQL script dosyası oluştur:
```bash
cat > /tmp/create_tables.sql << 'EOF'
USE InventoryDB;
GO

-- Devices tablosu
CREATE TABLE Devices (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    Name NVARCHAR(200) NOT NULL,
    MacAddress NVARCHAR(17),
    IpAddress NVARCHAR(15),
    DeviceType NVARCHAR(50),
    Model NVARCHAR(200),
    Location NVARCHAR(200),
    Status INT DEFAULT 0,
    CreatedDate DATETIME2 DEFAULT GETUTCDATE(),
    UpdatedDate DATETIME2 DEFAULT GETUTCDATE()
);

-- DeviceHardwareInfo tablosu
CREATE TABLE DeviceHardwareInfo (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    DeviceId UNIQUEIDENTIFIER NOT NULL,
    Cpu NVARCHAR(200),
    CpuCores INT,
    CpuLogical INT,
    CpuClockMHz INT,
    Motherboard NVARCHAR(200),
    MotherboardSerial NVARCHAR(100),
    BiosManufacturer NVARCHAR(100),
    BiosVersion NVARCHAR(100),
    BiosSerial NVARCHAR(100),
    RamGB INT,
    DiskGB INT,
    CreatedDate DATETIME2 DEFAULT GETUTCDATE(),
    FOREIGN KEY (DeviceId) REFERENCES Devices(Id) ON DELETE CASCADE
);

-- DeviceSoftwareInfo tablosu
CREATE TABLE DeviceSoftwareInfo (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    DeviceId UNIQUEIDENTIFIER NOT NULL,
    OperatingSystem NVARCHAR(200),
    OsVersion NVARCHAR(100),
    OsArchitecture NVARCHAR(50),
    RegisteredUser NVARCHAR(200),
    SerialNumber NVARCHAR(100),
    ActiveUser NVARCHAR(200),
    CreatedDate DATETIME2 DEFAULT GETUTCDATE(),
    FOREIGN KEY (DeviceId) REFERENCES Devices(Id) ON DELETE CASCADE
);

-- ChangeLogs tablosu
CREATE TABLE ChangeLogs (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    DeviceId UNIQUEIDENTIFIER NOT NULL,
    ChangeDate DATETIME2 DEFAULT GETUTCDATE(),
    ChangeType NVARCHAR(100),
    OldValue NVARCHAR(MAX),
    NewValue NVARCHAR(MAX),
    ChangedBy NVARCHAR(200),
    FOREIGN KEY (DeviceId) REFERENCES Devices(Id) ON DELETE CASCADE
);

-- InstalledApps tablosu
CREATE TABLE InstalledApps (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    DeviceId UNIQUEIDENTIFIER NOT NULL,
    AppName NVARCHAR(200),
    Version NVARCHAR(100),
    Publisher NVARCHAR(200),
    InstallDate DATETIME2,
    FOREIGN KEY (DeviceId) REFERENCES Devices(Id) ON DELETE CASCADE
);

-- NetworkAdapters tablosu
CREATE TABLE NetworkAdapters (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    DeviceId UNIQUEIDENTIFIER NOT NULL,
    Description NVARCHAR(200),
    MacAddress NVARCHAR(17),
    IpAddress NVARCHAR(15),
    FOREIGN KEY (DeviceId) REFERENCES Devices(Id) ON DELETE CASCADE
);

-- Indexes oluştur
CREATE INDEX IX_Devices_MacAddress ON Devices(MacAddress);
CREATE INDEX IX_Devices_IpAddress ON Devices(IpAddress);
CREATE INDEX IX_Devices_DeviceType ON Devices(DeviceType);
CREATE INDEX IX_ChangeLogs_DeviceId ON ChangeLogs(DeviceId);
CREATE INDEX IX_ChangeLogs_ChangeDate ON ChangeLogs(ChangeDate);
CREATE INDEX IX_InstalledApps_DeviceId ON InstalledApps(DeviceId);
CREATE INDEX IX_NetworkAdapters_DeviceId ON NetworkAdapters(DeviceId);

-- Test verisi ekle
INSERT INTO Devices (Name, MacAddress, IpAddress, DeviceType, Model, Location, Status)
VALUES 
    ('SERVER-001', '00:1B:44:11:3A:B7', '192.168.1.10', 'Server', 'Dell PowerEdge R720', 'Server Room', 0),
    ('PC-001', '00:1B:44:11:3A:B8', '192.168.1.100', 'PC', 'Dell OptiPlex 7090', 'Office-101', 0),
    ('LAPTOP-001', '00:1B:44:11:3A:B9', '192.168.1.150', 'Laptop', 'Lenovo ThinkPad X1', 'Mobile', 0);

PRINT 'Database setup completed successfully!';
GO
EOF

# SQL script'i çalıştır
sqlcmd -S localhost -U SA -P 'StrongPassword123!' -i /tmp/create_tables.sql
```

### Seçenek 2: PostgreSQL

```bash
# PostgreSQL kurulumu
sudo apt install -y postgresql postgresql-contrib

# PostgreSQL yapılandırması
sudo -u postgres psql << 'EOF'
CREATE DATABASE inventorydb;
CREATE USER inventoryuser WITH ENCRYPTED PASSWORD 'StrongPassword123!';
GRANT ALL PRIVILEGES ON DATABASE inventorydb TO inventoryuser;
\q
EOF

# Connection string'i güncelle (PostgreSQL için)
# "Server=localhost;Database=inventorydb;User Id=inventoryuser;Password=StrongPassword123!;"
```

### Seçenek 3: SQLite (Test/Geliştirme)

```bash
# SQLite kullanımı için connection string
# "Data Source=/opt/inventoryapi/inventory.db"

# Test verisi oluştur
sqlite3 /opt/inventoryapi/inventory.db << 'EOF'
CREATE TABLE Devices (
    Id TEXT PRIMARY KEY,
    Name TEXT NOT NULL,
    MacAddress TEXT,
    IpAddress TEXT,
    DeviceType TEXT,
    Model TEXT,
    Location TEXT,
    Status INTEGER DEFAULT 0,
    CreatedDate TEXT DEFAULT CURRENT_TIMESTAMP,
    UpdatedDate TEXT DEFAULT CURRENT_TIMESTAMP
);

INSERT INTO Devices (Id, Name, MacAddress, IpAddress, DeviceType, Model, Location)
VALUES 
    ('12345678-1234-1234-1234-123456789012', 'TEST-PC-001', '00:1B:44:11:3A:B7', '192.168.1.100', 'PC', 'Test Model', 'Test Location');
EOF
```

## API Servis Kurulumu

### Linux Systemd Service

```bash
# Service dosyası oluştur
sudo tee /etc/systemd/system/inventoryapi.service > /dev/null << 'EOF'
[Unit]
Description=Inventory Management System API
After=network.target

[Service]
Type=notify
User=www-data
Group=www-data
WorkingDirectory=/opt/inventoryapi
ExecStart=/usr/bin/dotnet /opt/inventoryapi/Inventory.Api.dll
Restart=on-failure
RestartSec=5
SyslogIdentifier=inventoryapi
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=ASPNETCORE_URLS=http://0.0.0.0:5000
Environment=ASPNETCORE_HTTPS_PORT=5001

[Install]
WantedBy=multi-user.target
EOF

# Service'i etkinleştir ve başlat
sudo systemctl daemon-reload
sudo systemctl enable inventoryapi
sudo systemctl start inventoryapi

# Service durumunu kontrol et
sudo systemctl status inventoryapi
```

### Nginx Reverse Proxy

```bash
# Nginx kurulumu
sudo apt install -y nginx

# Site konfigürasyonu
sudo tee /etc/nginx/sites-available/inventoryapi > /dev/null << 'EOF'
server {
    listen 80;
    server_name inventory.yourdomain.com;
    
    # API endpoints
    location /api/ {
        proxy_pass http://localhost:5000/api/;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection keep-alive;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_cache_bypass $http_upgrade;
        proxy_read_timeout 300s;
        proxy_connect_timeout 75s;
    }
    
    # Swagger UI
    location /swagger {
        proxy_pass http://localhost:5000/swagger;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection keep-alive;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_cache_bypass $http_upgrade;
    }
    
    # Health check
    location /health {
        proxy_pass http://localhost:5000/health;
        access_log off;
    }
    
    # Static files ve root
    location / {
        proxy_pass http://localhost:5000/;
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
EOF

# Site'ı etkinleştir
sudo ln -s /etc/nginx/sites-available/inventoryapi /etc/nginx/sites-enabled/
sudo nginx -t
sudo systemctl restart nginx
```

## API Test Senaryoları

### 1. Temel API Testleri

```bash
# API sunucu IP'sini ayarla
API_SERVER="http://your-server-ip"
# veya domain kullan
API_SERVER="http://inventory.yourdomain.com"

# 1. API durumunu kontrol et
echo "=== API Health Check ==="
curl -X GET "$API_SERVER/api/device" \
  -H "accept: application/json" \
  -w "\nStatus Code: %{http_code}\nResponse Time: %{time_total}s\n"

# 2. Swagger dokümantasyonuna erişim
echo -e "\n=== Swagger UI Access ==="
curl -I "$API_SERVER/swagger" -w "Status Code: %{http_code}\n"
```

### 2. CRUD Operasyonları Testi

```bash
#!/bin/bash
API_SERVER="http://your-server-ip"

echo "=== Device CRUD Operations Test ==="

# 1. Cihaz Listesi (GET)
echo -e "\n1. Getting all devices..."
curl -X GET "$API_SERVER/api/device" \
  -H "accept: application/json" | jq .

# 2. Yeni Cihaz Ekleme (POST)
echo -e "\n2. Creating new device..."
DEVICE_RESPONSE=$(curl -X POST "$API_SERVER/api/device" \
  -H "accept: application/json" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "TEST-API-PC-001",
    "macAddress": "00:1B:44:11:3A:C1",
    "ipAddress": "192.168.1.201",
    "deviceType": "PC",
    "model": "Test PC Model",
    "location": "API Test Lab",
    "status": 0,
    "hardwareInfo": {
      "cpu": "Intel Core i7-12700K",
      "cpuCores": 12,
      "cpuLogical": 20,
      "cpuClockMHz": 3600,
      "ramGB": 32,
      "diskGB": 1000
    },
    "softwareInfo": {
      "operatingSystem": "Windows 11 Pro",
      "osVersion": "10.0.22631",
      "osArchitecture": "64-bit"
    }
  }' -s)

echo "$DEVICE_RESPONSE" | jq .

# Device ID'yi al
DEVICE_ID=$(echo "$DEVICE_RESPONSE" | jq -r '.id')
echo "Created Device ID: $DEVICE_ID"

# 3. Tek Cihaz Getirme (GET by ID)
echo -e "\n3. Getting device by ID..."
curl -X GET "$API_SERVER/api/device/$DEVICE_ID" \
  -H "accept: application/json" | jq .

# 4. Cihaz Güncelleme (PUT)
echo -e "\n4. Updating device..."
curl -X PUT "$API_SERVER/api/device/$DEVICE_ID" \
  -H "accept: application/json" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "TEST-API-PC-001-UPDATED",
    "macAddress": "00:1B:44:11:3A:C1",
    "ipAddress": "192.168.1.202",
    "deviceType": "PC",
    "model": "Updated Test PC Model",
    "location": "API Test Lab - Updated",
    "status": 0
  }' -w "Status Code: %{http_code}\n"

# 5. Güncellenmiş cihazı kontrol et
echo -e "\n5. Verifying update..."
curl -X GET "$API_SERVER/api/device/$DEVICE_ID" \
  -H "accept: application/json" | jq '.name, .location'

# 6. Cihaz Silme (DELETE)
echo -e "\n6. Deleting device..."
curl -X DELETE "$API_SERVER/api/device/$DEVICE_ID" \
  -H "accept: application/json" \
  -w "Status Code: %{http_code}\n"

# 7. Silme işlemini doğrula
echo -e "\n7. Verifying deletion..."
curl -X GET "$API_SERVER/api/device/$DEVICE_ID" \
  -H "accept: application/json" \
  -w "Status Code: %{http_code}\n"
```

### 3. Logging API Testleri

```bash
#!/bin/bash
API_SERVER="http://your-server-ip"

echo "=== Logging API Tests ==="

# 1. Log gönderme
echo -e "\n1. Submitting log entry..."
curl -X POST "$API_SERVER/api/logging" \
  -H "accept: application/json" \
  -H "Content-Type: application/json" \
  -d '{
    "source": "TestAgent",
    "level": "Info",
    "message": "Test log message from API test",
    "data": {
      "testProperty": "testValue",
      "timestamp": "'$(date -u +%Y-%m-%dT%H:%M:%SZ)'"
    }
  }' | jq .

# 2. Son logları getirme
echo -e "\n2. Getting recent logs..."
curl -X GET "$API_SERVER/api/logging/recent?count=10" \
  -H "accept: application/json" | jq .

# 3. Kaynak bazında log getirme
echo -e "\n3. Getting logs by source..."
curl -X GET "$API_SERVER/api/logging/recent/TestAgent?count=5" \
  -H "accept: application/json" | jq .

# 4. Log kaynaklarını listeleme
echo -e "\n4. Getting log sources..."
curl -X GET "$API_SERVER/api/logging/sources" \
  -H "accept: application/json" | jq .
```

### 4. Network Scan API Testleri

```bash
#!/bin/bash
API_SERVER="http://your-server-ip"

echo "=== Network Scan API Tests ==="

# 1. Network tarama başlatma
echo -e "\n1. Starting network scan..."
curl -X POST "$API_SERVER/api/networkscan/start" \
  -H "accept: application/json" \
  -H "Content-Type: application/json" \
  -d '{
    "networkRange": "192.168.1.0/24",
    "timeout": 30
  }' | jq .

# 2. Tarama durumunu kontrol etme
echo -e "\n2. Checking scan status..."
curl -X GET "$API_SERVER/api/networkscan/status" \
  -H "accept: application/json" | jq .

# 3. Bulunan cihazları listeleme
echo -e "\n3. Getting discovered devices..."
curl -X GET "$API_SERVER/api/networkscan/devices" \
  -H "accept: application/json" | jq .
```

## Gerçek Veri Testi

### 1. Agent'dan Gerçek Veri Gönderimi

```bash
# Agent'ı test etmek için
cd Inventory.Agent.Windows

# Geçici test konfigürasyonu oluştur
cat > appsettings.Test.json << 'EOF'
{
  "ApiSettings": {
    "BaseUrl": "http://your-server-ip",
    "Timeout": 30,
    "RetryCount": 3
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    }
  }
}
EOF

# Agent'ı test modunda çalıştır
dotnet run --environment Test
```

### 2. Manuel Test Verisi Gönderimi

```bash
#!/bin/bash
API_SERVER="http://your-server-ip"

echo "=== Bulk Test Data Creation ==="

# Çoklu cihaz oluşturma
for i in {1..5}; do
  echo "Creating test device $i..."
  
  MAC_SUFFIX=$(printf "%02d" $i)
  IP_SUFFIX=$((100 + i))
  
  curl -X POST "$API_SERVER/api/device" \
    -H "Content-Type: application/json" \
    -d '{
      "name": "BULK-TEST-PC-'$i'",
      "macAddress": "00:1B:44:11:3A:'$MAC_SUFFIX'",
      "ipAddress": "192.168.1.'$IP_SUFFIX'",
      "deviceType": "PC",
      "model": "Test PC Model '$i'",
      "location": "Test Lab Floor '$i'",
      "status": 0,
      "hardwareInfo": {
        "cpu": "Intel Core i7-12700K",
        "cpuCores": '$((8 + i))',
        "cpuLogical": '$((16 + i))',
        "cpuClockMHz": '$((3000 + i * 100))',
        "ramGB": '$((16 + i * 8))',
        "diskGB": '$((500 + i * 250))'
      },
      "softwareInfo": {
        "operatingSystem": "Windows 11 Pro",
        "osVersion": "10.0.22631",
        "osArchitecture": "64-bit",
        "registeredUser": "TestUser'$i'",
        "activeUser": "testuser'$i'"
      }
    }' -s | jq '.id, .name'
  
  sleep 1
done

echo -e "\nAll test devices created. Checking total count..."
curl -X GET "$API_SERVER/api/device" -s | jq 'length'
```

### 3. Performans Testi

```bash
#!/bin/bash
API_SERVER="http://your-server-ip"

echo "=== Performance Test ==="

# Eş zamanlı istekler için test
echo "Testing concurrent requests..."

# 10 eş zamanlı GET isteği
for i in {1..10}; do
  (
    START_TIME=$(date +%s.%3N)
    curl -X GET "$API_SERVER/api/device" -s -o /dev/null -w "%{time_total}"
    END_TIME=$(date +%s.%3N)
    echo "Request $i completed in: ${END_TIME}s"
  ) &
done

wait

# Memory ve CPU kullanımını kontrol et
echo -e "\nServer resource usage:"
curl -X GET "$API_SERVER/api/device" -s -o /dev/null -w "Response Time: %{time_total}s\nTotal Time: %{time_total}s\n"
```

## Monitoring ve Logging

### 1. API Log Kontrolü

```bash
# Systemd service loglarını kontrol et
sudo journalctl -u inventoryapi -f

# API log dosyalarını kontrol et
ls -la /opt/inventoryapi/ApiLogs/
tail -f /opt/inventoryapi/ApiLogs/api-log-$(date +%Y-%m-%d-%H).json

# Error loglarını filtrele
sudo journalctl -u inventoryapi --since "1 hour ago" | grep -i error
```

### 2. Nginx Access Logları

```bash
# Nginx access loglarını izle
sudo tail -f /var/log/nginx/access.log | grep inventoryapi

# Error loglarını kontrol et
sudo tail -f /var/log/nginx/error.log
```

### 3. Database Monitoring

```bash
# SQL Server connection testi
sqlcmd -S localhost -U inventoryuser -P 'StrongPassword123!' -Q "SELECT COUNT(*) as DeviceCount FROM InventoryDB.dbo.Devices"

# PostgreSQL connection testi
PGPASSWORD='StrongPassword123!' psql -h localhost -U inventoryuser -d inventorydb -c "SELECT COUNT(*) FROM devices;"

# SQLite testi
sqlite3 /opt/inventoryapi/inventory.db "SELECT COUNT(*) FROM devices;"
```

### 4. Health Check Endpoint

```bash
# Basit health check
curl -X GET "$API_SERVER/health" -w "Status: %{http_code}\nResponse Time: %{time_total}s\n"

# Detaylı sistem durumu
curl -X GET "$API_SERVER/api/device" -I -w "Status: %{http_code}\n"
```

### 5. Automated Testing Script

```bash
#!/bin/bash
# automated_test.sh

API_SERVER="${1:-http://localhost:5000}"
TEST_COUNT=0
PASS_COUNT=0
FAIL_COUNT=0

run_test() {
    local test_name="$1"
    local command="$2"
    local expected_status="$3"
    
    echo "Running: $test_name"
    TEST_COUNT=$((TEST_COUNT + 1))
    
    response=$(eval "$command" 2>/dev/null)
    status=$?
    
    if [ $status -eq $expected_status ]; then
        echo "✅ PASS: $test_name"
        PASS_COUNT=$((PASS_COUNT + 1))
    else
        echo "❌ FAIL: $test_name (Expected: $expected_status, Got: $status)"
        FAIL_COUNT=$((FAIL_COUNT + 1))
    fi
    echo
}

echo "Starting automated tests for: $API_SERVER"
echo "=============================================="

# Test 1: API reachability
run_test "API Reachability" "curl -f -s '$API_SERVER/api/device' > /dev/null" 0

# Test 2: Swagger UI
run_test "Swagger UI Access" "curl -f -s '$API_SERVER/swagger' > /dev/null" 0

# Test 3: Device creation
run_test "Device Creation" "curl -f -s -X POST '$API_SERVER/api/device' -H 'Content-Type: application/json' -d '{\"name\":\"AutoTest\",\"macAddress\":\"00:1B:44:11:3A:FF\",\"ipAddress\":\"192.168.1.255\",\"deviceType\":\"Test\"}' > /dev/null" 0

# Test 4: Log submission
run_test "Log Submission" "curl -f -s -X POST '$API_SERVER/api/logging' -H 'Content-Type: application/json' -d '{\"source\":\"AutoTest\",\"level\":\"Info\",\"message\":\"Test message\"}' > /dev/null" 0

echo "=============================================="
echo "Test Results:"
echo "Total Tests: $TEST_COUNT"
echo "Passed: $PASS_COUNT"
echo "Failed: $FAIL_COUNT"
echo "Success Rate: $(( PASS_COUNT * 100 / TEST_COUNT ))%"

if [ $FAIL_COUNT -eq 0 ]; then
    echo "🎉 All tests passed!"
    exit 0
else
    echo "⚠️ Some tests failed. Check the output above."
    exit 1
fi
```

Test script'ini çalıştır:
```bash
chmod +x automated_test.sh
./automated_test.sh http://your-server-ip
```

Bu rehberle sunucunuzu kurabilir ve API'nizi gerçek verilerle test edebilirsiniz. Tüm adımları takip ederek production'a hazır bir inventory management sistemi elde edebilirsiniz.