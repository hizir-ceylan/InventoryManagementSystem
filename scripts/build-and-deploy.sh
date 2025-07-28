#!/bin/bash

# Inventory Management System - Build and Deploy Script
# Bu script sistemi birden fazla bilgisayara kurmak için hazırlar

set -e

echo "=========================================="
echo "Inventory Management System - Build Script"
echo "=========================================="

# Renk kodları
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
BLUE='\033[0;34m'
NC='\033[0m'

log_info() {
    echo -e "${GREEN}[INFO]${NC} $1"
}

log_warn() {
    echo -e "${YELLOW}[WARN]${NC} $1"
}

log_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

log_step() {
    echo -e "${BLUE}[STEP]${NC} $1"
}

# .NET kontrol
check_dotnet() {
    if ! command -v dotnet &> /dev/null; then
        log_error ".NET SDK bulunamadı! Lütfen .NET 8.0 SDK'yı kurun."
        echo ".NET kurulum: https://dotnet.microsoft.com/download"
        exit 1
    fi
    
    # .NET versiyonu kontrol et
    DOTNET_VERSION=$(dotnet --version)
    log_info ".NET SDK Versiyonu: $DOTNET_VERSION ✅"
}

# Build fonksiyonu
build_solution() {
    log_step "Solution build ediliyor..."
    
    # Restore
    log_info "Package'lar restore ediliyor..."
    dotnet restore InventoryManagementSystem.sln
    
    # Build
    log_info "Solution build ediliyor..."
    dotnet build InventoryManagementSystem.sln --configuration Release --no-restore
    
    if [ $? -eq 0 ]; then
        log_info "Build başarılı! ✅"
    else
        log_error "Build başarısız! ❌"
        exit 1
    fi
}

# Publish fonksiyonu
publish_applications() {
    log_step "Uygulamalar publish ediliyor..."
    
    # Publish dizinini temizle
    rm -rf publish
    mkdir -p publish
    
    # API publish
    log_info "API publish ediliyor..."
    dotnet publish Inventory.Api/Inventory.Api.csproj \
        --configuration Release \
        --output publish/api \
        --no-restore
    
    # Agent publish - Windows (restore gerekliyse)
    log_info "Agent (Windows) restore ediliyor..."
    dotnet restore Inventory.Agent.Windows/Inventory.Agent.Windows.csproj \
        --runtime win-x64
    
    log_info "Agent (Windows) publish ediliyor..."
    dotnet publish Inventory.Agent.Windows/Inventory.Agent.Windows.csproj \
        --configuration Release \
        --runtime win-x64 \
        --self-contained true \
        --output publish/agent-windows \
        --no-restore
    
    # Agent publish - Linux (restore gerekliyse)
    log_info "Agent (Linux) restore ediliyor..."
    dotnet restore Inventory.Agent.Windows/Inventory.Agent.Windows.csproj \
        --runtime linux-x64
    
    log_info "Agent (Linux) publish ediliyor..."
    dotnet publish Inventory.Agent.Windows/Inventory.Agent.Windows.csproj \
        --configuration Release \
        --runtime linux-x64 \
        --self-contained true \
        --output publish/agent-linux \
        --no-restore
    
    log_info "Publish işlemi tamamlandı! ✅"
}

# Deployment paketleri oluştur
create_deployment_packages() {
    log_step "Deployment paketleri oluşturuluyor..."
    
    mkdir -p packages
    
    # API paketi
    log_info "API paketi oluşturuluyor..."
    cd publish/api
    tar -czf ../../packages/inventory-api.tar.gz *
    cd ../..
    
    # Windows Agent paketi
    log_info "Windows Agent paketi oluşturuluyor..."
    cd publish/agent-windows
    zip -r ../../packages/inventory-agent-windows.zip *
    cd ../..
    
    # Linux Agent paketi
    log_info "Linux Agent paketi oluşturuluyor..."
    cd publish/agent-linux
    tar -czf ../../packages/inventory-agent-linux.tar.gz *
    cd ../..
    
    log_info "Deployment paketleri hazır! ✅"
}

