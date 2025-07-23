# Inventory Management System - Comprehensive Technical Documentation

## Table of Contents
1. [Overview](#overview)
2. [Architecture](#architecture)
3. [Docker Implementation](#docker-implementation)
4. [Installation Guide](#installation-guide)
5. [Testing Guide](#testing-guide)
6. [Platform Support](#platform-support)
7. [API Documentation](#api-documentation)
8. [Network Scanning](#network-scanning)
9. [Change Logging](#change-logging)
10. [Troubleshooting](#troubleshooting)
11. [Development](#development)

---

## Overview

Inventory Management System is a .NET 8.0-based inventory management system designed for comprehensive device tracking and monitoring. The system supports both agent-based and network-discovery methods for collecting device information.

### Key Features
- **Cross-platform support** (Windows and Linux)
- **Docker containerization** for easy deployment
- **RESTful API** with Swagger documentation
- **Network device discovery**
- **Agent-based detailed monitoring**
- **Change tracking and logging**
- **Multiple database support** (SQLite, SQL Server, PostgreSQL)

### Technology Stack
- **.NET 8.0** - Main framework
- **ASP.NET Core** - Web API
- **Entity Framework Core** - Database ORM
- **SQLite/SQL Server/PostgreSQL** - Database options
- **Docker** - Containerization
- **Swagger/OpenAPI** - API documentation

---

## Architecture

The system follows Clean Architecture principles with a layered approach:

```
┌─────────────────────────────────────────────────────────────┐
│                    Client Applications                      │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────┐ │
│  │ Windows Agent   │  │ Web Interface   │  │ Mobile App  │ │
│  └─────────────────┘  └─────────────────┘  └─────────────┘ │
└─────────────────────────────────────────────────────────────┘
                              │ HTTP/HTTPS
┌─────────────────────────────────────────────────────────────┐
│                    Inventory.Api                            │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────┐ │
│  │ Controllers     │  │ Middlewares     │  │ Services    │ │
│  └─────────────────┘  └─────────────────┘  └─────────────┘ │
└─────────────────────────────────────────────────────────────┘
                              │
┌─────────────────────────────────────────────────────────────┐
│                  Business Logic Layer                      │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────┐ │
│  │ Inventory.Domain│  │ Inventory.Shared│  │ Services    │ │
│  └─────────────────┘  └─────────────────┘  └─────────────┘ │
└─────────────────────────────────────────────────────────────┘
                              │
┌─────────────────────────────────────────────────────────────┐
│                    Data Access Layer                       │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────┐ │
│  │ Inventory.Data  │  │ DbContext       │  │ Database    │ │
│  └─────────────────┘  └─────────────────┘  └─────────────┘ │
└─────────────────────────────────────────────────────────────┘
```

### Project Structure
- **Inventory.Api** - Web API layer with controllers and services
- **Inventory.Domain** - Entity models and business logic
- **Inventory.Data** - Data access layer and Entity Framework context
- **Inventory.Shared** - Shared utilities and helpers
- **Inventory.Agent.Windows** - Cross-platform agent for device monitoring

---

## Docker Implementation

### Container Architecture

The Docker implementation provides a containerized environment for the entire inventory management system.

```
┌─────────────────────────────────────────────────────────┐
│                 Docker Environment                     │
│                                                         │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐    │
│  │   Nginx     │  │     API     │  │  Database   │    │
│  │  (Proxy)    │  │ (Container) │  │ (Optional)  │    │
│  │   Port 80   │  │  Port 5000  │  │ Port 1433   │    │
│  └─────────────┘  └─────────────┘  └─────────────┘    │
│                                                         │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐    │
│  │    Redis    │  │    Agent    │  │   Volumes   │    │
│  │ (Optional)  │  │   (Test)    │  │   (Data)    │    │
│  │ Port 6379   │  │     ---     │  │     ---     │    │
│  └─────────────┘  └─────────────┘  └─────────────┘    │
└─────────────────────────────────────────────────────────┘
```

### Docker Files

#### Main Dockerfile
```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY *.sln ./
COPY Inventory.Api/Inventory.Api.csproj ./Inventory.Api/
# ... other project files
RUN dotnet restore
COPY . .
WORKDIR /src/Inventory.Api
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
RUN apt-get update && apt-get install -y iputils-ping net-tools
COPY --from=build /app/publish .
EXPOSE 5000
ENTRYPOINT ["dotnet", "Inventory.Api.dll"]
```

#### Docker Compose Options

**Simple Setup (SQLite)**
```yaml
version: '3.8'
services:
  inventory-api:
    build: .
    ports:
      - "5000:5000"
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ConnectionStrings__DefaultConnection=Data Source=/app/Data/inventory.db
    volumes:
      - ./Data:/app/Data
```

**Production Setup (SQL Server)**
```yaml
version: '3.8'
services:
  inventory-api:
    build: .
    ports:
      - "5000:5000"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ConnectionStrings__DefaultConnection=Server=sqlserver;Database=InventoryDB;User Id=sa;Password=YourStrong@Password123;
    depends_on:
      - sqlserver
  
  sqlserver:
    image: mcr.microsoft.com/mssql/server:2022-latest
    environment:
      - ACCEPT_EULA=Y
      - SA_PASSWORD=YourStrong@Password123
    ports:
      - "1433:1433"
```

---

## Installation Guide

### Prerequisites
- Docker and Docker Compose
- Git
- 4GB+ RAM
- 10GB+ disk space

### Quick Start (Docker)

#### 1. Clone the Repository
```bash
git clone https://github.com/hizir-ceylan/InventoryManagementSystem.git
cd InventoryManagementSystem
```

#### 2. Simple Setup with SQLite
```bash
# Start with SQLite (easiest for testing)
docker-compose -f docker-compose.simple.yml up --build -d

# Check status
docker-compose -f docker-compose.simple.yml ps

# View logs
docker-compose -f docker-compose.simple.yml logs -f
```

#### 3. Production Setup with SQL Server
```bash
# Start with SQL Server (recommended for production)
docker-compose up --build -d

# Check status
docker-compose ps

# View logs
docker-compose logs -f
```

#### 4. Access the Application
- **API**: http://localhost:5000
- **Swagger UI**: http://localhost:5000/swagger
- **With Nginx**: http://localhost (port 80)

### Manual Installation (Without Docker)

#### Prerequisites
- .NET 8.0 SDK
- SQL Server / PostgreSQL / SQLite

#### Steps
```bash
# 1. Clone repository
git clone https://github.com/hizir-ceylan/InventoryManagementSystem.git
cd InventoryManagementSystem

# 2. Restore packages
dotnet restore

# 3. Build solution
dotnet build --configuration Release

# 4. Configure database (edit appsettings.json)
cd Inventory.Api
# Edit ConnectionStrings section

# 5. Run API
dotnet run --urls="http://0.0.0.0:5000"
```

---

## Testing Guide

### Docker Testing

#### Automated Testing Script
Use the provided test script for comprehensive testing:

```bash
# Run all tests
./scripts/test-docker.sh test

# Check container status
./scripts/test-docker.sh status

# View logs
./scripts/test-docker.sh logs

# Cleanup
./scripts/test-docker.sh cleanup
```

#### Manual Testing Steps

**1. Container Health Check**
```bash
# Check if containers are running
docker-compose -f docker-compose.simple.yml ps

# Check container logs
docker-compose -f docker-compose.simple.yml logs inventory-api
```

**2. API Functionality Testing**
```bash
# Test API availability
curl http://localhost:5000/api/device

# Test device creation
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

# Test log submission
curl -X POST "http://localhost:5000/api/logging" \
  -H "Content-Type: application/json" \
  -d '{
    "source": "TestAgent",
    "level": "Info",
    "message": "Test log message"
  }'
```

**3. Data Persistence Testing**
```bash
# Check SQLite database
ls -la ./Data/SQLite/
sqlite3 ./Data/SQLite/inventory.db "SELECT * FROM Devices;"

# Check log files
ls -la ./Data/ApiLogs/
```

### Performance Testing

#### Basic Load Testing
```bash
# Install Apache Bench (if not available)
# Ubuntu: sudo apt-get install apache2-utils
# macOS: brew install httpie

# Run 100 requests with 10 concurrent connections
ab -n 100 -c 10 http://localhost:5000/api/device

# Alternative with curl
for i in {1..10}; do
  curl -s http://localhost:5000/api/device &
done
wait
```

#### Stress Testing
```bash
# Monitor resource usage during load
docker stats inventory-api-simple

# Test with higher load
ab -n 1000 -c 50 http://localhost:5000/api/device
```

### Integration Testing

#### Agent-API Integration
```bash
# Run agent container to test integration
docker-compose -f docker-compose.simple.yml up inventory-agent

# Check if agent data is received by API
curl http://localhost:5000/api/device | jq .
```

#### Network Scanning Testing
```bash
# Trigger network scan
curl -X POST "http://localhost:5000/api/networkscan/start" \
  -H "Content-Type: application/json" \
  -d '{"networkRange": "172.20.0.0/16"}'

# Check scan status
curl http://localhost:5000/api/networkscan/status

# Get discovered devices
curl http://localhost:5000/api/networkscan/devices
```

---

## Platform Support

### Cross-Platform Compatibility

The system supports both Windows and Linux platforms with automatic platform detection.

#### Windows Support
- **WMI Integration**: Comprehensive system information gathering
- **GPU Monitoring**: Support for LibreHardwareMonitor
- **Windows Services**: Background service installation
- **Registry Access**: Windows-specific configuration

#### Linux Support
- **Proc Filesystem**: System information from /proc
- **DMI Decode**: Hardware information gathering
- **Package Managers**: Support for apt, yum, dnf, pacman
- **Systemd Integration**: Service management
- **Network Tools**: ip, lshw, lscpu integration

#### Platform Detection
```csharp
if (CrossPlatformSystemInfo.IsWindows)
{
    // Windows-specific implementation
    return WindowsSystemInfo.GetSystemInfo();
}
else if (CrossPlatformSystemInfo.IsLinux)
{
    // Linux-specific implementation
    return LinuxSystemInfo.GetSystemInfo();
}
```

### Change Logging

#### Separate Change Files
The system creates separate files for device changes:

- **Main logs**: `LocalLogs/device-log-{date}.json`
- **Change files**: `LocalLogs/Changes/device-changes-{date}-{time}.json`

#### Change File Format
```json
{
  "DetectedAt": "2025-07-16T07:49:13.0635368+00:00",
  "DeviceName": "hostname",
  "Changes": {
    "Diff": {
      "HardwareInfo.RamGB": {
        "ChangedValues": [
          {
            "Field": "RamGB",
            "OldValue": 8,
            "NewValue": 16
          }
        ]
      }
    }
  }
}
```

---

## API Documentation

### Authentication
Currently, the API operates without authentication. For production use, implement appropriate authentication mechanisms.

### Base URL
- **Local**: `http://localhost:5000`
- **Docker**: `http://localhost:5000`
- **Production**: `https://your-domain.com`

### Main Endpoints

#### Device Management
```
GET    /api/device              - List all devices
POST   /api/device              - Create new device
GET    /api/device/{id}         - Get device by ID
PUT    /api/device/{id}         - Update device
DELETE /api/device/{id}         - Delete device
```

#### Logging
```
POST   /api/logging             - Submit log entry
GET    /api/logging/recent      - Get recent logs
GET    /api/logging/sources     - Get log sources
```

#### Network Scanning
```
POST   /api/networkscan/start   - Start network scan
GET    /api/networkscan/status  - Get scan status
GET    /api/networkscan/devices - Get discovered devices
```

### Request/Response Examples

#### Create Device
```bash
curl -X POST "http://localhost:5000/api/device" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "WORKSTATION-001",
    "macAddress": "00:1B:44:11:3A:B7",
    "ipAddress": "192.168.1.100",
    "deviceType": "PC",
    "model": "Dell OptiPlex 7090",
    "location": "Office-101",
    "status": 0,
    "hardwareInfo": {
      "cpu": "Intel Core i7-12700",
      "cpuCores": 12,
      "ramGB": 32,
      "diskGB": 512
    },
    "softwareInfo": {
      "operatingSystem": "Windows 11 Pro",
      "osVersion": "10.0.22631"
    }
  }'
```

#### Submit Log
```bash
curl -X POST "http://localhost:5000/api/logging" \
  -H "Content-Type: application/json" \
  -d '{
    "source": "Agent-001",
    "level": "Info",
    "message": "System scan completed successfully",
    "data": {
      "scanDuration": "00:02:15",
      "devicesFound": 5
    }
  }'
```

---

## Network Scanning

### Automated Network Discovery

The system can automatically discover devices on the network using various methods:

#### Ping Sweep
- ICMP ping to discover active IP addresses
- Configurable network ranges
- Timeout and retry settings

#### Port Scanning
- Common service port detection
- Service identification
- Response time measurement

#### MAC Address Resolution
- ARP table lookup
- Vendor identification
- Device type detection

### Configuration
```json
{
  "NetworkScan": {
    "Enabled": true,
    "Interval": "01:00:00",
    "NetworkRange": "192.168.1.0/24",
    "Timeout": 5000,
    "MaxConcurrency": 50
  }
}
```

---

## Troubleshooting

### Common Issues

#### Docker Container Won't Start
```bash
# Check container logs
docker-compose logs inventory-api

# Check system resources
docker system df
docker stats

# Remove and rebuild
docker-compose down -v
docker-compose up --build
```

#### Database Connection Issues
```bash
# SQLite permission issues
chmod 666 ./Data/SQLite/inventory.db
chown -R $(id -u):$(id -g) ./Data/

# SQL Server connection issues
docker-compose logs sqlserver
# Check SA password and connection string
```

#### API Not Responding
```bash
# Check if API is listening
netstat -tlnp | grep 5000
docker-compose ps

# Check API logs
docker-compose logs -f inventory-api

# Test direct connection
curl -v http://localhost:5000/api/device
```

#### Agent Connection Issues
```bash
# Check agent logs
docker-compose logs inventory-agent

# Verify API URL configuration
docker-compose exec inventory-agent env | grep API

# Test network connectivity
docker-compose exec inventory-agent ping inventory-api
```

### Performance Issues

#### High Memory Usage
```bash
# Monitor container resources
docker stats inventory-api-simple

# Check for memory leaks in logs
docker-compose logs inventory-api | grep -i memory

# Restart container if needed
docker-compose restart inventory-api
```

#### Slow API Response
```bash
# Check database size
ls -lh ./Data/SQLite/inventory.db

# Monitor active connections
docker-compose exec sqlserver /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P 'YourStrong@Password123' -Q "SELECT DB_NAME() AS DatabaseName, COUNT(*) AS ActiveConnections FROM sys.dm_exec_sessions WHERE database_id = DB_ID()"

# Analyze slow queries (if using SQL Server)
# Enable query store and check performance views
```

### Log Analysis

#### API Logs
```bash
# View real-time logs
tail -f ./Data/ApiLogs/api-log-$(date +%Y-%m-%d-%H).json

# Search for errors
grep -i error ./Data/ApiLogs/*.json

# Count request types
grep -o '"path":"[^"]*"' ./Data/ApiLogs/*.json | sort | uniq -c
```

#### Agent Logs
```bash
# View agent logs
cat ./Data/AgentLogs/device-log-$(date +%Y-%m-%d)-*.json

# Check change logs
ls -la ./Data/AgentLogs/Changes/
```

---

## Development

### Development Setup

#### Prerequisites
- .NET 8.0 SDK
- Docker Desktop
- Visual Studio Code or Visual Studio
- Git

#### Local Development
```bash
# Clone repository
git clone https://github.com/hizir-ceylan/InventoryManagementSystem.git
cd InventoryManagementSystem

# Restore packages
dotnet restore

# Run in development mode
cd Inventory.Api
dotnet run --environment Development
```

#### Hot Reload Development
```bash
# Enable hot reload for API development
dotnet watch run --project Inventory.Api --environment Development
```

### Building and Testing

#### Build All Projects
```bash
dotnet build --configuration Release
```

#### Run Tests
```bash
# If tests exist
dotnet test

# Manual API testing
./scripts/test-docker.sh test
```

#### Docker Development
```bash
# Build development image
docker build -t inventory-api:dev .

# Run with development settings
docker run -p 5000:5000 -e ASPNETCORE_ENVIRONMENT=Development inventory-api:dev
```

### Contributing

#### Code Style
- Follow .NET coding conventions
- Use meaningful variable and method names
- Add XML documentation for public APIs
- Include error handling and logging

#### Pull Request Process
1. Fork the repository
2. Create a feature branch
3. Make changes with appropriate tests
4. Ensure Docker build succeeds
5. Update documentation if needed
6. Submit pull request

### Configuration

#### Environment Variables
```bash
# API Configuration
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://+:5000
ConnectionStrings__DefaultConnection=...

# Agent Configuration
ApiSettings__BaseUrl=http://localhost:5000
ApiSettings__Timeout=30
ApiSettings__RetryCount=3
```

#### Configuration Files
- `appsettings.json` - Base configuration
- `appsettings.Development.json` - Development overrides
- `appsettings.Production.json` - Production overrides
- `appsettings.Docker.json` - Docker-specific settings

---

## Conclusion

This comprehensive documentation covers all aspects of the Inventory Management System, from basic installation to advanced troubleshooting. The Docker implementation provides a robust, scalable solution for device inventory management across different platforms.

For additional support or contributions, please refer to the project repository and follow the established development guidelines.