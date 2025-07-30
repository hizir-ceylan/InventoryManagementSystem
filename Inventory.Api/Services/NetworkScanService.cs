using Inventory.Api.Controllers;
using Inventory.Api.Helpers;
using Inventory.Domain.Entities;
using Inventory.Data;
using Microsoft.EntityFrameworkCore;

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
                .Include(d => d.ChangeLogs)
                .ToListAsync();
        }

        public async Task<IEnumerable<Device>> GetAgentInstalledDevicesAsync()
        {
            return await _context.Devices
                .Where(d => d.AgentInstalled || d.ManagementType == ManagementType.Agent)
                .Include(d => d.ChangeLogs)
                .ToListAsync();
        }

        public async Task<IEnumerable<Device>> GetNetworkDiscoveredDevicesAsync()
        {
            return await _context.Devices
                .Where(d => !d.AgentInstalled && 
                    (d.ManagementType == ManagementType.NetworkDiscovery || d.DiscoveryMethod == DiscoveryMethod.NetworkDiscovery))
                .Include(d => d.ChangeLogs)
                .ToListAsync();
        }

        public async Task<Device?> GetDeviceByIdAsync(Guid id)
        {
            return await _context.Devices
                .Include(d => d.ChangeLogs)
                .FirstOrDefaultAsync(d => d.Id == id);
        }

        public async Task<Device?> FindDeviceByIpOrMacAsync(string? ipAddress, string? macAddress)
        {
            return await _context.Devices
                .Include(d => d.ChangeLogs)
                .FirstOrDefaultAsync(d => 
                    (!string.IsNullOrWhiteSpace(ipAddress) && d.IpAddress == ipAddress) ||
                    (!string.IsNullOrWhiteSpace(macAddress) && d.MacAddress == macAddress));
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
            
            _context.Devices.Add(device);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Device created: {DeviceName} ({DeviceId})", device.Name, device.Id);
            return device;
        }

        public async Task<Device> UpdateDeviceAsync(Device device)
        {
            device.LastSeen = DateTime.UtcNow;
            _context.Devices.Update(device);
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
            var existingDevice = await FindDeviceByIpOrMacAsync(device.IpAddress, device.MacAddress);
            
            if (existingDevice != null)
            {
                // Update existing device with new information
                existingDevice.Name = device.Name ?? existingDevice.Name;
                existingDevice.DeviceType = device.DeviceType != DeviceType.Unknown ? device.DeviceType : existingDevice.DeviceType;
                existingDevice.Model = device.Model ?? existingDevice.Model;
                existingDevice.Location = device.Location ?? existingDevice.Location;
                existingDevice.ManagementType = device.ManagementType != ManagementType.Unknown ? device.ManagementType : existingDevice.ManagementType;
                existingDevice.AgentInstalled = device.AgentInstalled;
                existingDevice.Status = device.Status;
                existingDevice.HardwareInfo = device.HardwareInfo ?? existingDevice.HardwareInfo;
                existingDevice.SoftwareInfo = device.SoftwareInfo ?? existingDevice.SoftwareInfo;
                existingDevice.LastSeen = DateTime.UtcNow;

                return await UpdateDeviceAsync(existingDevice);
            }
            else
            {
                // Create new device
                device.Id = Guid.NewGuid();
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
    }
}