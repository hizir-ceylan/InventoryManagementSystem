#!/bin/bash

# Inventory Management System - Quick Start Script
# Bu script Docker kullanarak sistemi hızlı bir şekilde başlatır

set -e

echo "=========================================="
echo "Inventory Management System Quick Start"
echo "=========================================="

# Renk kodları
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
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

# Docker kontrol
check_docker() {
    if ! command -v docker &> /dev/null; then
        log_error "Docker bulunamadı! Lütfen Docker'ı kurun."
        echo "Docker kurulum: https://docs.docker.com/get-docker/"
        exit 1
    fi
    
    if ! docker info &> /dev/null; then
        log_error "Docker çalışmıyor! Lütfen Docker'ı başlatın."
        exit 1
    fi
    
    # Docker Compose kontrol
    if ! command -v docker-compose &> /dev/null && ! docker compose version &> /dev/null; then
        log_error "Docker Compose bulunamadı!"
        exit 1
    fi
    
    if docker compose version &> /dev/null; then
        DOCKER_COMPOSE="docker compose"
    else
        DOCKER_COMPOSE="docker-compose"
    fi
    
    log_info "Docker kontrolü başarılı ✅"
}

# Setup seçimi
choose_setup() {
    echo
    echo "Hangi setup'ı kullanmak istiyorsunuz?"
    echo "1) Simple (SQLite) - Test için hızlı başlangıç"
    echo "2) Production (SQL Server) - Tam özellikli setup"
    echo "3) Manuel build ve test"
    echo
    read -p "Seçiminiz (1-3): " setup_choice
    
    case $setup_choice in
        1)
            COMPOSE_FILE="docker-compose.simple.yml"
            SETUP_NAME="Simple (SQLite)"
            ;;
        2)
            COMPOSE_FILE="docker-compose.yml"
            SETUP_NAME="Production (SQL Server)"
            ;;
        3)
            manual_setup
            return
            ;;
        *)
            log_error "Geçersiz seçim!"
            exit 1
            ;;
    esac
}

# Manuel setup
manual_setup() {
    log_info "Manuel build başlatılıyor..."
    
    # Build image
    log_info "Docker image build ediliyor..."
    docker build -t inventory-api:latest .
    
    # Run simple container
    log_info "Container başlatılıyor..."
    docker run -d \
        --name inventory-api-manual \
        -p 5000:5000 \
        -e ASPNETCORE_ENVIRONMENT=Development \
        -e ConnectionStrings__DefaultConnection="Data Source=/app/Data/inventory.db" \
        -v "$(pwd)/Data:/app/Data" \
        inventory-api:latest
    
    log_info "Manuel setup tamamlandı!"
    echo "API: http://localhost:5000"
    echo "Swagger: http://localhost:5000/swagger"
    return
}

# Ana setup
start_setup() {
    log_info "$SETUP_NAME setup başlatılıyor..."
    
    # Önceki container'ları durdur
    log_info "Önceki container'lar durduruluyor..."
    $DOCKER_COMPOSE -f $COMPOSE_FILE down 2>/dev/null || true
    
    # Data dizinleri oluştur
    mkdir -p Data/ApiLogs Data/SQLite Data/AgentLogs
    
    # Build ve start
    log_info "Build ve start işlemi başlatılıyor..."
    $DOCKER_COMPOSE -f $COMPOSE_FILE up --build -d
    
    if [ $? -eq 0 ]; then
        log_info "Setup başarılı! ✅"
    else
        log_error "Setup başarısız! ❌"
        exit 1
    fi
}

# Sistem kontrolü
check_system() {
    log_info "Sistem kontrolü yapılıyor..."
    
    # API'nin hazır olmasını bekle
    local counter=0
    while [ $counter -lt 30 ]; do
        if curl -f -s http://localhost:5000/api/device > /dev/null 2>&1; then
            log_info "API hazır! ✅"
            break
        fi
        echo -n "."
        sleep 2
        counter=$((counter + 1))
    done
    
    if [ $counter -eq 30 ]; then
        log_warn "API 60 saniye içinde hazır olmadı"
        log_info "Manuel kontrol için: docker-compose -f $COMPOSE_FILE logs"
    fi
}

# Bilgilendirme
show_info() {
    echo
    echo "=========================================="
    log_info "🎉 Inventory Management System hazır!"
    echo "=========================================="
    echo
    echo "📍 Erişim Bilgileri:"
    echo "   • API: http://localhost:5000"
    echo "   • Swagger UI: http://localhost:5000/swagger"
    if [ "$COMPOSE_FILE" = "docker-compose.yml" ]; then
        echo "   • Nginx: http://localhost"
        echo "   • SQL Server: localhost:1433"
    fi
    echo
    echo "🔧 Yararlı Komutlar:"
    echo "   • Container durumu: $DOCKER_COMPOSE -f $COMPOSE_FILE ps"
    echo "   • Logları görüntüle: $DOCKER_COMPOSE -f $COMPOSE_FILE logs -f"
    echo "   • Durdur: $DOCKER_COMPOSE -f $COMPOSE_FILE down"
    echo "   • Test script: ./test-docker.sh test"
    echo
    echo "📖 Dokümantasyon:"
    echo "   • Docker rehberi: docs/DOCKER-GUIDE.md"
    echo "   • Tam dokümantasyon: docs/COMPLETE-DOCUMENTATION.md"
    echo
}

# Test önerileri
suggest_tests() {
    echo "🧪 Test Önerileri:"
    echo "   • Otomatik test: ./test-docker.sh test"
    echo "   • Manuel API test: curl http://localhost:5000/api/device"
    echo "   • Cihaz ekleme test için Swagger UI kullanın"
    echo
}

# Ana fonksiyon
main() {
    case "${1:-start}" in
        "start"|"")
            check_docker
            choose_setup
            start_setup
            check_system
            show_info
            suggest_tests
            ;;
        "stop")
            log_info "Tüm container'lar durduruluyor..."
            docker-compose -f docker-compose.simple.yml down 2>/dev/null || true
            docker-compose down 2>/dev/null || true
            docker stop inventory-api-manual 2>/dev/null || true
            docker rm inventory-api-manual 2>/dev/null || true
            log_info "Durdurma işlemi tamamlandı"
            ;;
        "status")
            echo "Container Durumu:"
            docker ps --filter "name=inventory" --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}"
            ;;
        "logs")
            echo "Hangi container'ın loglarını görmek istiyorsunız?"
            docker ps --filter "name=inventory" --format "{{.Names}}" | nl
            read -p "Seçim (numara): " log_choice
            container_name=$(docker ps --filter "name=inventory" --format "{{.Names}}" | sed -n "${log_choice}p")
            if [ ! -z "$container_name" ]; then
                docker logs -f "$container_name"
            fi
            ;;
        *)
            echo "Kullanım: $0 [start|stop|status|logs]"
            echo "  start  - Sistemi başlat (varsayılan)"
            echo "  stop   - Tüm container'ları durdur"
            echo "  status - Container durumunu göster"
            echo "  logs   - Container loglarını göster"
            exit 1
            ;;
    esac
}

main "$@"