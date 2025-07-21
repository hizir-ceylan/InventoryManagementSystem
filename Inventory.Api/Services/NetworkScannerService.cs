using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using Inventory.Api.Helpers;
using Inventory.Api.Services;
using Inventory.Shared.Utils;
using Inventory.Domain.Entities;

namespace Inventory.Api.Services
{
    public interface INetworkScannerService
    {
        Task<List<NetworkDeviceResult>> ScanNetworkAsync(string? networkRange = null);
        Task<List<NetworkDeviceResult>> ScanAllLocalNetworksAsync();
        Task<string?> GetMacAddressAsync(IPAddress ipAddress);
        List<string> GetLocalNetworkRanges();
    }

    public class NetworkScannerService : INetworkScannerService
    {
        private readonly ILogger<NetworkScannerService> _logger;
        private readonly ICentralizedLoggingService _loggingService;

        public NetworkScannerService(ILogger<NetworkScannerService> logger, ICentralizedLoggingService loggingService)
        {
            _logger = logger;
            _loggingService = loggingService;
        }

        public async Task<List<NetworkDeviceResult>> ScanNetworkAsync(string? networkRange = null)
        {
            var devices = new List<NetworkDeviceResult>();
            var tasks = new List<Task<NetworkDeviceResult?>>();

            try
            {
                // Use provided range or detect automatically
                var rangeToScan = networkRange ?? NetworkRangeDetector.GetPrimaryNetworkRange();
                
                await _loggingService.LogInfoAsync("NetworkScanner", $"Starting network scan for range: {rangeToScan}");
                _logger.LogInformation("Starting network scan for range: {NetworkRange}", rangeToScan);

                // Parse network range and create IP addresses to scan
                var ipAddresses = GetIPAddressesFromRange(rangeToScan);
                _logger.LogInformation("Scanning {IpCount} IP addresses in range {NetworkRange}", ipAddresses.Count, rangeToScan);

                foreach (var ip in ipAddresses)
                {
                    tasks.Add(ScanHostAsync(ip));
                }

                var results = await Task.WhenAll(tasks);
                
                // Filter out null results
                devices.AddRange(results.Where(device => device != null)!);

                await _loggingService.LogInfoAsync("NetworkScanner", $"Network scan completed. Found {devices.Count} devices in range {rangeToScan}");
                _logger.LogInformation("Network scan completed. Found {DeviceCount} devices in range {NetworkRange}", devices.Count, rangeToScan);
                
                return devices;
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync("NetworkScanner", "Network scan failed", ex);
                _logger.LogError(ex, "Network scan failed for range {NetworkRange}", networkRange);
                throw;
            }
        }

        public async Task<List<NetworkDeviceResult>> ScanAllLocalNetworksAsync()
        {
            var allDevices = new List<NetworkDeviceResult>();
            var networkRanges = GetLocalNetworkRanges();

            _logger.LogInformation("Scanning {RangeCount} local network ranges", networkRanges.Count);
            
            foreach (var range in networkRanges)
            {
                try
                {
                    var devices = await ScanNetworkAsync(range);
                    allDevices.AddRange(devices);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to scan network range {NetworkRange}", range);
                    await _loggingService.LogErrorAsync("NetworkScanner", $"Failed to scan network range {range}", ex);
                }
            }

            // Remove duplicates based on MAC address
            var uniqueDevices = allDevices
                .Where(d => !string.IsNullOrEmpty(d.MacAddress))
                .GroupBy(d => d.MacAddress)
                .Select(g => g.First())
                .ToList();

            _logger.LogInformation("Found {TotalDevices} total devices across all network ranges ({UniqueDevices} unique)", 
                allDevices.Count, uniqueDevices.Count);

            return uniqueDevices;
        }

        public List<string> GetLocalNetworkRanges()
        {
            return NetworkRangeDetector.GetLocalNetworkRanges();
        }

