# Remote Server and Offline Batch Configuration Guide

This document explains how to configure the Inventory Management System for remote server connections and offline batch processing.

## Configuration Options

### API Server Configuration

The API server supports both local SQLite and remote database connections. Configure this in `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=inventory.db"
  },
  "ServerSettings": {
    "Mode": "Local",
    "RemoteServerUrl": "",
    "RemoteDatabaseConnectionString": ""
  }
}
```

#### Remote SQL Server Configuration

To use a remote SQL Server database:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=your-server.com;Database=InventoryDB;User Id=username;Password=password;TrustServerCertificate=true;"
  },
  "ServerSettings": {
    "Mode": "Remote",
    "RemoteServerUrl": "https://your-api-server.com",
    "RemoteDatabaseConnectionString": "Server=your-server.com;Database=InventoryDB;User Id=username;Password=password;TrustServerCertificate=true;"
  }
}
```

### Agent Configuration

The Agent supports configuration via environment variables:

#### Basic Configuration
```bash
export ApiSettings__BaseUrl="https://your-api-server.com"
export ApiSettings__Timeout="30"
export ApiSettings__RetryCount="3"
```

#### Offline Storage Configuration
```bash
export ApiSettings__EnableOfflineStorage="true"
export ApiSettings__OfflineStoragePath="Data/OfflineStorage"
export ApiSettings__BatchUploadInterval="300"  # 5 minutes
export ApiSettings__MaxOfflineRecords="10000"
```

## Features

### 1. Remote Server Support

The system now supports connecting to remote servers instead of just localhost:

- **Configurable API URL**: Agents can connect to any API server URL
- **Remote Database**: API can connect to remote SQL Server databases
- **Environment Variables**: Easy configuration without code changes

### 2. Offline Data Storage

When the API server is unavailable, the Agent automatically stores data offline:

- **Local SQLite Storage**: Device data is stored in JSON format locally
- **Automatic Detection**: Connection failures trigger offline storage
- **Data Persistence**: Offline data survives application restarts

### 3. Batch Upload on Connection Restore

When connectivity is restored, offline data is automatically uploaded:

- **Connectivity Monitoring**: Periodic checks every 5 minutes (configurable)
- **Batch Processing**: Multiple devices uploaded in batches of 50
- **Automatic Cleanup**: Successfully uploaded records are removed
- **Retry Logic**: Failed uploads are retried with exponential backoff
- **Old Data Cleanup**: Records older than 7 days or with 10+ attempts are removed

### 4. New API Endpoints

#### Batch Upload Endpoint
```
POST /api/device/batch
Content-Type: application/json

[
  {
    "name": "DEVICE-001",
    "macAddress": "00:11:22:33:44:55",
    "ipAddress": "192.168.1.100",
    "deviceType": 1,
    "model": "Dell OptiPlex",
    "location": "Office-101"
  }
]
```

Response:
```json
{
  "totalDevices": 1,
  "successfulUploads": 1,
  "failedUploads": 0,
  "errors": []
}
```

## Usage Examples

### 1. Running Agent with Remote Server

```bash
# Set environment variables
export ApiSettings__BaseUrl="https://inventory-api.company.com"
export ApiSettings__EnableOfflineStorage="true"

# Run agent
dotnet run --project Inventory.Agent.Windows
```

### 2. Docker Configuration

```yaml
# docker-compose.yml
version: '3.8'
services:
  inventory-api:
    build: .
    environment:
      - ConnectionStrings__DefaultConnection=Server=sql-server;Database=InventoryDB;User=sa;Password=YourPassword123!
      - ServerSettings__Mode=Remote
    ports:
      - "5000:80"
    depends_on:
      - sql-server
      
  sql-server:
    image: mcr.microsoft.com/mssql/server:2022-latest
    environment:
      - ACCEPT_EULA=Y
      - SA_PASSWORD=YourPassword123!
    ports:
      - "1433:1433"
```

### 3. Testing Offline Functionality

1. Start the API server
2. Run the Agent to verify normal operation
3. Stop the API server
4. Run the Agent again - data should be stored offline
5. Start the API server
6. Run the Agent - offline data should be uploaded automatically

## Monitoring

The system provides extensive logging for monitoring:

- **Agent Logs**: Connection status, offline storage, batch uploads
- **API Logs**: Request/response logging, batch processing results
- **File Locations**: 
  - Offline data: `Data/OfflineStorage/offline_devices.json`
  - Agent logs: `Data/AgentLogs/`
  - API logs: `Data/ApiLogs/`

## Troubleshooting

### Common Issues

1. **Connection Refused**: Verify API URL and network connectivity
2. **Authentication Errors**: Check database credentials
3. **Offline Storage Full**: Check `MaxOfflineRecords` setting
4. **Batch Upload Failures**: Check API server logs for validation errors

### Configuration Validation

Test your configuration:

```bash
# Test API connectivity
curl -X GET "https://your-api-server.com/api/device"

# Test batch upload
curl -X POST "https://your-api-server.com/api/device/batch" \
  -H "Content-Type: application/json" \
  -d '[{"name":"TEST","deviceType":1}]'
```