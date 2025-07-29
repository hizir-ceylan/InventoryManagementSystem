# Platform Desteği ve Değişiklik Takibi

## Yeni Özellikler

### Ayrı Değişiklik Loglaması

Sistem artık cihaz değişiklikleri tespit edildiğinde ayrı dosyalar oluşturmaktadır:

- **Ana log dosyaları**: `LocalLogs/device-log-{tarih}.json` - Tam cihaz bilgilerini içerir
- **Değişiklik dosyaları**: `LocalLogs/Changes/device-changes-{tarih}-{saat}.json` - Sadece tespit edilen değişiklikleri içerir

#### Değişiklik Dosyası Formatı

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

### Çoklu Platform Desteği

Agent artık hem Windows hem de Linux platformlarını desteklemektedir:

#### Windows Desteği
- Kapsamlı sistem bilgileri için WMI (Windows Management Instrumentation) kullanır
- LibreHardwareMonitor ile GPU izleme desteği
- Detaylı donanım ve yazılım enumerasyonu

#### Linux Desteği
- Kapsamlı sistem bilgileri için /proc dosya sistemi ve sistem komutları kullanır
- `ip addr show` ve `/sys/class/net` kullanarak gerçek ağ arayüzü tespiti
- `dmidecode` ve `/sys/class/dmi/id/` kullanarak sistem bilgisi toplama
- Detaylı RAM modülleri için `/proc/meminfo` ve `dmidecode`'dan bellek bilgileri
- dpkg, rpm, pacman, yum, snap ve flatpak paket yöneticisi desteği
- NVIDIA kartları için `lspci` ve `nvidia-smi` kullanarak GPU tespiti
- Gerçek CPU, disk ve donanım bilgisi toplama
- Cross-platform disk and memory information gathering

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