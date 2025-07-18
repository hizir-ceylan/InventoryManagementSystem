using System;
using System.Management;
using System.Threading.Tasks;
using Inventory.Agent.Windows.Models;
using System.Collections.Generic;
using LibreHardwareMonitor.Hardware;
using System.Runtime.InteropServices;

namespace Inventory.Agent.Windows
{
    class Program
    {
        static async Task Main(string[] args)
        {
            try
            {
                Console.WriteLine("Inventory Management System - Agent");
                Console.WriteLine("===================================");
                
                // Check for network discovery argument
                bool networkDiscovery = args.Length > 0 && args[0].ToLower() == "network";
                
                if (networkDiscovery)
                {
                    Console.WriteLine("Starting network discovery...");
                    await RunNetworkDiscoveryAsync();
                }
                else
                {
                    Console.WriteLine("Starting local system inventory...");
                    await RunLocalInventoryAsync();
                }
            }
            catch (PlatformNotSupportedException ex)
            {
                Console.WriteLine($"Platform desteklenmiyor: {ex.Message}");
                Console.WriteLine("Şu anda Windows ve Linux desteklenmektedir.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Bir hata oluştu: {ex.Message}");
            }

            Console.WriteLine("\nİşlem tamamlandı. Çıkmak için bir tuşa basın.");
            Console.ReadKey();
        }

        static async Task RunLocalInventoryAsync()
        {
            // Çapraz platform sistem bilgilerini topla
            var device = CrossPlatformSystemInfo.GatherSystemInformation();

            // --- Cihazı logla ---
            DeviceLogger.LogDevice(device);

            // --- API'ye gönder ---
            string apiUrl = "https://localhost:7296/api/device";
            bool success = await ApiClient.PostDeviceAsync(device, apiUrl);
            Console.WriteLine(success ? "Cihaz başarıyla API'ye gönderildi!" : "Gönderim başarısız.");
        }

        static async Task RunNetworkDiscoveryAsync()
        {
            CentralizedLogger logger = null;
            try
            {
                logger = new CentralizedLogger("https://localhost:7296", "Agent.NetworkDiscovery");
                await logger.LogInfoAsync("Starting network discovery mode");
                
                Console.WriteLine("Network Discovery Mode");
                Console.WriteLine("=====================");
                
                // Initialize network discovery reporter
                var reporter = new NetworkDiscoveryReporter(
                    apiBaseUrl: "https://localhost:7296/api/devices/network-discovered",
                    networkRange: "192.168.1.0/24"
                );

                // Discover and report devices
                var success = await reporter.DiscoverAndReportDevicesAsync();
                
                if (success)
                {
                    Console.WriteLine("\n✓ Network discovery completed successfully!");
                    await logger.LogInfoAsync("Network discovery completed successfully");
                }
                else
                {
                    Console.WriteLine("\n✗ Network discovery failed or found no devices.");
                    await logger.LogWarningAsync("Network discovery failed or found no devices");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Network discovery error: {ex.Message}");
                if (logger != null)
                {
                    await logger.LogErrorAsync("Network discovery error", ex);
                }
            }
            finally
            {
                logger?.Dispose();
            }
        }
    }
}