# Kurulum talimatları oluştur
create_installation_guide() {
    log_step "Kurulum talimatları oluşturuluyor..."
    
    cat > packages/KURULUM-TALİMATLARI.md << 'EOF'
# Inventory Management System - Kurulum Talimatları

## Sistem Gereksinimleri
- .NET 8.0 Runtime
- Windows 10/11 veya Linux (Ubuntu 20.04+)
- Minimum 2GB RAM
- Minimum 1GB disk alanı

## API Kurulumu

### Windows
1. `inventory-api.tar.gz` dosyasını çıkarın
2. Klasöre gidin ve `Inventory.Api.exe` dosyasını çalıştırın
3. API şu adreste çalışacak: http://localhost:5093

### Linux
1. `inventory-api.tar.gz` dosyasını çıkarın:
   ```bash
   tar -xzf inventory-api.tar.gz -C /opt/inventory-api
   ```
2. Çalıştırın:
   ```bash
   cd /opt/inventory-api
   ./Inventory.Api
   ```

## Agent Kurulumu

### Windows
1. `inventory-agent-windows.zip` dosyasını çıkarın
2. Klasöre gidin ve `Inventory.Agent.Windows.exe` dosyasını çalıştırın

### Linux
1. `inventory-agent-linux.tar.gz` dosyasını çıkarın:
   ```bash
   tar -xzf inventory-agent-linux.tar.gz -C /opt/inventory-agent
   ```
2. Çalıştırın:
   ```bash
   cd /opt/inventory-agent
   ./Inventory.Agent.Windows
   ```

## Yapılandırma

### API Port Ayarı
API varsayılan olarak 5093 portunda çalışır. Farklı bir port kullanmak için:
```bash
./Inventory.Api --urls "http://localhost:PORTNUMARASI"
```

### Agent API Bağlantısı
Agent varsayılan olarak http://localhost:5093 adresine bağlanır.
Farklı bir API adresi için environment variable kullanın:

Windows:
```cmd
set ApiSettings__BaseUrl=http://SUNUCU_IP:5093
Inventory.Agent.Windows.exe
```

Linux:
```bash
export ApiSettings__BaseUrl=http://SUNUCU_IP:5093
./Inventory.Agent.Windows
```

### Offline Storage Konumu
Veriler varsayılan olarak kullanıcının Belgeler klasöründe saklanır:
- Windows: `C:\Users\KULLANICI\Documents\InventoryManagementSystem\`
- Linux: `/home/KULLANICI/Documents/InventoryManagementSystem/`

## Arka Plan Servisi Olarak Çalıştırma

### Windows Service
Windows Service olarak kurmak için PowerShell (Admin) ile:
```powershell
sc create "InventoryAgent" binPath="C:\Path\To\Inventory.Agent.Windows.exe"
sc start "InventoryAgent"
```

### Linux Systemd Service
1. Service dosyası oluşturun `/etc/systemd/system/inventory-agent.service`:
```ini
[Unit]
Description=Inventory Management Agent
After=network.target

[Service]
Type=simple
User=inventory
WorkingDirectory=/opt/inventory-agent
ExecStart=/opt/inventory-agent/Inventory.Agent.Windows
Restart=always
Environment=ApiSettings__BaseUrl=http://localhost:5093

[Install]
WantedBy=multi-user.target
```

2. Servisi aktifleştirin:
```bash
sudo systemctl daemon-reload
sudo systemctl enable inventory-agent
sudo systemctl start inventory-agent
```

## Ağ Keşfi Modu
Agent'ı ağ keşfi modunda çalıştırmak için:
```bash
./Inventory.Agent.Windows network
```

## Sorun Giderme
- API'nin çalıştığından emin olun: http://localhost:5093/swagger
- Agent log dosyalarını kontrol edin
- Firewall ayarlarını kontrol edin
- API ve Agent'ın aynı ağda olduğundan emin olun
EOF

    log_info "Kurulum talimatları oluşturuldu! ✅"
}

# Örnek batch/shell scriptleri oluştur
create_helper_scripts() {
    log_step "Yardımcı scriptler oluşturuluyor..."
    
    # Windows batch script for API
    cat > packages/start-api.bat << 'EOF'
@echo off
echo Starting Inventory Management System API...
echo API will be available at: http://localhost:5093
echo Swagger UI: http://localhost:5093/swagger
echo.
echo Press Ctrl+C to stop
Inventory.Api.exe --urls "http://localhost:5093"
pause
EOF

    # Windows batch script for Agent
    cat > packages/start-agent.bat << 'EOF'
@echo off
echo Starting Inventory Management System Agent...
echo.
echo This agent will:
echo - Collect system information
echo - Send data to API at http://localhost:5093
echo - Store data offline when API is not available
echo - Data will be stored in: %USERPROFILE%\Documents\InventoryManagementSystem
echo.
Inventory.Agent.Windows.exe
pause
EOF

    # Linux shell script for API
    cat > packages/start-api.sh << 'EOF'
#!/bin/bash
echo "Starting Inventory Management System API..."
echo "API will be available at: http://localhost:5093"
echo "Swagger UI: http://localhost:5093/swagger"
echo ""
echo "Press Ctrl+C to stop"
./Inventory.Api --urls "http://localhost:5093"
EOF

    # Linux shell script for Agent
    cat > packages/start-agent.sh << 'EOF'
#!/bin/bash
echo "Starting Inventory Management System Agent..."
echo ""
echo "This agent will:"
echo "- Collect system information"
echo "- Send data to API at http://localhost:5093"
echo "- Store data offline when API is not available"
echo "- Data will be stored in: $HOME/Documents/InventoryManagementSystem"
echo ""
./Inventory.Agent.Windows
EOF

    chmod +x packages/start-api.sh
    chmod +x packages/start-agent.sh

    log_info "Yardımcı scriptler oluşturuldu! ✅"
}

# Test fonksiyonu
run_tests() {
    log_step "Test ediliyor..."
    
    # API'yi test modunda başlat
    log_info "API test ediliyor..."
    cd publish/api
    timeout 10 ./Inventory.Api --urls "http://localhost:5094" &
    API_PID=$!
    sleep 3
    
    # Health check
    if curl -f -s http://localhost:5094/swagger > /dev/null; then
        log_info "API test başarılı! ✅"
    else
        log_warn "API test yanıt vermiyor, manuel test gerekebilir"
    fi
    
    # API'yi durdur
    kill $API_PID 2>/dev/null || true
    cd ../..
    
    log_info "Test tamamlandı! ✅"
}

# Özet bilgiler
show_summary() {
    echo
    echo "=========================================="
    log_info "🎉 Build işlemi tamamlandı!"
    echo "=========================================="
    echo
    echo "📦 Oluşturulan Paketler:"
    echo "   • packages/inventory-api.tar.gz (API)"
    echo "   • packages/inventory-agent-windows.zip (Windows Agent)"
    echo "   • packages/inventory-agent-linux.tar.gz (Linux Agent)"
    echo
    echo "📋 Yardımcı Dosyalar:"
    echo "   • packages/KURULUM-TALİMATLARI.md"
    echo "   • packages/start-api.bat/sh"
    echo "   • packages/start-agent.bat/sh"
    echo
    echo "🚀 Sonraki Adımlar:"
    echo "   1. packages/ klasörünü hedef bilgisayarlara kopyalayın"
    echo "   2. KURULUM-TALİMATLARI.md dosyasını okuyun"
    echo "   3. İlgili paketleri çıkarıp kurun"
    echo "   4. API'yi önce başlatın, sonra Agent'ları çalıştırın"
    echo
    echo "⚙️  Önemli Notlar:"
    echo "   • API varsayılan port: 5093"
    echo "   • Agent varsayılan API adresi: http://localhost:5093"
    echo "   • Offline veriler: Belgeler/InventoryManagementSystem klasöründe"
    echo "   • Port çakışması varsa --urls parametresi ile değiştirin"
    echo
}

# Ana fonksiyon
main() {
    case "${1:-all}" in
        "all"|"")
            check_dotnet
            build_solution
            publish_applications
            create_deployment_packages
            create_installation_guide
            create_helper_scripts
            run_tests
            show_summary
            ;;
        "build")
            check_dotnet
            build_solution
            ;;
        "publish")
            check_dotnet
            publish_applications
            ;;
        "package")
            create_deployment_packages
            create_installation_guide
            create_helper_scripts
            ;;
        "test")
            run_tests
            ;;
        *)
            echo "Kullanım: $0 [all|build|publish|package|test]"
            echo "  all     - Tüm işlemleri yap (varsayılan)"
            echo "  build   - Sadece build yap"
            echo "  publish - Sadece publish yap"
            echo "  package - Sadece paketleme yap"
            echo "  test    - Sadece test yap"
            exit 1
            ;;
    esac
}

main "$@"