# Platform Support and Change Logging

## New Features

### Separate Change Logging

The system now creates separate files for device changes when they are detected:

- **Main log files**: `LocalLogs/device-log-{date}.json` - Contains full device snapshots
- **Change files**: `LocalLogs/Changes/device-changes-{date}-{time}.json` - Contains only the detected changes

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

### Cross-Platform Support

The agent now supports both Windows and Linux platforms:

#### Windows Support
- Uses WMI (Windows Management Instrumentation) for comprehensive system information
- Supports GPU monitoring with LibreHardwareMonitor
- Detailed hardware and software enumeration

#### Linux Support
- Uses /proc filesystem and system commands for system information
- Cross-platform disk and memory information gathering
- Basic package and user enumeration

#### Platform Detection
The system automatically detects the platform and uses appropriate methods:

```csharp
if (CrossPlatformSystemInfo.IsWindows)
{
    // Windows-specific implementation
}
else if (CrossPlatformSystemInfo.IsLinux)
{
    // Linux-specific implementation
}
```

## Usage

The agent works the same way on both platforms:

```bash
# Windows
cd Inventory.Agent.Windows
dotnet run

# Linux
cd Inventory.Agent.Windows
dotnet run
```

## Log File Locations

- **Windows**: `{ExecutableDirectory}\LocalLogs\`
- **Linux**: `{ExecutableDirectory}/LocalLogs/`

Change files are stored in a `Changes` subdirectory within the logs folder.

## System Requirements

### Windows
- .NET 8.0 Runtime
- Windows 7 or later
- Administrator privileges recommended for comprehensive system information

### Linux
- .NET 8.0 Runtime
- Any modern Linux distribution
- Access to /proc filesystem
- Standard user privileges sufficient for basic information