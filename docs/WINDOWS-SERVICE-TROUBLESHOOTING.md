# Inventory Management System - Windows Service Troubleshooting

## Service Installation and Startup Issues

### Error 1053: Service did not respond in timely manner

This error typically occurs when:
1. The service takes too long to start (>30 seconds)
2. An exception occurs during service startup
3. The service doesn't properly communicate with Windows Service Control Manager

### Troubleshooting Steps

#### 1. Check Event Logs
- Open Event Viewer (eventvwr.msc)
- Navigate to Windows Logs > Application
- Look for entries from "InventoryManagementAgent" source
- Check for error details and stack traces

#### 2. Check Service-Specific Logs
Service logs are written to:
```
C:\ProgramData\Inventory Management System\Logs\
```

Look for:
- `service-{date}.log` - General service operations
- `service-errors.log` - Critical startup errors

#### 3. Manual Service Management
Use the Service Management tool from Start Menu:
- Start Menu → Inventory Management System → Service Management
- Or run: `C:\Program Files\Inventory Management System\ServiceManagement.bat`

#### 4. Manual Testing
Test the agent in console mode first:
```cmd
cd "C:\Program Files\Inventory Management System\Agent"
Inventory.Agent.Windows.exe
```

#### 5. Service Dependencies
Ensure the API service starts first:
```cmd
net start InventoryManagementApi
timeout /t 10
net start InventoryManagementAgent
```

#### 6. Service Configuration
Check service configuration:
```cmd
sc query InventoryManagementAgent
sc qc InventoryManagementAgent
```

### Common Solutions

#### Solution 1: Restart Services
```cmd
net stop InventoryManagementAgent
net stop InventoryManagementApi
timeout /t 5
net start InventoryManagementApi
timeout /t 10
net start InventoryManagementAgent
```

#### Solution 2: Recreate Services
Run as Administrator:
```cmd
sc delete InventoryManagementAgent
sc delete InventoryManagementApi

sc create InventoryManagementApi binPath= "C:\Program Files\Inventory Management System\Api\Inventory.Api.exe" start= auto DisplayName= "Inventory Management API" obj= LocalSystem

sc create InventoryManagementAgent binPath= "C:\Program Files\Inventory Management System\Agent\Inventory.Agent.Windows.exe --service" start= auto DisplayName= "Inventory Management Agent" obj= LocalSystem depend= InventoryManagementApi

sc config InventoryManagementAgent start= delayed-auto
```

#### Solution 3: Check Firewall
Ensure port 5093 is open:
```cmd
netsh advfirewall firewall add rule name="Inventory Management API" dir=in action=allow protocol=TCP localport=5093
```

#### Solution 4: Check Environment Variables
Verify environment variables are set:
```cmd
echo %ApiSettings__BaseUrl%
echo %ApiSettings__EnableOfflineStorage%
echo %ApiSettings__OfflineStoragePath%
```

If not set, run as Administrator:
```cmd
setx ApiSettings__BaseUrl "http://localhost:5093" /M
setx ApiSettings__EnableOfflineStorage "true" /M
setx ApiSettings__OfflineStoragePath "C:\Program Files\Inventory Management System\Data\OfflineStorage" /M
```

### Service Improvements Made

1. **Faster Startup**: Service now signals readiness to SCM within seconds
2. **Better Error Handling**: Comprehensive exception handling during startup
3. **Background Operations**: Heavy operations moved to background tasks
4. **Enhanced Logging**: Multiple logging targets (Event Log, File Log)
5. **Service Recovery**: Automatic restart on failure
6. **Delayed Start**: Agent waits for API to be ready

### Monitoring

#### Service Status
```cmd
sc query InventoryManagementApi
sc query InventoryManagementAgent
```

#### Real-time Logs
Monitor service logs in real-time by opening the log folder:
```cmd
explorer "C:\ProgramData\Inventory Management System\Logs"
```

#### API Health Check
Test API availability:
```cmd
curl http://localhost:5093/api/device
```

### Contact Support

If issues persist:
1. Collect logs from Event Viewer and service log files
2. Include service configuration details
3. Provide system information (Windows version, .NET version)
4. Include any error messages or codes

## Performance Notes

- Service startup time reduced from 30+ seconds to <5 seconds
- Background inventory scanning every 30 minutes
- Automatic API connectivity monitoring
- Offline storage for when API is unavailable
- Graceful service shutdown and cleanup