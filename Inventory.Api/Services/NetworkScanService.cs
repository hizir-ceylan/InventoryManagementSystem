using Inventory.Api.Controllers;
using Inventory.Api.Helpers;
using Inventory.Domain.Entities;
using Inventory.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace Inventory.Api.Services
{
    public interface INetworkScanService
    {
        Task TriggerManualScanAsync();
        Task TriggerManualScanAsync(string networkRange);
        Task TriggerScanAllNetworksAsync();
        object GetScanStatus();
        object GetScanHistory();
        void SetSchedule(NetworkScanScheduleDto schedule);
        object GetSchedule();
        List<string> GetLocalNetworkRanges();
    }

    public class NetworkScanService : INetworkScanService
    {
        private readonly ILogger<NetworkScanService> _logger;
        private readonly IDeviceService _deviceService;
        private readonly INetworkScannerService _networkScannerService;
        private readonly ICentralizedLoggingService _loggingService;
        private bool _isScanning = false;
        private DateTime? _lastScanTime;
        private readonly List<NetworkScanHistoryItem> _scanHistory = new();
        private NetworkScanScheduleDto _schedule = new() { Enabled = false, Interval = TimeSpan.FromHours(1) };

        public NetworkScanService(
            ILogger<NetworkScanService> logger, 
            IDeviceService deviceService,
            INetworkScannerService networkScannerService,
            ICentralizedLoggingService loggingService)
        {
            _logger = logger;
            _deviceService = deviceService;
            _networkScannerService = networkScannerService;
            _loggingService = loggingService;
        }

        public async Task TriggerManualScanAsync()
        {
            if (_isScanning)
            {
                throw new InvalidOperationException("Tarama zaten devam ediyor.");
            }

            await TriggerScanAsync("Manual", null);
        }

        public async Task TriggerManualScanAsync(string networkRange)
        {
            if (_isScanning)
            {
                throw new InvalidOperationException("Tarama zaten devam ediyor.");
            }

            await TriggerScanAsync("Manual", networkRange);
        }

        public async Task TriggerScanAllNetworksAsync()
        {
            if (_isScanning)
            {
                throw new InvalidOperationException("Tarama zaten devam ediyor.");
            }

            await TriggerScanAsync("Manual-AllNetworks", null, true);
        }

        private async Task TriggerScanAsync(string scanType, string? networkRange, bool scanAllNetworks = false)
        {
            _isScanning = true;
            _lastScanTime = DateTime.UtcNow;
            int devicesFound = 0;
            string? error = null;

            try
            {
                var scanTarget = networkRange ?? (scanAllNetworks ? "All Local Networks" : "Auto-detected Network");
                _logger.LogInformation("Starting {ScanType} network scan for {ScanTarget}...", scanType, scanTarget);
                await _loggingService.LogInfoAsync("NetworkScan", $"Starting {scanType} network scan for {scanTarget}");
                
                List<NetworkDeviceResult> devices;
                
                if (scanAllNetworks)
                {
                    devices = await _networkScannerService.ScanAllLocalNetworksAsync();
                }
                else
                {
                    devices = await _networkScannerService.ScanNetworkAsync(networkRange);
                }

                devicesFound = devices.Count;

                // Keşfedilen cihazları kaydet
                if (devices.Any())
                {
                    await _deviceService.RegisterNetworkDevicesAsync(devices);
                    _logger.LogInformation("Registered {DeviceCount} network devices", devicesFound);
                    await _loggingService.LogInfoAsync("NetworkScan", $"Registered {devicesFound} network devices");
                }

                _logger.LogInformation("{ScanType} network scan completed successfully. Found {DevicesFound} devices", scanType, devicesFound);
            }
            catch (Exception ex)
            {
                error = ex.Message;
                _logger.LogError(ex, "Error during {ScanType} network scan", scanType);
                await _loggingService.LogErrorAsync("NetworkScan", $"Error during {scanType} network scan", ex);
                throw;
            }
            finally
            {
                _scanHistory.Add(new NetworkScanHistoryItem
                {
                    ScanTime = _lastScanTime.Value,
                    ScanType = scanType,
                    Status = error == null ? "Tamamlandı" : "Başarısız",
                    DevicesFound = devicesFound,
                    Error = error,
                    NetworkRange = networkRange ?? (scanAllNetworks ? "Tüm Ağlar" : NetworkRangeDetector.GetPrimaryNetworkRange())
                });
                
                _isScanning = false;
            }
        }

        public object GetScanStatus()
        {
            return new
            {
                IsScanning = _isScanning,
                LastScanTime = _lastScanTime,
                Schedule = _schedule,
                LocalNetworkRanges = GetLocalNetworkRanges(),
                CurrentIpAddress = NetworkRangeDetector.GetLocalIPAddress()
            };
        }

        public object GetScanHistory()
        {
            return _scanHistory.OrderByDescending(h => h.ScanTime).Take(20).ToList();
        }

        public void SetSchedule(NetworkScanScheduleDto schedule)
        {
            _schedule = schedule;
            _logger.LogInformation("Network scan schedule updated: Enabled={Enabled}, Interval={Interval}, NetworkRange={NetworkRange}",
                schedule.Enabled, schedule.Interval, schedule.NetworkRange);
            
            // Ağ aralığı belirtilmemişse, otomatik algılanan aralığı kullan
            if (string.IsNullOrEmpty(_schedule.NetworkRange))
            {
                _schedule.NetworkRange = NetworkRangeDetector.GetPrimaryNetworkRange();
                _logger.LogInformation("Auto-detected network range: {NetworkRange}", _schedule.NetworkRange);
            }
        }

        public object GetSchedule()
        {
            return _schedule;
        }

        public List<string> GetLocalNetworkRanges()
        {
            return _networkScannerService.GetLocalNetworkRanges();
        }

        private async Task PerformNetworkScanAsync()
        {
            // This method is kept for backward compatibility but now uses the new scanner
            var devices = await _networkScannerService.ScanNetworkAsync(_schedule.NetworkRange);
            await _deviceService.RegisterNetworkDevicesAsync(devices);
        }
    }

    public class NetworkScanHistoryItem
    {
        public DateTime ScanTime { get; set; }
        public string ScanType { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int DevicesFound { get; set; }
        public string? Error { get; set; }
        public string? NetworkRange { get; set; }
    }
}

namespace Inventory.Api.Services
{
    public interface IDeviceService
    {
        Task RegisterNetworkDevicesAsync(IEnumerable<NetworkDeviceResult> devices);
        Task RegisterNetworkDevicesAsync(IEnumerable<object> devices); // Backward compatibility
        
        // Database operations
        Task<IEnumerable<Device>> GetAllDevicesAsync();
        Task<IEnumerable<Device>> GetAgentInstalledDevicesAsync();
        Task<IEnumerable<Device>> GetNetworkDiscoveredDevicesAsync();
        Task<Device?> GetDeviceByIdAsync(Guid id);
        Task<Device?> FindDeviceByIpOrMacAsync(string? ipAddress, string? macAddress);
        Task<Device?> FindDeviceByNameAndIpAsync(string? deviceName, string? ipAddress);
        Task<Device> CreateDeviceAsync(Device device);
        Task<Device> UpdateDeviceAsync(Device device);
        Task<bool> DeleteDeviceAsync(Guid id);
        Task<Device?> CreateOrUpdateDeviceAsync(Device device);
        Task<List<Device>> BatchCreateOrUpdateDevicesAsync(List<Device> devices);
    }

    public class DeviceService : IDeviceService
    {
        private readonly ILogger<DeviceService> _logger;
        private readonly ICentralizedLoggingService _loggingService;
        private readonly InventoryDbContext _context;

        public DeviceService(ILogger<DeviceService> logger, ICentralizedLoggingService loggingService, InventoryDbContext context)
        {
            _logger = logger;
            _loggingService = loggingService;
            _context = context;
        }

        public async Task RegisterNetworkDevicesAsync(IEnumerable<NetworkDeviceResult> devices)
        {
            try
            {
                var deviceList = devices.ToList();
                _logger.LogInformation("Registering {DeviceCount} network devices", deviceList.Count);

                foreach (var device in deviceList)
                {
                    // Here you would typically save to database
                    // For now, we'll just log the device information
                    await _loggingService.LogInfoAsync("DeviceRegistration", 
                        $"Registering device: {device.Name} ({device.IpAddress}, {device.MacAddress}) - {device.Manufacturer}");
                    
                    _logger.LogDebug("Registered device: {DeviceName} ({IpAddress}, {MacAddress}) - {Manufacturer}", 
                        device.Name, device.IpAddress, device.MacAddress, device.Manufacturer);
                }

                await _loggingService.LogInfoAsync("DeviceRegistration", $"Successfully registered {deviceList.Count} network devices");
                _logger.LogInformation("Successfully registered {DeviceCount} network devices", deviceList.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering network devices");
                await _loggingService.LogErrorAsync("DeviceRegistration", "Error registering network devices", ex);
                throw;
            }
        }

        public async Task RegisterNetworkDevicesAsync(IEnumerable<object> devices)
        {
            // Backward compatibility method
            await Task.CompletedTask;
            _logger.LogInformation("Registered {DeviceCount} network devices (legacy method)", devices.Count());
        }

        // Database operations
        public async Task<IEnumerable<Device>> GetAllDevicesAsync()
        {
            return await _context.Devices
                .ToListAsync();
        }

        public async Task<IEnumerable<Device>> GetAgentInstalledDevicesAsync()
        {
            return await _context.Devices
                .Where(d => d.AgentInstalled || d.ManagementType == ManagementType.Agent)
                .ToListAsync();
        }

        public async Task<IEnumerable<Device>> GetNetworkDiscoveredDevicesAsync()
        {
            return await _context.Devices
                .Where(d => !d.AgentInstalled && 
                    (d.ManagementType == ManagementType.NetworkDiscovery || d.DiscoveryMethod == DiscoveryMethod.NetworkDiscovery))
                .ToListAsync();
        }

        public async Task<Device?> GetDeviceByIdAsync(Guid id)
        {
            return await _context.Devices
                .FirstOrDefaultAsync(d => d.Id == id);
        }

        public async Task<Device?> FindDeviceByIpOrMacAsync(string? ipAddress, string? macAddress)
        {
            // First try to find by IP address (prioritize this for devices with multiple network interfaces)
            if (!string.IsNullOrWhiteSpace(ipAddress))
            {
                var deviceByIp = await _context.Devices
                    .FirstOrDefaultAsync(d => d.IpAddress == ipAddress);
                if (deviceByIp != null)
                    return deviceByIp;
            }
            
            // If not found by IP, try by MAC address
            if (!string.IsNullOrWhiteSpace(macAddress))
            {
                var deviceByMac = await _context.Devices
                    .FirstOrDefaultAsync(d => d.MacAddress == macAddress);
                if (deviceByMac != null)
                    return deviceByMac;
            }
            
            return null;
        }

        public async Task<Device?> FindDeviceByNameAndIpAsync(string? deviceName, string? ipAddress)
        {
            if (string.IsNullOrWhiteSpace(deviceName) || string.IsNullOrWhiteSpace(ipAddress))
                return null;
                
            return await _context.Devices
                .FirstOrDefaultAsync(d => 
                    d.Name.Equals(deviceName, StringComparison.OrdinalIgnoreCase) && 
                    d.IpAddress == ipAddress);
        }

        public async Task<Device> CreateDeviceAsync(Device device)
        {
            // Koleksiyonları null ise başlat
            device.ChangeLogs ??= new List<ChangeLog>();
            
            // Gerekli varsayılan değerleri sağla
            if (device.ManagementType == ManagementType.Unknown)
                device.ManagementType = ManagementType.Manual;
            if (device.DiscoveryMethod == DiscoveryMethod.Unknown)
                device.DiscoveryMethod = DiscoveryMethod.Manual;
            
            device.LastSeen = DateTime.UtcNow;
            
            try
            {
                // Owned entity'lerin ID'lerini SQLite için hazırla
                PrepareOwnedEntitiesForSqlite(device);
                
                // Entity'yi context'e ekle
                _context.Devices.Add(device);
                
                // Değişiklikleri kaydet
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Device created: {DeviceName} ({DeviceId})", device.Name, device.Id);
                return device;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating device: {DeviceName}. Error: {Error}", device.Name, ex.Message);
                
                // Eğer constraint hatası ise, owned entity'ler için detach ve yeniden ekleme yaklaşımını dene
                if (ex.Message.Contains("NOT NULL constraint failed") && ex.Message.Contains("Id"))
                {
                    _logger.LogWarning("Attempting to recreate device with fresh entity state");
                    
                    // Context'i temizle
                    _context.ChangeTracker.Clear();
                    
                    // Owned entity'leri yeniden oluştur
                    RecreateOwnedEntitiesForSqlite(device);
                    
                    // Yeniden ekle ve kaydet
                    _context.Devices.Add(device);
                    await _context.SaveChangesAsync();
                    
                    _logger.LogInformation("Device created after retry: {DeviceName} ({DeviceId})", device.Name, device.Id);
                    return device;
                }
                
                throw;
            }
        }

        private void PrepareOwnedEntitiesForSqlite(Device device)
        {
            if (device.HardwareInfo != null)
            {
                // For SQLite auto-increment, ensure all owned entity IDs are 0
                if (device.HardwareInfo.Disks != null)
                {
                    foreach (var disk in device.HardwareInfo.Disks)
                    {
                        disk.Id = 0;
                    }
                }

                if (device.HardwareInfo.RamModules != null)
                {
                    foreach (var ram in device.HardwareInfo.RamModules)
                    {
                        ram.Id = 0;
                    }
                }

                if (device.HardwareInfo.Gpus != null)
                {
                    foreach (var gpu in device.HardwareInfo.Gpus)
                    {
                        gpu.Id = 0;
                    }
                }

                if (device.HardwareInfo.NetworkAdapters != null)
                {
                    foreach (var adapter in device.HardwareInfo.NetworkAdapters)
                    {
                        adapter.Id = 0;
                    }
                }
            }
        }

        private void RecreateOwnedEntitiesForSqlite(Device device)
        {
            if (device.HardwareInfo != null)
            {
                // More aggressive approach: recreate collections to ensure clean state
                if (device.HardwareInfo.Disks != null)
                {
                    var diskData = device.HardwareInfo.Disks.Select(d => new DiskInfo
                    {
                        Id = 0, // Explicitly set to 0 for new entities
                        DeviceId = d.DeviceId,
                        TotalGB = d.TotalGB,
                        FreeGB = d.FreeGB
                    }).ToList();
                    device.HardwareInfo.Disks = diskData;
                }

                if (device.HardwareInfo.RamModules != null)
                {
                    var ramData = device.HardwareInfo.RamModules.Select(r => new RamModule
                    {
                        Id = 0,
                        Slot = r.Slot,
                        CapacityGB = r.CapacityGB,
                        SpeedMHz = r.SpeedMHz,
                        Manufacturer = r.Manufacturer,
                        PartNumber = r.PartNumber,
                        SerialNumber = r.SerialNumber
                    }).ToList();
                    device.HardwareInfo.RamModules = ramData;
                }

                if (device.HardwareInfo.Gpus != null)
                {
                    var gpuData = device.HardwareInfo.Gpus.Select(g => new GpuInfo
                    {
                        Id = 0,
                        Name = g.Name,
                        MemoryGB = g.MemoryGB
                    }).ToList();
                    device.HardwareInfo.Gpus = gpuData;
                }

                if (device.HardwareInfo.NetworkAdapters != null)
                {
                    var adapterData = device.HardwareInfo.NetworkAdapters.Select(a => new NetworkAdapter
                    {
                        Id = 0,
                        Description = a.Description,
                        MacAddress = a.MacAddress,
                        IpAddress = a.IpAddress
                    }).ToList();
                    device.HardwareInfo.NetworkAdapters = adapterData;
                }
            }
        }

        public async Task<Device> UpdateDeviceAsync(Device device)
        {
            device.LastSeen = DateTime.UtcNow;
            
            // Clear any existing tracking to avoid conflicts
            _context.ChangeTracker.Clear();
            
            // Attach the device and mark it as modified
            var entry = _context.Entry(device);
            entry.State = Microsoft.EntityFrameworkCore.EntityState.Modified;
            
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Device updated: {DeviceName} ({DeviceId})", device.Name, device.Id);
            return device;
        }

        public async Task<bool> DeleteDeviceAsync(Guid id)
        {
            var device = await _context.Devices.FindAsync(id);
            if (device == null)
                return false;

            _context.Devices.Remove(device);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Device deleted: {DeviceName} ({DeviceId})", device.Name, device.Id);
            return true;
        }

        public async Task<Device?> CreateOrUpdateDeviceAsync(Device device)
        {
            Device? existingDevice = null;
            
            // Try to find existing device using multiple strategies to avoid duplicates
            // 1. First try by device name and IP address (best for multi-interface devices)
            if (!string.IsNullOrWhiteSpace(device.Name) && !string.IsNullOrWhiteSpace(device.IpAddress))
            {
                existingDevice = await FindDeviceByNameAndIpAsync(device.Name, device.IpAddress);
            }
            
            // 2. If not found, try by IP or MAC
            if (existingDevice == null)
            {
                existingDevice = await FindDeviceByIpOrMacAsync(device.IpAddress, device.MacAddress);
            }
            
            if (existingDevice != null)
            {
                var changes = new List<ChangeLog>();
                
                // Track changes and create change logs
                if (!string.IsNullOrEmpty(device.Name) && existingDevice.Name != device.Name)
                {
                    changes.Add(new ChangeLog
                    {
                        Id = Guid.NewGuid(),
                        DeviceId = existingDevice.Id,
                        ChangeDate = DateTime.UtcNow,
                        ChangeType = "Name",
                        OldValue = existingDevice.Name ?? "",
                        NewValue = device.Name,
                        ChangedBy = "Agent"
                    });
                    existingDevice.Name = device.Name;
                }

                if (!string.IsNullOrEmpty(device.IpAddress) && existingDevice.IpAddress != device.IpAddress)
                {
                    changes.Add(new ChangeLog
                    {
                        Id = Guid.NewGuid(),
                        DeviceId = existingDevice.Id,
                        ChangeDate = DateTime.UtcNow,
                        ChangeType = "IpAddress",
                        OldValue = existingDevice.IpAddress ?? "",
                        NewValue = device.IpAddress,
                        ChangedBy = "Agent"
                    });
                    existingDevice.IpAddress = device.IpAddress;
                }

                if (!string.IsNullOrEmpty(device.Model) && existingDevice.Model != device.Model)
                {
                    changes.Add(new ChangeLog
                    {
                        Id = Guid.NewGuid(),
                        DeviceId = existingDevice.Id,
                        ChangeDate = DateTime.UtcNow,
                        ChangeType = "Model",
                        OldValue = existingDevice.Model ?? "",
                        NewValue = device.Model,
                        ChangedBy = "Agent"
                    });
                    existingDevice.Model = device.Model;
                }

                if (!string.IsNullOrEmpty(device.Location) && existingDevice.Location != device.Location)
                {
                    changes.Add(new ChangeLog
                    {
                        Id = Guid.NewGuid(),
                        DeviceId = existingDevice.Id,
                        ChangeDate = DateTime.UtcNow,
                        ChangeType = "Location",
                        OldValue = existingDevice.Location ?? "",
                        NewValue = device.Location,
                        ChangedBy = "Agent"
                    });
                    existingDevice.Location = device.Location;
                }

                if (device.DeviceType != DeviceType.Unknown && existingDevice.DeviceType != device.DeviceType)
                {
                    changes.Add(new ChangeLog
                    {
                        Id = Guid.NewGuid(),
                        DeviceId = existingDevice.Id,
                        ChangeDate = DateTime.UtcNow,
                        ChangeType = "DeviceType",
                        OldValue = existingDevice.DeviceType.ToString(),
                        NewValue = device.DeviceType.ToString(),
                        ChangedBy = "Agent"
                    });
                    existingDevice.DeviceType = device.DeviceType;
                }

                if (device.ManagementType != ManagementType.Unknown && existingDevice.ManagementType != device.ManagementType)
                {
                    changes.Add(new ChangeLog
                    {
                        Id = Guid.NewGuid(),
                        DeviceId = existingDevice.Id,
                        ChangeDate = DateTime.UtcNow,
                        ChangeType = "ManagementType",
                        OldValue = existingDevice.ManagementType.ToString(),
                        NewValue = device.ManagementType.ToString(),
                        ChangedBy = "Agent"
                    });
                    existingDevice.ManagementType = device.ManagementType;
                }

                if (existingDevice.Status != device.Status)
                {
                    changes.Add(new ChangeLog
                    {
                        Id = Guid.NewGuid(),
                        DeviceId = existingDevice.Id,
                        ChangeDate = DateTime.UtcNow,
                        ChangeType = "Status",
                        OldValue = existingDevice.Status.ToString(),
                        NewValue = device.Status.ToString(),
                        ChangedBy = "Agent"
                    });
                    existingDevice.Status = device.Status;
                }

                // Update hardware info if available
                if (device.HardwareInfo != null)
                {
                    if (existingDevice.HardwareInfo == null)
                    {
                        changes.Add(new ChangeLog
                        {
                            Id = Guid.NewGuid(),
                            DeviceId = existingDevice.Id,
                            ChangeDate = DateTime.UtcNow,
                            ChangeType = "HardwareInfo",
                            OldValue = "None",
                            NewValue = "Hardware information added",
                            ChangedBy = "Agent"
                        });
                        existingDevice.HardwareInfo = device.HardwareInfo;
                    }
                    else
                    {
                        // Track significant hardware changes
                        if (!string.IsNullOrEmpty(device.HardwareInfo.Cpu) && 
                            existingDevice.HardwareInfo.Cpu != device.HardwareInfo.Cpu)
                        {
                            changes.Add(new ChangeLog
                            {
                                Id = Guid.NewGuid(),
                                DeviceId = existingDevice.Id,
                                ChangeDate = DateTime.UtcNow,
                                ChangeType = "CPU",
                                OldValue = existingDevice.HardwareInfo.Cpu ?? "",
                                NewValue = device.HardwareInfo.Cpu,
                                ChangedBy = "Agent"
                            });
                        }

                        if (device.HardwareInfo.RamGB != existingDevice.HardwareInfo.RamGB)
                        {
                            changes.Add(new ChangeLog
                            {
                                Id = Guid.NewGuid(),
                                DeviceId = existingDevice.Id,
                                ChangeDate = DateTime.UtcNow,
                                ChangeType = "RAM",
                                OldValue = $"{existingDevice.HardwareInfo.RamGB} GB",
                                NewValue = $"{device.HardwareInfo.RamGB} GB",
                                ChangedBy = "Agent"
                            });
                        }

                        // Update hardware info fields
                        existingDevice.HardwareInfo.Cpu = device.HardwareInfo.Cpu ?? existingDevice.HardwareInfo.Cpu;
                        existingDevice.HardwareInfo.CpuCores = device.HardwareInfo.CpuCores > 0 ? device.HardwareInfo.CpuCores : existingDevice.HardwareInfo.CpuCores;
                        existingDevice.HardwareInfo.CpuLogical = device.HardwareInfo.CpuLogical > 0 ? device.HardwareInfo.CpuLogical : existingDevice.HardwareInfo.CpuLogical;
                        existingDevice.HardwareInfo.CpuClockMHz = device.HardwareInfo.CpuClockMHz > 0 ? device.HardwareInfo.CpuClockMHz : existingDevice.HardwareInfo.CpuClockMHz;
                        existingDevice.HardwareInfo.Motherboard = device.HardwareInfo.Motherboard ?? existingDevice.HardwareInfo.Motherboard;
                        existingDevice.HardwareInfo.MotherboardSerial = device.HardwareInfo.MotherboardSerial ?? existingDevice.HardwareInfo.MotherboardSerial;
                        existingDevice.HardwareInfo.BiosManufacturer = device.HardwareInfo.BiosManufacturer ?? existingDevice.HardwareInfo.BiosManufacturer;
                        existingDevice.HardwareInfo.BiosVersion = device.HardwareInfo.BiosVersion ?? existingDevice.HardwareInfo.BiosVersion;
                        existingDevice.HardwareInfo.BiosSerial = device.HardwareInfo.BiosSerial ?? existingDevice.HardwareInfo.BiosSerial;
                        existingDevice.HardwareInfo.RamGB = device.HardwareInfo.RamGB > 0 ? device.HardwareInfo.RamGB : existingDevice.HardwareInfo.RamGB;
                        existingDevice.HardwareInfo.DiskGB = device.HardwareInfo.DiskGB > 0 ? device.HardwareInfo.DiskGB : existingDevice.HardwareInfo.DiskGB;
                    }
                }

                // Update software info if available
                if (device.SoftwareInfo != null)
                {
                    if (existingDevice.SoftwareInfo == null)
                    {
                        changes.Add(new ChangeLog
                        {
                            Id = Guid.NewGuid(),
                            DeviceId = existingDevice.Id,
                            ChangeDate = DateTime.UtcNow,
                            ChangeType = "SoftwareInfo",
                            OldValue = "None",
                            NewValue = "Software information added",
                            ChangedBy = "Agent"
                        });
                        existingDevice.SoftwareInfo = device.SoftwareInfo;
                    }
                    else
                    {
                        // Track significant software changes
                        if (!string.IsNullOrEmpty(device.SoftwareInfo.OperatingSystem) && 
                            existingDevice.SoftwareInfo.OperatingSystem != device.SoftwareInfo.OperatingSystem)
                        {
                            changes.Add(new ChangeLog
                            {
                                Id = Guid.NewGuid(),
                                DeviceId = existingDevice.Id,
                                ChangeDate = DateTime.UtcNow,
                                ChangeType = "OperatingSystem",
                                OldValue = existingDevice.SoftwareInfo.OperatingSystem ?? "",
                                NewValue = device.SoftwareInfo.OperatingSystem,
                                ChangedBy = "Agent"
                            });
                        }

                        if (!string.IsNullOrEmpty(device.SoftwareInfo.OsVersion) && 
                            existingDevice.SoftwareInfo.OsVersion != device.SoftwareInfo.OsVersion)
                        {
                            changes.Add(new ChangeLog
                            {
                                Id = Guid.NewGuid(),
                                DeviceId = existingDevice.Id,
                                ChangeDate = DateTime.UtcNow,
                                ChangeType = "OSVersion",
                                OldValue = existingDevice.SoftwareInfo.OsVersion ?? "",
                                NewValue = device.SoftwareInfo.OsVersion,
                                ChangedBy = "Agent"
                            });
                        }

                        // Update software info fields
                        existingDevice.SoftwareInfo.OperatingSystem = device.SoftwareInfo.OperatingSystem ?? existingDevice.SoftwareInfo.OperatingSystem;
                        existingDevice.SoftwareInfo.OsVersion = device.SoftwareInfo.OsVersion ?? existingDevice.SoftwareInfo.OsVersion;
                        existingDevice.SoftwareInfo.OsArchitecture = device.SoftwareInfo.OsArchitecture ?? existingDevice.SoftwareInfo.OsArchitecture;
                        existingDevice.SoftwareInfo.RegisteredUser = device.SoftwareInfo.RegisteredUser ?? existingDevice.SoftwareInfo.RegisteredUser;
                        existingDevice.SoftwareInfo.SerialNumber = device.SoftwareInfo.SerialNumber ?? existingDevice.SoftwareInfo.SerialNumber;
                        existingDevice.SoftwareInfo.ActiveUser = device.SoftwareInfo.ActiveUser ?? existingDevice.SoftwareInfo.ActiveUser;
                        
                        // Update InstalledApps list based on provided data
                        if (device.SoftwareInfo.InstalledApps != null && device.SoftwareInfo.InstalledApps.Any())
                        {
                            existingDevice.SoftwareInfo.InstalledApps = device.SoftwareInfo.InstalledApps;
                        }
                    }
                }

                // Update other fields
                existingDevice.AgentInstalled = device.AgentInstalled;
                if (device.DiscoveryMethod != DiscoveryMethod.Unknown)
                    existingDevice.DiscoveryMethod = device.DiscoveryMethod;
                existingDevice.LastSeen = DateTime.UtcNow;

                // Add change logs to the device
                foreach (var changeLog in changes)
                {
                    existingDevice.ChangeLogs.Add(changeLog);
                }

                // Add any new change logs provided in the device update request
                if (device.ChangeLogs != null && device.ChangeLogs.Any())
                {
                    foreach (var newChangeLog in device.ChangeLogs)
                    {
                        // Ensure the change log has the correct device ID
                        newChangeLog.DeviceId = existingDevice.Id;
                        existingDevice.ChangeLogs.Add(newChangeLog);

                        // Process application install/uninstall changes to update InstalledApps list
                        if (existingDevice.SoftwareInfo != null)
                        {
                            ProcessApplicationChangeLog(existingDevice.SoftwareInfo, newChangeLog);
                        }
                    }
                }

                var result = await UpdateDeviceAsync(existingDevice);
                
                if (changes.Any())
                {
                    _logger.LogInformation("Device updated with {ChangeCount} changes: {DeviceName} ({MacAddress})", 
                        changes.Count, existingDevice.Name, existingDevice.MacAddress);
                }
                else
                {
                    _logger.LogInformation("Device seen but no changes detected: {DeviceName} ({MacAddress})", 
                        existingDevice.Name, existingDevice.MacAddress);
                }
                
                return result;
            }
            else
            {
                // Create new device
                device.Id = Guid.NewGuid();
                _logger.LogInformation("Creating new device: {DeviceName} ({MacAddress})", device.Name, device.MacAddress);
                return await CreateDeviceAsync(device);
            }
        }

        public async Task<List<Device>> BatchCreateOrUpdateDevicesAsync(List<Device> devices)
        {
            var results = new List<Device>();
            
            foreach (var device in devices)
            {
                try
                {
                    var result = await CreateOrUpdateDeviceAsync(device);
                    if (result != null)
                    {
                        results.Add(result);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to create/update device: {DeviceName}", device.Name);
                }
            }
            
            return results;
        }

        /// <summary>
        /// Processes application install/uninstall change logs to update the InstalledApps list
        /// </summary>
        private void ProcessApplicationChangeLog(DeviceSoftwareInfo softwareInfo, ChangeLog changeLog)
        {
            // Ensure InstalledApps list exists
            softwareInfo.InstalledApps ??= new List<string>();

            switch (changeLog.ChangeType)
            {
                case "Application Installed":
                    // Add new application if not already present
                    if (!string.IsNullOrEmpty(changeLog.NewValue) && 
                        !softwareInfo.InstalledApps.Any(app => app.Equals(changeLog.NewValue, StringComparison.OrdinalIgnoreCase)))
                    {
                        softwareInfo.InstalledApps.Add(changeLog.NewValue);
                        _logger.LogDebug("Added application to InstalledApps: {Application}", changeLog.NewValue);
                    }
                    break;

                case "Application Uninstalled":
                    // Remove application if present
                    if (!string.IsNullOrEmpty(changeLog.OldValue))
                    {
                        var appToRemove = softwareInfo.InstalledApps.FirstOrDefault(app => 
                            app.Equals(changeLog.OldValue, StringComparison.OrdinalIgnoreCase));
                        
                        if (appToRemove != null)
                        {
                            softwareInfo.InstalledApps.Remove(appToRemove);
                            _logger.LogDebug("Removed application from InstalledApps: {Application}", changeLog.OldValue);
                        }
                    }
                    break;

                case "Application Updated":
                    // Handle application updates (remove old, add new)
                    if (!string.IsNullOrEmpty(changeLog.OldValue) && !string.IsNullOrEmpty(changeLog.NewValue))
                    {
                        var oldAppToRemove = softwareInfo.InstalledApps.FirstOrDefault(app => 
                            app.Equals(changeLog.OldValue, StringComparison.OrdinalIgnoreCase));
                        
                        if (oldAppToRemove != null)
                        {
                            softwareInfo.InstalledApps.Remove(oldAppToRemove);
                        }
                        
                        if (!softwareInfo.InstalledApps.Any(app => app.Equals(changeLog.NewValue, StringComparison.OrdinalIgnoreCase)))
                        {
                            softwareInfo.InstalledApps.Add(changeLog.NewValue);
                        }
                        
                        _logger.LogDebug("Updated application in InstalledApps: {OldApp} -> {NewApp}", 
                            changeLog.OldValue, changeLog.NewValue);
                    }
                    break;

                // Handle version updates that might come as different change types
                case "Software Version Changed":
                case "Application Version Updated":
                    if (!string.IsNullOrEmpty(changeLog.OldValue) && !string.IsNullOrEmpty(changeLog.NewValue))
                    {
                        // Try to find the application by extracting the base name (without version)
                        var oldApp = ExtractAppNameFromVersionString(changeLog.OldValue);
                        var newApp = ExtractAppNameFromVersionString(changeLog.NewValue);
                        
                        // Remove the old version
                        var existingApp = softwareInfo.InstalledApps.FirstOrDefault(app => 
                            ExtractAppNameFromVersionString(app).Equals(oldApp, StringComparison.OrdinalIgnoreCase) ||
                            app.Equals(changeLog.OldValue, StringComparison.OrdinalIgnoreCase));
                            
                        if (existingApp != null)
                        {
                            softwareInfo.InstalledApps.Remove(existingApp);
                        }
                        
                        // Add the new version
                        if (!softwareInfo.InstalledApps.Any(app => app.Equals(changeLog.NewValue, StringComparison.OrdinalIgnoreCase)))
                        {
                            softwareInfo.InstalledApps.Add(changeLog.NewValue);
                        }
                        
                        _logger.LogDebug("Updated application version in InstalledApps: {OldApp} -> {NewApp}", 
                            changeLog.OldValue, changeLog.NewValue);
                    }
                    break;
            }
        }

        /// <summary>
        /// Extracts application name from a version string (e.g., "Microsoft Edge 109.0.1518.78" -> "Microsoft Edge")
        /// </summary>
        private string ExtractAppNameFromVersionString(string appWithVersion)
        {
            if (string.IsNullOrEmpty(appWithVersion))
                return appWithVersion;

            // Try to find version pattern and remove it
            // Common patterns: "AppName 1.2.3", "AppName (1.2.3)", "AppName v1.2.3"
            var patterns = new[]
            {
                @"\s+\d+(\.\d+)*(\.\d+)*(\.\d+)*$",  // "AppName 1.2.3.4"
                @"\s+v\d+(\.\d+)*(\.\d+)*(\.\d+)*$", // "AppName v1.2.3.4"
                @"\s+\(\d+(\.\d+)*(\.\d+)*(\.\d+)*\)$", // "AppName (1.2.3.4)"
                @"\s+version\s+\d+(\.\d+)*(\.\d+)*(\.\d+)*$" // "AppName version 1.2.3.4"
            };

            foreach (var pattern in patterns)
            {
                var match = System.Text.RegularExpressions.Regex.Match(appWithVersion, pattern, 
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    return appWithVersion.Substring(0, match.Index).Trim();
                }
            }

            return appWithVersion; // Return original if no version pattern found
        }
    }
}