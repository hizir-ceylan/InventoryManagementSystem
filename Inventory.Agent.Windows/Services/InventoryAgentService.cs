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
        private Timer? _inventoryTimer;
        private readonly int _inventoryIntervalMinutes = 60; // Varsayılan olarak her saat

        public InventoryAgentService(ILogger<InventoryAgentService> logger)
        {
            _logger = logger;
            _apiSettings = ApiSettings.LoadFromEnvironment();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Inventory Agent Service başlatılıyor...");
            _logger.LogInformation($"API Base URL: {_apiSettings.BaseUrl}");

            OfflineStorageService? offlineStorage = null;

            try
            {
                // Initialize offline storage if enabled
                if (_apiSettings.EnableOfflineStorage)
                {
                    offlineStorage = new OfflineStorageService(_apiSettings.OfflineStoragePath, _apiSettings.MaxOfflineRecords);
                    var offlineCount = await offlineStorage.GetStoredRecordCountAsync();
                    _logger.LogInformation($"Offline Storage Enabled. Pending records: {offlineCount}");
                    
                    // Start connectivity monitoring
                    _connectivityMonitor = new ConnectivityMonitorService(_apiSettings, offlineStorage);
                    _connectivityMonitor.StartMonitoring();
                }

                // İlk envanteri hemen al
                _logger.LogInformation("Initial inventory scan başlatılıyor...");
                await RunInventoryAsync(offlineStorage);

                // Periyodik envanter almayı başlat
                _inventoryTimer = new Timer(async _ => await RunInventoryAsync(offlineStorage), 
                    null, 
                    TimeSpan.FromMinutes(_inventoryIntervalMinutes), 
                    TimeSpan.FromMinutes(_inventoryIntervalMinutes));

                _logger.LogInformation($"Periyodik envanter taraması {_inventoryIntervalMinutes} dakikada bir çalışacak.");

                // Service sonlanana kadar bekle
                while (!stoppingToken.IsCancellationRequested)
                {
                    await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Inventory Agent Service durduruldu.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Inventory Agent Service'de hata oluştu.");
                throw;
            }
            finally
            {
                // Cleanup
                _inventoryTimer?.Dispose();
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
            
            _inventoryTimer?.Dispose();
            _connectivityMonitor?.Stop();
            
            await base.StopAsync(cancellationToken);
            _logger.LogInformation("Inventory Agent Service durduruldu.");
        }
    }
}