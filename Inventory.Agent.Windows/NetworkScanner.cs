using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using Inventory.Agent.Windows.Models;
using Inventory.Domain.Entities;
using Inventory.Shared.Utils;

namespace Inventory.Agent.Windows
{
    public class NetworkScanner
    {
        private readonly string _networkRange;
        private readonly CentralizedLogger _logger;

        public NetworkScanner(string networkRange = "192.168.1.0/24")
        {
            _networkRange = networkRange;
            _logger = new CentralizedLogger("https://localhost:5001", "NetworkScanner");
        }

        public async Task<List<DeviceDto>> ScanNetworkAsync()
        {
            var devices = new List<DeviceDto>();
            var tasks = new List<Task<DeviceDto?>>();

            try
            {
                await _logger.LogInfoAsync($"Starting network scan for range: {_networkRange}");

                // Parse network range and create IP addresses to scan
                var ipAddresses = GetIPAddressesFromRange(_networkRange);

                foreach (var ip in ipAddresses)
                {
                    tasks.Add(ScanHostAsync(ip));
                }

                var results = await Task.WhenAll(tasks);
                
                // Filter out null results
                devices.AddRange(results.Where(device => device != null)!);

                await _logger.LogInfoAsync($"Network scan completed. Found {devices.Count} devices");
                return devices;
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"Network scan failed", ex);
                throw;
            }
        }

        private async Task<DeviceDto?> ScanHostAsync(IPAddress ipAddress)
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

                        var device = new DeviceDto
                        {
                            Name = await GetHostNameAsync(ipAddress),
                            IpAddress = ipAddress.ToString(),
                            MacAddress = macAddress,
                            DeviceType = deviceType,
                            Manufacturer = manufacturer,
                            Status = 1, // Aktif
                            AgentInstalled = false, // Ağ üzerinden keşfedilen cihazlarda agent yok
                            Location = "Network Discovery",
                            Model = "Unknown",
                            ChangeLogs = new List<ChangeLogDto>(),
                            HardwareInfo = null, // Ağ üzerinden keşfedilen cihazlarda donanım bilgisi yok
                            SoftwareInfo = null  // Ağ üzerinden keşfedilen cihazlarda yazılım bilgisi yok
                        };

                        await _logger.LogInfoAsync($"Device discovered: {device.Name} ({device.IpAddress}) - {device.Manufacturer}");
                        return device;
                    }
                }
            }
            catch (Exception ex)
            {
                // Hatayı günlükle ancak fırlat - diğer host'ları taramaya devam et
                await _logger.LogWarningAsync($"Error scanning {ipAddress}: {ex.Message}");
                Console.WriteLine($"Error scanning {ipAddress}: {ex.Message}");
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

        private async Task<string?> GetMacAddressAsync(IPAddress ipAddress)
        {
            try
            {
                // MAC adresini almak için ARP tablosunu kullan
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

                    // MAC adresini çıkarmak için ARP çıktısını ayrıştır
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
                Console.WriteLine($"Error getting MAC address for {ipAddress}: {ex.Message}");
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
                // Tek IP adresi
                ipAddresses.Add(IPAddress.Parse(networkRange));
            }

            return ipAddresses;
        }
    }
}