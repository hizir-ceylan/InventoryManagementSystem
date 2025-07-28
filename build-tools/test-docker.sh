#!/bin/bash

# Docker Test Script for Inventory Management System
# This script tests the Docker setup and API functionality

set -e

echo "=================================================="
echo "Inventory Management System - Docker Test Script"
echo "=================================================="

# Configuration
API_URL="http://localhost:5093"
WAIT_TIMEOUT=60
TEST_DEVICE_NAME="DOCKER-TEST-$(date +%s)"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Helper functions
log_info() {
    echo -e "${GREEN}[INFO]${NC} $1"
}

log_warn() {
    echo -e "${YELLOW}[WARN]${NC} $1"
}

log_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# Check if docker and docker-compose are available
check_dependencies() {
    log_info "Checking dependencies..."
    
    if ! command -v docker &> /dev/null; then
        log_error "Docker is not installed or not in PATH"
        exit 1
    fi
    
    if ! command -v docker-compose &> /dev/null && ! docker compose version &> /dev/null; then
        log_error "Docker Compose is not installed or not in PATH"
        exit 1
    fi
    
    # Prefer docker compose over docker-compose
    if docker compose version &> /dev/null; then
        DOCKER_COMPOSE="docker compose"
    else
        DOCKER_COMPOSE="docker-compose"
    fi
    
    log_info "Dependencies check passed"
}

# Build and start containers
start_containers() {
    log_info "Building and starting containers..."
    
    # Stop any existing containers
    $DOCKER_COMPOSE -f docker-compose.simple.yml down || true
    
    # Build and start
    $DOCKER_COMPOSE -f docker-compose.simple.yml up --build -d
    
    if [ $? -eq 0 ]; then
        log_info "Containers started successfully"
    else
        log_error "Failed to start containers"
        exit 1
    fi
}

# Wait for API to be ready
wait_for_api() {
    log_info "Waiting for API to be ready..."
    
    local counter=0
    while [ $counter -lt $WAIT_TIMEOUT ]; do
        if curl -f -s $API_URL/api/device > /dev/null 2>&1; then
            log_info "API is ready!"
            return 0
        fi
        
        echo -n "."
        sleep 1
        counter=$((counter + 1))
    done
    
    log_error "API did not become ready within $WAIT_TIMEOUT seconds"
    return 1
}

# Test API endpoints
test_api_endpoints() {
    log_info "Testing API endpoints..."
    
    # Test 1: Health check
    echo "  Testing health check..."
    if curl -f -s $API_URL/api/device > /dev/null; then
        log_info "✅ Health check passed"
    else
        log_error "❌ Health check failed"
        return 1
    fi
    
    # Test 2: Swagger UI
    echo "  Testing Swagger UI..."
    if curl -f -s $API_URL/swagger > /dev/null; then
        log_info "✅ Swagger UI accessible"
    else
        log_warn "⚠️  Swagger UI not accessible"
    fi
    
    # Test 3: Device creation
    echo "  Testing device creation..."
    local device_response=$(curl -s -X POST "$API_URL/api/device" \
        -H "Content-Type: application/json" \
        -d '{
            "name": "'$TEST_DEVICE_NAME'",
            "macAddress": "00:1B:44:11:3A:99",
            "ipAddress": "172.20.0.99",
            "deviceType": "PC",
            "model": "Docker Test PC",
            "location": "Test Environment",
            "status": 0
        }')
    
    if echo "$device_response" | grep -q "id"; then
        log_info "✅ Device creation successful"
        DEVICE_ID=$(echo "$device_response" | grep -o '"id":"[^"]*"' | cut -d'"' -f4)
        log_info "Created device ID: $DEVICE_ID"
    else
        log_error "❌ Device creation failed"
        echo "Response: $device_response"
        return 1
    fi
    
    # Test 4: Device retrieval
    echo "  Testing device retrieval..."
    if curl -f -s "$API_URL/api/device" | grep -q "$TEST_DEVICE_NAME"; then
        log_info "✅ Device retrieval successful"
    else
        log_error "❌ Device retrieval failed"
        return 1
    fi
    
    # Test 5: Log submission
    echo "  Testing log submission..."
    local log_response=$(curl -s -X POST "$API_URL/api/logging" \
        -H "Content-Type: application/json" \
        -d '{
            "source": "DockerTest",
            "level": "Info",
            "message": "Test log message from Docker test script",
            "data": {
                "testProperty": "testValue",
                "timestamp": "'$(date -u +%Y-%m-%dT%H:%M:%SZ)'"
            }
        }')
    
    if echo "$log_response" | grep -q "success\|ok\|id" || [ -z "$log_response" ]; then
        log_info "✅ Log submission successful"
    else
        log_warn "⚠️  Log submission may have failed"
        echo "Response: $log_response"
    fi
}

