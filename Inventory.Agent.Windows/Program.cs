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
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Inventory.Agent.Windows
{
    class Program
    {
        private static ConnectivityMonitorService? _connectivityMonitor;
        
        static async Task Main(string[] args)
        {
            // Check for help argument
            if (args.Length > 0 && (args[0].ToLower() == "--help" || args[0].ToLower() == "-h"))
            {
                ShowUsage();
                return;
            }

            // Service modu kontrolü - Windows service olarak çalışıp çalışmadığını kontrol et
            bool isWindowsService = !Environment.UserInteractive || 
                                   (args.Length > 0 && args[0].ToLower() == "--service");
            bool isNetworkDiscovery = args.Length > 0 && args[0].ToLower() == "network";

            if (isWindowsService)
            {
                await RunAsServiceAsync();
                return;
            }

            // Normal console mode - mevcut kod
            await RunAsConsoleAsync(args);
        }

        static void ShowUsage()
        {
            Console.WriteLine("Inventory Management System - Agent");
            Console.WriteLine("===================================");
            Console.WriteLine();
            Console.WriteLine("Kullanım (Usage):");
            Console.WriteLine("  Inventory.Agent.Windows.exe [options]");
            Console.WriteLine();
            Console.WriteLine("Seçenekler (Options):");
            Console.WriteLine("  (hiçbiri)            Tek seferlik envanter taraması yapar ve çıkar");
            Console.WriteLine("  --continuous         Sürekli mod - her 30 dakikada bir tarama yapar");
            Console.WriteLine("  --daemon             --continuous ile aynı");
            Console.WriteLine("  network              Ağ keşfi modunda çalışır");
            Console.WriteLine("  --service            Servis modunda çalışır (otomatik algılanır)");
            Console.WriteLine("  --help, -h           Bu yardım mesajını gösterir");
            Console.WriteLine();
            Console.WriteLine("Örnekler (Examples):");
            Console.WriteLine("  Inventory.Agent.Windows.exe                    # Tek seferlik çalıştır");
            Console.WriteLine("  Inventory.Agent.Windows.exe --continuous       # Sürekli modda çalıştır");
            Console.WriteLine("  Inventory.Agent.Windows.exe network            # Ağ keşfi yap");
            Console.WriteLine();
            Console.WriteLine("Servis kurulumu için: build-tools/Install-WindowsServices.ps1");
        }

        static async Task RunAsServiceAsync()
        {
            try
            {
                var builder = Host.CreateApplicationBuilder();
                
                // Windows Service desteği ekle
                builder.Services.AddWindowsService(options =>
                {
                    options.ServiceName = "InventoryManagementAgent";
                });

                // Logging yapılandırması
                builder.Services.AddLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.AddConsole();
                    if (OperatingSystem.IsWindows())
                    {
                        logging.AddEventLog(settings =>
                        {
                            settings.SourceName = "InventoryManagementAgent";
                            settings.LogName = "Application";
                        });
                    }
                });

                // Hosted service ekle
                builder.Services.AddHostedService<InventoryAgentService>();

                var host = builder.Build();
                
                // Service'i başlat
                await host.RunAsync();
            }
            catch (Exception ex)
            {
                // Service startup başarısız olursa event log'a yazalım
                if (OperatingSystem.IsWindows())
                {
                    try
                    {
                        using var eventLog = new System.Diagnostics.EventLog("Application");
                        eventLog.Source = "InventoryManagementAgent";
                        eventLog.WriteEntry($"Service startup failed: {ex.Message}", 
                                          System.Diagnostics.EventLogEntryType.Error);
                    }
                    catch
                    {
                        // Event log yazma başarısız olsa bile continue et
                    }
                }
                
                // Exception'ı re-throw etme, service start timeout'una neden olabilir
                Console.WriteLine($"Service startup error: {ex.Message}");
            }
        }

        static async Task RunAsConsoleAsync(string[] args)
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
                
                // Check for different operating modes
                bool networkDiscovery = args.Length > 0 && args[0].ToLower() == "network";
                bool continuousMode = args.Length > 0 && (args[0].ToLower() == "--continuous" || args[0].ToLower() == "--daemon");
                
                if (networkDiscovery)
                {
                    Console.WriteLine("Starting network discovery...");
                    await RunNetworkDiscoveryAsync(apiSettings);
                }
                else if (continuousMode)
                {
                    Console.WriteLine("Starting continuous mode (runs every 30 minutes)...");
                    Console.WriteLine("Press Ctrl+C to stop.");
                    await RunContinuousInventoryAsync(apiSettings, offlineStorage);
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

        static async Task RunContinuousInventoryAsync(ApiSettings apiSettings, OfflineStorageService? offlineStorage)
        {
            const int intervalMinutes = 30;
            using var cts = new CancellationTokenSource();
            
            // Handle Ctrl+C gracefully
            Console.CancelKeyPress += (sender, e) =>
            {
                e.Cancel = true;
                cts.Cancel();
                Console.WriteLine("\nDurdurma sinyali alındı. Güvenli bir şekilde kapatılıyor...");
            };

            try
            {
                // İlk taramayı hemen yap
                Console.WriteLine("İlk envanter taraması başlatılıyor...");
                await RunLocalInventoryAsync(apiSettings, offlineStorage);
                
                int runCount = 1;
                Console.WriteLine($"İlk tarama tamamlandı. Sonraki tarama {intervalMinutes} dakika sonra.");
                
                while (!cts.Token.IsCancellationRequested)
                {
                    try
                    {
                        var nextRun = DateTime.Now.AddMinutes(intervalMinutes);
                        Console.WriteLine($"Sonraki tarama: {nextRun:HH:mm:ss}. İptal etmek için Ctrl+C kullanın.");
                        
                        await Task.Delay(TimeSpan.FromMinutes(intervalMinutes), cts.Token);
                        
                        if (!cts.Token.IsCancellationRequested)
                        {
                            runCount++;
                            Console.WriteLine($"\n--- Tarama #{runCount} ({DateTime.Now:HH:mm:ss}) ---");
                            await RunLocalInventoryAsync(apiSettings, offlineStorage);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Tarama sırasında hata: {ex.Message}");
                        Console.WriteLine("Bir sonraki döngüde devam edilecek...");
                    }
                }
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Sürekli mod durduruldu.");
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