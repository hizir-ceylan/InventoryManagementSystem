using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Inventory.Agent.Windows.Configuration;
using Inventory.Agent.Windows.Services;
using Inventory.Agent.Windows.Models;
using System;
using System.Net.Http;
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
        private readonly int _initialDelaySeconds = 15; // Service startup'tan sonra ilk tarama için bekleme süresi (azaltıldı)
        private readonly int _apiCheckIntervalSeconds = 3; // API hazır olma kontrolü aralığı (azaltıldı)
        private readonly int _maxApiCheckAttempts = 5; // Maksimum API kontrol deneme sayısı (15 saniye toplam)
        private OfflineStorageService? _offlineStorage;
        private readonly CancellationTokenSource _startupCancellation = new();

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
                // SCM'e service'in başladığını hızla bildirmek için minimum işlem
                _logger.LogInformation("Service Windows SCM'e hazır sinyali gönderiyor...");
                
                // Service'in Windows SCM tarafından "başlatıldı" olarak algılanması için
                // ExecuteAsync metodunun hızla başlaması gerekiyor
                
                // Combine cancellation tokens for proper shutdown
                using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken, _startupCancellation.Token);
                var cancellationToken = combinedCts.Token;
                
                _logger.LogInformation("Service başarıyla başlatıldı. Arka plan işlemleri başlatılıyor...");

                // Initialize offline storage first (lightweight operation)
                await InitializeOfflineStorageAsync(cancellationToken);

                // Start background operations without blocking service startup
                var backgroundTask = StartBackgroundOperationsAsync(cancellationToken);

                // Start the main service loop immediately
                var serviceTask = RunServiceLoopAsync(cancellationToken);

                // Wait for either task to complete or cancellation
                await Task.WhenAny(backgroundTask, serviceTask);
                
                if (cancellationToken.IsCancellationRequested)
                {
                    _logger.LogInformation("Service shutdown requested.");
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Inventory Agent Service durduruldu.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Inventory Agent Service'de kritik hata oluştu.");
                // Service'de kritik hata olursa, service'i yeniden başlatmaya çalışacak
                throw;
            }
            finally
            {
                // Cleanup
                await CleanupAsync();
                _logger.LogInformation("Inventory Agent Service temizlendi.");
            }
        }

        private async Task InitializeOfflineStorageAsync(CancellationToken cancellationToken)
        {
            if (_apiSettings.EnableOfflineStorage)
            {
                try
                {
                    _offlineStorage = new OfflineStorageService(_apiSettings.OfflineStoragePath, _apiSettings.MaxOfflineRecords);
                    _logger.LogInformation("Offline Storage başlatıldı.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Offline Storage başlatılırken hata oluştu. Devam edilecek.");
                }
            }
        }

        private async Task StartBackgroundOperationsAsync(CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation($"İlk envanter taraması {_initialDelaySeconds} saniye sonra başlayacak.");

                // İlk tarama için kısa bekleme
                await Task.Delay(TimeSpan.FromSeconds(_initialDelaySeconds), cancellationToken);
                
                if (cancellationToken.IsCancellationRequested)
                    return;

                // API hazır olma kontrolü - daha kısa süre
                bool apiReady = await WaitForApiReadyAsync(cancellationToken);
                
                if (!apiReady)
                {
                    _logger.LogWarning("API henüz hazır değil, offline modda devam edilecek.");
                }

                // Connectivity monitoring'i başlat (eğer offline storage varsa)
                await StartConnectivityMonitoringAsync();

                // İlk envanteri al
                _logger.LogInformation("İlk envanter taraması başlatılıyor...");
                await RunInventoryAsync(_offlineStorage);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Background operations cancelled.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Background operations başlatılırken hata oluştu.");
            }
        }

        private async Task RunServiceLoopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Periyodik envanter taraması {_inventoryIntervalMinutes} dakikada bir çalışacak.");

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    // Bir sonraki tarama için bekle
                    await Task.Delay(TimeSpan.FromMinutes(_inventoryIntervalMinutes), cancellationToken);
                    
                    if (!cancellationToken.IsCancellationRequested)
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
                    
                    // Hata sonrası kısa bekleme
                    try
                    {
                        await Task.Delay(TimeSpan.FromMinutes(1), cancellationToken);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                }
            }
        }

        private async Task StartConnectivityMonitoringAsync()
        {
            if (_apiSettings.EnableOfflineStorage && _offlineStorage != null)
            {
                try
                {
                    var offlineCount = await _offlineStorage.GetStoredRecordCountAsync();
                    _logger.LogInformation($"Offline records pending: {offlineCount}");
                    
                    _connectivityMonitor = new ConnectivityMonitorService(_apiSettings, _offlineStorage);
                    _connectivityMonitor.StartMonitoring();
                    _logger.LogInformation("Connectivity monitoring başlatıldı.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Connectivity monitoring başlatılırken hata oluştu.");
                }
            }
        }

        private async Task<bool> WaitForApiReadyAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("API hazır olma durumu kontrol ediliyor...");
            
            for (int attempt = 1; attempt <= _maxApiCheckAttempts && !cancellationToken.IsCancellationRequested; attempt++)
            {
                try
                {
                    // API health check endpoint'ini kontrol et
                    using var httpClient = new HttpClient();
                    httpClient.Timeout = TimeSpan.FromSeconds(3); // Daha kısa timeout
                    
                    var response = await httpClient.GetAsync($"{_apiSettings.BaseUrl}/api/device", cancellationToken);
                    
                    if (response.IsSuccessStatusCode)
                    {
                        _logger.LogInformation($"API hazır! (Deneme {attempt}/{_maxApiCheckAttempts})");
                        return true;
                    }
                    
                    _logger.LogWarning($"API henüz hazır değil (HTTP {response.StatusCode}). Deneme {attempt}/{_maxApiCheckAttempts}");
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogDebug($"API kontrolü başarısız (Deneme {attempt}/{_maxApiCheckAttempts}): {ex.Message}");
                }

                if (attempt < _maxApiCheckAttempts && !cancellationToken.IsCancellationRequested)
                {
                    await Task.Delay(TimeSpan.FromSeconds(_apiCheckIntervalSeconds), cancellationToken);
                }
            }

            _logger.LogWarning($"API {_maxApiCheckAttempts} deneme sonrası hala hazır değil. Offline modda devam edilecek.");
            return false;
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

        private async Task CleanupAsync()
        {
            try
            {
                _connectivityMonitor?.Stop();
                _startupCancellation?.Cancel();
                _startupCancellation?.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Cleanup sırasında hata oluştu.");
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Inventory Agent Service durduruluyor...");
            
            // Cancel startup operations
            _startupCancellation?.Cancel();
            
            await CleanupAsync();
            
            await base.StopAsync(cancellationToken);
            _logger.LogInformation("Inventory Agent Service durduruldu.");
        }
    }
}