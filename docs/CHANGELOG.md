# Inventory Management System - Changelog

## Recent Updates and Fixes

### ✅ Weekend Logging Issue Fixed
**Problem**: Logs created on Friday were being deleted when the system ran on Monday.
**Solution**: 
- Switched from daily logging to hourly logging
- Implemented 48-hour sliding window for proper retention
- File format: `device-log-2024-01-15-14.json` (includes hour)

### ✅ Hourly Logging Added
- Log creation every hour
- 48-hour (2-day) retention period
- Automatic cleanup of old files

### ✅ Configuration Updated
```json
{
  "Agent": {
    "LoggingInterval": "01:00:00",
    "LogRetentionHours": 48,
    "EnableHourlyLogging": true
  }
}
```

## New Documentation

### 📚 Installation Guide (`installation-guide.md`)
- System requirements
- Step-by-step installation (Windows/Linux)
- Agent deployment methods
- Troubleshooting guide

### 🚀 Server Setup (`server-deployment-testing.md`)
- Quick start (1-minute test)
- Detailed server setup
- API test scenarios
- Real data testing

### 🗄️ Database Setup (`database/setup-database.sql`)
- Complete SQL Server schema
- Sample data for testing
- Indexes and optimizations
- Automatic cleanup

## Quick Start

### 1. Test Environment (with SQLite)
```bash
git clone https://github.com/hizir-ceylan/InventoryManagementSystem.git
cd InventoryManagementSystem/Inventory.Api
dotnet run --urls="http://0.0.0.0:5000"
```
Swagger UI: `http://localhost:5000/swagger`

### 2. Database Setup
```sql
-- Run in SQL Server
sqlcmd -S localhost -U SA -P 'StrongPassword123!' -i database/setup-database.sql
```

### 3. API Testing
```bash
# Device list
curl http://localhost:5000/api/device

# Add test device
curl -X POST http://localhost:5000/api/device \
  -H "Content-Type: application/json" \
  -d '{"name":"Test-PC","macAddress":"00:1B:44:11:3A:B7","ipAddress":"192.168.1.100","deviceType":"PC"}'
```

### 4. Agent Installation
```bash
cd Inventory.Agent.Windows
dotnet build --configuration Release
# Copy output to C:\InventoryAgent
# Install as Windows Service
sc create "InventoryAgent" binPath="C:\InventoryAgent\Inventory.Agent.Windows.exe"
```

## File Structure

```
InventoryManagementSystem/
├── Inventory.Api/              # Web API
├── Inventory.Agent.Windows/    # Windows Agent
├── Inventory.Domain/           # Entity models
├── Inventory.Data/            # Data access layer
├── Inventory.Shared/          # Shared libraries
├── docs/
│   ├── installation-guide.md     # Installation guide
│   ├── server-deployment-testing.md  # Server setup
│   └── technical-documentation.md    # Technical documentation
├── database/
│   └── setup-database.sql        # Database setup script
└── build-tools/                      # Utility scripts
    ├── quick-start.sh            # Quick start script
    ├── test-docker.sh            # Docker testing
    └── test-logging.sh           # Logging tests
```

## Connection String Examples

### SQL Server
```
Server=localhost;Database=InventoryDB;User Id=inventoryuser;Password=StrongPassword123!;TrustServerCertificate=true;
```

### SQLite (Test)
```
Data Source=inventory.db
```

### PostgreSQL
```
Server=localhost;Database=inventorydb;User Id=inventoryuser;Password=StrongPassword123!;
```

## Important Notes

### Logging Changes
- **Old format**: `device-log-2024-01-15.json`
- **New format**: `device-log-2024-01-15-14.json`
- **Retention**: 48 hours (includes weekends)
- **Cleanup**: Automatic, hourly operation

### Agent Configuration
```json
{
  "ApiSettings": {
    "BaseUrl": "https://your-api-server.com",
    "Timeout": 30
  },
  "Agent": {
    "ScanInterval": "01:00:00",
    "LogRetentionHours": 48
  }
}
```

### API Endpoints
- `GET /api/device` - Device list
- `POST /api/device` - New device
- `GET /swagger` - API documentation
- `POST /api/logging` - Log submission

## Testing and Validation

### Logging Tests
```bash
./build-tools/test-logging.sh
```

### API Tests
```bash
# Automated test script available (see server-deployment-testing.md)
./build-tools/automated_test.sh http://your-server-ip
```

### Database Tests
```sql
SELECT COUNT(*) FROM Devices;
SELECT COUNT(*) FROM ChangeLogs;
```

## Next Steps

1. **Production Deployment**:
   - IIS/Nginx configuration
   - SSL certificate setup
   - Firewall rules

2. **Monitoring**:
   - Log analysis
   - Performance monitoring
   - Health checks

3. **Backup**:
   - Database backup
   - Configuration backup
   - Automated backup scripts

## Support and Troubleshooting

- **Installation issues**: `installation-guide.md` - Troubleshooting section
- **API issues**: `server-deployment-testing.md` - Test scenarios
- **Agent issues**: Check Event Viewer "InventoryAgent" logs
- **Database issues**: Check connection string and SQL Server services

## Summary

✅ **Weekend logging issue fixed** - Friday logs no longer deleted on Monday
✅ **Hourly logging added** - Hourly logs, 48-hour retention
✅ **Comprehensive documentation** - Installation, deployment, test guides
✅ **Database setup script** - One-click complete setup
✅ **Testing tools** - Automated test and validation scripts

The system is now ready for production use! 🚀