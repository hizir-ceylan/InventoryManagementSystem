using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Inventory.Agent.Windows.Models;

namespace Inventory.Agent.Windows
{
    public class NetworkDiscoveryReporter
    {
        private readonly NetworkScanner _networkScanner;
        private readonly string _apiBaseUrl;
        private readonly CentralizedLogger _logger;

        public NetworkDiscoveryReporter(string apiBaseUrl = "https://localhost:5001/api/devices", string networkRange = "192.168.1.0/24")
        {
            _networkScanner = new NetworkScanner(networkRange);
            _apiBaseUrl = apiBaseUrl;
            _logger = new CentralizedLogger(apiBaseUrl.Replace("/api/devices", ""), "NetworkDiscoveryReporter");
        }

        public async Task<bool> DiscoverAndReportDevicesAsync()
        {
            try
            {
                Console.WriteLine("Starting network discovery...");
                await _logger.LogInfoAsync("Starting network discovery");
                
                // Scan network for devices
                var discoveredDevices = await _networkScanner.ScanNetworkAsync();
                
                Console.WriteLine($"Found {discoveredDevices.Count} devices on the network.");
                await _logger.LogInfoAsync($"Found {discoveredDevices.Count} devices on the network");

                // Report each device to the API
                var successCount = 0;
                foreach (var device in discoveredDevices)
                {
                    try
                    {
                        Console.WriteLine($"Reporting device: {device.Name} ({device.IpAddress}) - {device.Manufacturer}");
                        
                        // Ensure proper null/empty field handling
                        device.HardwareInfo = null; // Network discovered devices don't have hardware info
                        device.SoftwareInfo = null; // Network discovered devices don't have software info
                        device.ChangeLogs = device.ChangeLogs ?? new List<ChangeLogDto>();
                        device.Model = string.IsNullOrEmpty(device.Model) ? "Unknown" : device.Model;
                        device.Location = string.IsNullOrEmpty(device.Location) ? "Network Discovery" : device.Location;
                        
                        var success = await ApiClient.PostDeviceAsync(device, _apiBaseUrl);
                        if (success)
                        {
                            successCount++;
                            Console.WriteLine($"✓ Successfully reported {device.Name}");
                            await _logger.LogInfoAsync($"Successfully reported device: {device.Name} ({device.IpAddress})");
                        }
                        else
                        {
                            Console.WriteLine($"✗ Failed to report {device.Name}");
                            await _logger.LogWarningAsync($"Failed to report device: {device.Name} ({device.IpAddress})");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"✗ Error reporting device {device.Name}: {ex.Message}");
                    }
                }

                Console.WriteLine($"Network discovery completed. {successCount}/{discoveredDevices.Count} devices reported successfully.");
                return successCount > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Network discovery failed: {ex.Message}");
                return false;
            }
        }

        public async Task<List<DeviceDto>> GetDiscoveredDevicesAsync()
        {
            try
            {
                Console.WriteLine("Scanning network for devices...");
                var devices = await _networkScanner.ScanNetworkAsync();
                Console.WriteLine($"Found {devices.Count} devices on the network.");
                return devices;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error scanning network: {ex.Message}");
                return new List<DeviceDto>();
            }
        }
    }
}
