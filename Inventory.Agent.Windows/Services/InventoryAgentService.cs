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
        private readonly int _initialDelaySeconds = 60; // Service startup'tan sonra ilk tarama için bekleme süresi
        private readonly int _apiCheckIntervalSeconds = 10; // API hazır olma kontrolü aralığı
        private readonly int _maxApiCheckAttempts = 30; // Maksimum API kontrol deneme sayısı (5 dakika)
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
                // Hızlı service başlatma - ağır işlemleri defer et
                _logger.LogInformation("Service Windows SCM'e hazır olduğunu bildiriyor...");
                
                // Service'in Windows tarafından başlatıldığını bildirmek için kısa bir delay
                await Task.Delay(1000, stoppingToken);
                
                // Initialize offline storage if enabled (hafif işlem)
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

                _logger.LogInformation($"Service başarıyla başlatıldı. İlk envanter taraması {_initialDelaySeconds} saniye sonra başlayacak.");

                // API hazır olma kontrolü ve başlangıç gecikmesi
                await WaitForApiAndStartOperationsAsync(stoppingToken);

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

        private async Task WaitForApiAndStartOperationsAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation($"API hazır olma kontrolü başlatılıyor... ({_initialDelaySeconds} saniye bekleme)");
            
            // İlk startup gecikmesi - Windows Service'in hızlı başlaması için
            await Task.Delay(TimeSpan.FromSeconds(_initialDelaySeconds), stoppingToken);
            
            if (stoppingToken.IsCancellationRequested)
                return;

            // API hazır olma kontrolü
            bool apiReady = await WaitForApiReadyAsync(stoppingToken);
            
            if (!apiReady)
            {
                _logger.LogWarning("API hazır değil, ancak offline storage varsa devam edilecek.");
            }

            // Connectivity monitoring'i başlat (eğer offline storage varsa)
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

            // İlk envanteri al
            _logger.LogInformation("İlk envanter taraması başlatılıyor...");
            await RunInventoryAsync(_offlineStorage);
        }

        private async Task<bool> WaitForApiReadyAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("API hazır olma durumu kontrol ediliyor...");
            
            for (int attempt = 1; attempt <= _maxApiCheckAttempts && !stoppingToken.IsCancellationRequested; attempt++)
            {
                try
                {
                    // API health check endpoint'ini kontrol et
                    using var httpClient = new HttpClient();
                    httpClient.Timeout = TimeSpan.FromSeconds(5);
                    
                    var response = await httpClient.GetAsync($"{_apiSettings.BaseUrl}/api/device", stoppingToken);
                    
                    if (response.IsSuccessStatusCode)
                    {
                        _logger.LogInformation($"API hazır! (Deneme {attempt}/{_maxApiCheckAttempts})");
                        return true;
                    }
                    
                    _logger.LogWarning($"API henüz hazır değil (HTTP {response.StatusCode}). Deneme {attempt}/{_maxApiCheckAttempts}");
                }
                catch (Exception ex)
                {
                    _logger.LogDebug($"API kontrolü başarısız (Deneme {attempt}/{_maxApiCheckAttempts}): {ex.Message}");
                }

                if (attempt < _maxApiCheckAttempts && !stoppingToken.IsCancellationRequested)
                {
                    await Task.Delay(TimeSpan.FromSeconds(_apiCheckIntervalSeconds), stoppingToken);
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

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Inventory Agent Service durduruluyor...");
            
            _connectivityMonitor?.Stop();
            
            await base.StopAsync(cancellationToken);
            _logger.LogInformation("Inventory Agent Service durduruldu.");
        }
    }
}