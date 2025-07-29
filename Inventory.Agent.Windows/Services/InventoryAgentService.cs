using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Inventory.Agent.Windows.Configuration;
using Inventory.Agent.Windows.Services;
using Inventory.Agent.Windows.Models;
using System.Threading;
using System.Threading.Tasks;

namespace Inventory.Agent.Windows.Services
{
    public class InventoryAgentService : BackgroundService
    {
        private readonly ILogger<InventoryAgentService> _logger;
        private readonly ApiSettings _apiSettings;
        private ConnectivityMonitorService? _connectivityMonitor;
        private readonly int _inventoryIntervalMinutes = 30; // Her 30 dakikada bir
        private OfflineStorageService? _offlineStorage;

        public InventoryAgentService(ILogger<InventoryAgentService> logger)
        {
            _logger = logger;
            _apiSettings = ApiSettings.LoadFromEnvironment();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Inventory Agent Service başlatılıyor...");
            _logger.LogInformation($"API Base URL: {_apiSettings.BaseUrl}");

            try
            {
                // Initialize offline storage if enabled
                if (_apiSettings.EnableOfflineStorage)
                {
                    _offlineStorage = new OfflineStorageService(_apiSettings.OfflineStoragePath, _apiSettings.MaxOfflineRecords);
                    var offlineCount = await _offlineStorage.GetStoredRecordCountAsync();
                    _logger.LogInformation($"Offline Storage Enabled. Pending records: {offlineCount}");
                    
                    // Start connectivity monitoring
                    _connectivityMonitor = new ConnectivityMonitorService(_apiSettings, _offlineStorage);
                    _connectivityMonitor.StartMonitoring();
                }

                // İlk envanteri hemen al
                _logger.LogInformation("Initial inventory scan başlatılıyor...");
                await RunInventoryAsync(_offlineStorage);

                // Periyodik döngü - Timer yerine Task.Delay kullan
                _logger.LogInformation($"Periyodik envanter taraması {_inventoryIntervalMinutes} dakikada bir çalışacak.");

                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        // Bir sonraki tarama için bekle
                        await Task.Delay(TimeSpan.FromMinutes(_inventoryIntervalMinutes), stoppingToken);
                        
                        if (!stoppingToken.IsCancellationRequested)
                        {
                            _logger.LogInformation("Periyodik envanter taraması başlatılıyor...");
                            await RunInventoryAsync(_offlineStorage);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        // Normal shutdown, break the loop
                        break;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Periyodik envanter taraması sırasında hata oluştu. Bir sonraki döngüde devam edilecek.");
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Inventory Agent Service durduruldu.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Inventory Agent Service'de kritik hata oluştu.");
                throw;
            }
            finally
            {
                // Cleanup
                _connectivityMonitor?.Stop();
                _logger.LogInformation("Inventory Agent Service temizlendi.");
            }
        }

        private async Task RunInventoryAsync(OfflineStorageService? offlineStorage)
        {
            try
            {
                _logger.LogInformation("Sistem envanteri toplanıyor...");
                
                // Çapraz platform sistem bilgilerini topla
                var device = CrossPlatformSystemInfo.GatherSystemInformation();

                // DeviceLogger kullanma (konsol çıktısı service için uygun değil)
                _logger.LogInformation($"Device: {device.Name}, IP: {device.IpAddress}, MAC: {device.MacAddress}");

                // API'ye gönder
                string apiUrl = _apiSettings.GetDeviceEndpoint();
                bool success = await ApiClient.PostDeviceAsync(device, apiUrl, offlineStorage);
                
                if (success)
                {
                    _logger.LogInformation("Cihaz bilgileri başarıyla API'ye gönderildi.");
                }
                else if (offlineStorage != null)
                {
                    _logger.LogWarning("API'ye gönderim başarısız, veri offline olarak saklandı.");
                }
                else
                {
                    _logger.LogError("API'ye gönderim başarısız.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Envanter toplama sırasında hata oluştu.");
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Inventory Agent Service durduruluyor...");
            
            _connectivityMonitor?.Stop();
            
            await base.StopAsync(cancellationToken);
            _logger.LogInformation("Inventory Agent Service durduruldu.");
        }
    }
}