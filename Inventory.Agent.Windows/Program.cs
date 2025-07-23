using System;
using System.Management;
using System.Threading.Tasks;
using Inventory.Agent.Windows.Models;
using System.Collections.Generic;
using LibreHardwareMonitor.Hardware;
using System.Runtime.InteropServices;
using Inventory.Agent.Windows.Configuration;
using Inventory.Agent.Windows.Services;
using System.Threading;

namespace Inventory.Agent.Windows
{
    class Program
    {
        private static ConnectivityMonitorService? _connectivityMonitor;
        
        static async Task Main(string[] args)
        {
            var apiSettings = ApiSettings.LoadFromEnvironment();
            OfflineStorageService? offlineStorage = null;
            
            try
            {
                Console.WriteLine("Inventory Management System - Agent");
                Console.WriteLine("===================================");
                Console.WriteLine($"API Base URL: {apiSettings.BaseUrl}");
                Console.WriteLine($"Offline Storage: {(apiSettings.EnableOfflineStorage ? "Enabled" : "Disabled")}");
                
                // Initialize offline storage if enabled
                if (apiSettings.EnableOfflineStorage)
                {
                    offlineStorage = new OfflineStorageService(apiSettings.OfflineStoragePath, apiSettings.MaxOfflineRecords);
                    var offlineCount = await offlineStorage.GetStoredRecordCountAsync();
                    Console.WriteLine($"Offline records pending: {offlineCount}");
                    
                    // Start connectivity monitoring
                    _connectivityMonitor = new ConnectivityMonitorService(apiSettings, offlineStorage);
                    _connectivityMonitor.StartMonitoring();
                }
                
                // Check for network discovery argument
                bool networkDiscovery = args.Length > 0 && args[0].ToLower() == "network";
                
                if (networkDiscovery)
                {
                    Console.WriteLine("Starting network discovery...");
                    await RunNetworkDiscoveryAsync(apiSettings);
                }
                else
                {
                    Console.WriteLine("Starting local system inventory...");
                    await RunLocalInventoryAsync(apiSettings, offlineStorage);
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

            Console.WriteLine("\nİşlem tamamlandı.");
            
            // Only wait for key press if running interactively (not in Docker)
            if (IsRunningInteractively())
            {
                Console.WriteLine("Çıkmak için bir tuşa basın.");
                Console.ReadKey();
            }
            else
            {
                Console.WriteLine("Agent execution completed.");
                
                // If connectivity monitor is running, wait for a while to allow batch uploads
                if (_connectivityMonitor != null)
                {
                    Console.WriteLine("Waiting for potential batch uploads...");
                    await Task.Delay(TimeSpan.FromSeconds(30));
                }
            }
            
            // Stop connectivity monitoring
            _connectivityMonitor?.Stop();
        }
        
        /// <summary>
        /// Checks if the application is running in an interactive environment
        /// </summary>
        private static bool IsRunningInteractively()
        {
            try
            {
                // Check if console input is available and not redirected
                return Environment.UserInteractive && !Console.IsInputRedirected;
            }
            catch
            {
                return false;
            }
        }

        static async Task RunLocalInventoryAsync(ApiSettings apiSettings, OfflineStorageService? offlineStorage)
        {
            // Çapraz platform sistem bilgilerini topla
            var device = CrossPlatformSystemInfo.GatherSystemInformation();

            // --- Cihazı logla ---
            DeviceLogger.LogDevice(device);

            // --- API'ye gönder ---
            string apiUrl = apiSettings.GetDeviceEndpoint();
            bool success = await ApiClient.PostDeviceAsync(device, apiUrl, offlineStorage);
            
            if (success)
            {
                Console.WriteLine("Cihaz başarıyla API'ye gönderildi!");
            }
            else if (offlineStorage != null)
            {
                Console.WriteLine("Gönderim başarısız, veri offline olarak saklandı.");
            }
            else
            {
                Console.WriteLine("Gönderim başarısız.");
            }
        }

        static async Task RunNetworkDiscoveryAsync(ApiSettings apiSettings)
        {
            CentralizedLogger logger = null;
            try
            {
                logger = new CentralizedLogger(apiSettings.BaseUrl, "Agent.NetworkDiscovery");
                await logger.LogInfoAsync("Starting network discovery mode");
                
                Console.WriteLine("Network Discovery Mode");
                Console.WriteLine("=====================");
                
                // Initialize network discovery reporter
                var reporter = new NetworkDiscoveryReporter(
                    apiBaseUrl: apiSettings.GetNetworkDiscoveryEndpoint(),
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