        private async Task<NetworkDeviceResult?> ScanHostAsync(IPAddress ipAddress)
        {
            try
            {
                var ping = new Ping();
                var reply = await ping.SendPingAsync(ipAddress, 1000);

                if (reply.Status == IPStatus.Success)
                {
                    var macAddress = await GetMacAddressAsync(ipAddress);
                    if (!string.IsNullOrEmpty(macAddress))
                    {
                        var manufacturer = OuiLookup.GetManufacturer(macAddress);
                        var deviceType = OuiLookup.GuessDeviceType(macAddress, manufacturer);

                        var device = new NetworkDeviceResult
                        {
                            Name = await GetHostNameAsync(ipAddress),
                            IpAddress = ipAddress.ToString(),
                            MacAddress = macAddress,
                            DeviceType = deviceType,
                            Manufacturer = manufacturer,
                            Status = DeviceStatus.Active,
                            AgentInstalled = false, // Network-discovered devices don't have agent
                            Location = "Network Discovery",
                            Model = "Unknown",
                            DiscoveryMethod = DiscoveryMethod.NetworkDiscovery,
                            LastSeen = DateTime.UtcNow,
                            ResponseTime = reply.RoundtripTime
                        };

                        await _loggingService.LogInfoAsync("NetworkScanner", $"Device discovered: {device.Name} ({device.IpAddress}) - {device.Manufacturer}");
                        _logger.LogDebug("Device discovered: {DeviceName} ({IpAddress}) - {Manufacturer}", device.Name, device.IpAddress, device.Manufacturer);
                        
                        return device;
                    }
                }
            }
            catch (Exception ex)
            {
                // Log error but don't throw - continue scanning other hosts
                _logger.LogDebug(ex, "Error scanning {IpAddress}: {ErrorMessage}", ipAddress, ex.Message);
            }

            return null;
        }

        private async Task<string> GetHostNameAsync(IPAddress ipAddress)
        {
            try
            {
                var hostEntry = await Dns.GetHostEntryAsync(ipAddress);
                return hostEntry.HostName;
            }
            catch
            {
                return $"Host-{ipAddress}";
            }
        }

        public async Task<string?> GetMacAddressAsync(IPAddress ipAddress)
        {
            try
            {
                // Use ARP table to get MAC address
                var startInfo = new ProcessStartInfo
                {
                    FileName = "arp",
                    Arguments = $"-a {ipAddress}",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(startInfo);
                if (process != null)
                {
                    var output = await process.StandardOutput.ReadToEndAsync();
                    await process.WaitForExitAsync();

                    // Parse ARP output to extract MAC address
                    var lines = output.Split('\n');
                    foreach (var line in lines)
                    {
                        if (line.Contains(ipAddress.ToString()) && line.Contains("-"))
                        {
                            var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                            foreach (var part in parts)
                            {
                                if (part.Length == 17 && part.Count(c => c == '-') == 5)
                                {
                                    return part.Replace('-', ':').ToUpper();
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Error getting MAC address for {IpAddress}", ipAddress);
            }

            return null;
        }

        private List<IPAddress> GetIPAddressesFromRange(string networkRange)
        {
            var ipAddresses = new List<IPAddress>();

            if (networkRange.Contains("/"))
            {
                var parts = networkRange.Split('/');
                var baseAddress = IPAddress.Parse(parts[0]);
                var prefixLength = int.Parse(parts[1]);

                var mask = ~(uint.MaxValue >> prefixLength);
                var networkAddress = BitConverter.ToUInt32(baseAddress.GetAddressBytes().Reverse().ToArray(), 0) & mask;
                var broadcastAddress = networkAddress | ~mask;

                for (var addr = networkAddress + 1; addr < broadcastAddress; addr++)
                {
                    var bytes = BitConverter.GetBytes(addr).Reverse().ToArray();
                    ipAddresses.Add(new IPAddress(bytes));
                }
            }
            else
            {
                // Single IP address
                ipAddresses.Add(IPAddress.Parse(networkRange));
            }

            return ipAddresses;
        }
    }

    public class NetworkDeviceResult
    {
        public string Name { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
        public string MacAddress { get; set; } = string.Empty;
        public DeviceType DeviceType { get; set; }
        public string? Manufacturer { get; set; }
        public DeviceStatus Status { get; set; }
        public bool AgentInstalled { get; set; }
        public string Location { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public DiscoveryMethod DiscoveryMethod { get; set; }
        public DateTime LastSeen { get; set; }
        public long ResponseTime { get; set; }
    }
}