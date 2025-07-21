using Inventory.Api.Controllers;
using Inventory.Api.Helpers;

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
                throw new InvalidOperationException("A scan is already in progress.");
            }

            await TriggerScanAsync("Manual", null);
        }

        public async Task TriggerManualScanAsync(string networkRange)
        {
            if (_isScanning)
            {
                throw new InvalidOperationException("A scan is already in progress.");
            }

            await TriggerScanAsync("Manual", networkRange);
        }

        public async Task TriggerScanAllNetworksAsync()
        {
            if (_isScanning)
            {
                throw new InvalidOperationException("A scan is already in progress.");
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

                // Register discovered devices
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
                    Status = error == null ? "Completed" : "Failed",
                    DevicesFound = devicesFound,
                    Error = error,
                    NetworkRange = networkRange ?? (scanAllNetworks ? "All Networks" : NetworkRangeDetector.GetPrimaryNetworkRange())
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
            
            // If no network range specified, use auto-detected range
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
    }

    public class DeviceService : IDeviceService
    {
        private readonly ILogger<DeviceService> _logger;
        private readonly ICentralizedLoggingService _loggingService;

        public DeviceService(ILogger<DeviceService> logger, ICentralizedLoggingService loggingService)
        {
            _logger = logger;
            _loggingService = loggingService;
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
    }
}