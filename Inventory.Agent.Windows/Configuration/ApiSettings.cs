using System;

namespace Inventory.Agent.Windows.Configuration
{
    public class ApiSettings
    {
        public string BaseUrl { get; set; } = "http://localhost:5000";
        public int Timeout { get; set; } = 30;
        public int RetryCount { get; set; } = 3;
        public bool EnableOfflineStorage { get; set; } = true;
        public string OfflineStoragePath { get; set; } = "Data/OfflineStorage";
        public int BatchUploadInterval { get; set; } = 300; // 5 dakika (saniye cinsinden)
        public int MaxOfflineRecords { get; set; } = 10000;
        
        public static ApiSettings LoadFromEnvironment()
        {
            var settings = new ApiSettings();
            
            // Çevre değişkenlerinden yükle, yoksa varsayılanları kullan
            var baseUrl = Environment.GetEnvironmentVariable("ApiSettings__BaseUrl");
            if (!string.IsNullOrEmpty(baseUrl))
            {
                settings.BaseUrl = baseUrl;
            }
            
            var timeout = Environment.GetEnvironmentVariable("ApiSettings__Timeout");
            if (!string.IsNullOrEmpty(timeout) && int.TryParse(timeout, out int timeoutValue))
            {
                settings.Timeout = timeoutValue;
            }
            
            var retryCount = Environment.GetEnvironmentVariable("ApiSettings__RetryCount");
            if (!string.IsNullOrEmpty(retryCount) && int.TryParse(retryCount, out int retryValue))
            {
                settings.RetryCount = retryValue;
            }

            var enableOfflineStorage = Environment.GetEnvironmentVariable("ApiSettings__EnableOfflineStorage");
            if (!string.IsNullOrEmpty(enableOfflineStorage) && bool.TryParse(enableOfflineStorage, out bool offlineValue))
            {
                settings.EnableOfflineStorage = offlineValue;
            }

            var offlineStoragePath = Environment.GetEnvironmentVariable("ApiSettings__OfflineStoragePath");
            if (!string.IsNullOrEmpty(offlineStoragePath))
            {
                settings.OfflineStoragePath = offlineStoragePath;
            }

            var batchUploadInterval = Environment.GetEnvironmentVariable("ApiSettings__BatchUploadInterval");
            if (!string.IsNullOrEmpty(batchUploadInterval) && int.TryParse(batchUploadInterval, out int intervalValue))
            {
                settings.BatchUploadInterval = intervalValue;
            }

            var maxOfflineRecords = Environment.GetEnvironmentVariable("ApiSettings__MaxOfflineRecords");
            if (!string.IsNullOrEmpty(maxOfflineRecords) && int.TryParse(maxOfflineRecords, out int maxRecordsValue))
            {
                settings.MaxOfflineRecords = maxRecordsValue;
            }
            
            return settings;
        }
        
        public string GetDeviceEndpoint()
        {
            return $"{BaseUrl.TrimEnd('/')}/api/device";
        }
        
        public string GetNetworkDiscoveryEndpoint()
        {
            return $"{BaseUrl.TrimEnd('/')}/api/devices/network-discovered";
        }

        public string GetBatchUploadEndpoint()
        {
            return $"{BaseUrl.TrimEnd('/')}/api/device/batch";
        }
    }
}