# Test container logs
test_container_logs() {
    log_info "Checking container logs..."
    
    # Check API container logs
    log_info "API Container logs (last 20 lines):"
    $DOCKER_COMPOSE -f docker-compose.simple.yml logs --tail=20 inventory-api
    
    # Check if there are any errors in logs
    local error_count=$($DOCKER_COMPOSE -f docker-compose.simple.yml logs inventory-api | grep -i error | wc -l)
    if [ $error_count -gt 0 ]; then
        log_warn "Found $error_count error entries in API logs"
    else
        log_info "✅ No errors found in API logs"
    fi
}

# Test data persistence
test_data_persistence() {
    log_info "Testing data persistence..."
    
    # Check if SQLite database file was created
    if [ -f "./Data/SQLite/inventory.db" ]; then
        log_info "✅ SQLite database file created"
        
        # Check if we can query the database
        if command -v sqlite3 &> /dev/null; then
            local device_count=$(sqlite3 ./Data/SQLite/inventory.db "SELECT COUNT(*) FROM Devices;" 2>/dev/null || echo "0")
            log_info "Database contains $device_count devices"
        fi
    else
        log_warn "⚠️  SQLite database file not found"
    fi
    
    # Check log files
    if [ -d "./Data/ApiLogs" ] && [ "$(ls -A ./Data/ApiLogs)" ]; then
        log_info "✅ API log files created"
        ls -la ./Data/ApiLogs/
    else
        log_warn "⚠️  API log files not found"
    fi
}

# Performance test
test_performance() {
    log_info "Running basic performance test..."
    
    # Simple load test with 10 concurrent requests
    log_info "Testing with 10 concurrent requests..."
    
    for i in {1..10}; do
        (
            start_time=$(date +%s.%3N)
            curl -s "$API_URL/api/device" > /dev/null
            end_time=$(date +%s.%3N)
            response_time=$(echo "$end_time - $start_time" | bc)
            echo "Request $i: ${response_time}s"
        ) &
    done
    wait
    
    log_info "✅ Performance test completed"
}

# Cleanup
cleanup() {
    log_info "Cleaning up test environment..."
    
    if [ ! -z "$1" ] && [ "$1" = "full" ]; then
        $DOCKER_COMPOSE -f docker-compose.simple.yml down -v
        log_info "Containers stopped and volumes removed"
    else
        log_info "Containers left running. Use './test-docker.sh cleanup' to stop them"
    fi
}

# Main execution
main() {
    case "${1:-test}" in
        "test")
            check_dependencies
            start_containers
            
            if wait_for_api; then
                test_api_endpoints
                test_container_logs
                test_data_persistence
                test_performance
                
                log_info "=================================================="
                log_info "🎉 All tests completed successfully!"
                log_info "API is available at: $API_URL"
                log_info "Swagger UI: $API_URL/swagger"
                log_info "=================================================="
            else
                log_error "API readiness test failed"
                exit 1
            fi
            ;;
        "cleanup")
            cleanup full
            ;;
        "logs")
            $DOCKER_COMPOSE -f docker-compose.simple.yml logs -f
            ;;
        "status")
            $DOCKER_COMPOSE -f docker-compose.simple.yml ps
            ;;
        *)
            echo "Usage: $0 [test|cleanup|logs|status]"
            echo "  test    - Run full test suite (default)"
            echo "  cleanup - Stop containers and cleanup"
            echo "  logs    - Show container logs"
            echo "  status  - Show container status"
            exit 1
            ;;
    esac
}

main "$@"