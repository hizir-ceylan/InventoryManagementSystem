# Inventory Management System - Test Guide

## 🎯 Quick Test Steps

### 1. Local Development Test (Recommended)

Since Docker network connectivity issues may occur with NuGet, test locally first:

```bash
# 1. Start the API
cd Inventory.Api
dotnet run --environment Development --urls "http://localhost:5000"

# 2. In another terminal, test the agent
cd Inventory.Agent.Windows
ApiSettings__BaseUrl=http://localhost:5000 dotnet run
```

### 2. API Testing with Swagger

1. **Start the API** (as shown above)
2. **Open Swagger UI**: http://localhost:5000/swagger
3. **Test Device Endpoints**:
   - GET `/api/device` - List devices
   - POST `/api/device` - Add a device

### 3. Manual Device Test

Test adding a device via curl:

```bash
curl -X POST "http://localhost:5000/api/device" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "TEST-PC-001",
    "macAddress": "00:1B:44:11:3A:B7",
    "ipAddress": "192.168.1.100",
    "deviceType": 0,
    "model": "Test Model",
    "location": "Office-101",
    "status": 0,
    "hardwareInfo": {
      "cpu": "Test CPU",
      "cpuCores": 4,
      "cpuLogical": 8,
      "cpuClockMHz": 2400,
      "motherboard": "Test Motherboard",
      "motherboardSerial": "TEST123",
      "biosManufacturer": "Test BIOS",
      "biosVersion": "1.0",
      "biosSerial": "BIOS123",
      "ramGB": 16,
      "diskGB": 500
    },
    "softwareInfo": {
      "operatingSystem": "Linux",
      "osVersion": "Ubuntu 20.04",
      "osArchitecture": "x64",
      "registeredUser": "testuser",
      "serialNumber": "SN123456",
      "installedApps": "[]",
      "updates": "[]",
      "users": "[]",
      "activeUser": "testuser"
    }
  }'
```

### 4. Verify Data

```bash
# List devices
curl http://localhost:5000/api/device

# Check database (if using SQLite)
sqlite3 ./Data/SQLite/inventory.db "SELECT Name, Model, IpAddress FROM Devices;"
```

## 🐳 Docker Testing (When Network Issues Are Resolved)

```bash
# Clean start
docker compose -f docker-compose.simple.yml down
docker compose -f docker-compose.simple.yml up --build -d

# Check status
docker compose -f docker-compose.simple.yml ps

# View logs
docker compose -f docker-compose.simple.yml logs -f

# Test API
curl http://localhost:5000/api/device
```

## 🔧 Troubleshooting

### Agent Issues Fixed:
✅ **Connection Refused**: Agent now connects to correct port (5000)
✅ **Console Input Error**: Fixed for Docker environments
✅ **Configuration**: API URL now configurable via environment variables

### Validation Errors:
If you see validation errors like "Model field is required", this is expected in Linux environments where some hardware info isn't accessible without root privileges. The core connectivity is working.

### Docker Network Issues:
If Docker build fails with NuGet connectivity issues, use the local development approach shown above.

## 📊 Testing Swagger UI

1. Navigate to: http://localhost:5000/swagger
2. Expand the `/api/device` endpoints
3. Test with the sample JSON above
4. Verify responses and status codes

## 🎉 Success Indicators

- ✅ Agent connects to API without "Connection refused"
- ✅ Agent completes without console input errors
- ✅ API responds to HTTP requests on port 5000
- ✅ Swagger UI is accessible and functional
- ✅ Device data can be posted and retrieved