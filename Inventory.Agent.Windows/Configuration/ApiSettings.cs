using System;
using System.IO;

namespace Inventory.Agent.Windows.Configuration
{
    public class ApiSettings
    {
        public string BaseUrl { get; set; } = "http://localhost:5093";
        public int Timeout { get; set; } = 30;
        public int RetryCount { get; set; } = 3;
        public bool EnableOfflineStorage { get; set; } = true;
        public string OfflineStoragePath { get; set; } = GetDefaultOfflineStoragePath();
        public int BatchUploadInterval { get; set; } = 300; // 5 dakika (saniye cinsinden)
        public int MaxOfflineRecords { get; set; } = 10000;
        
        private static string GetDefaultOfflineStoragePath()
        {
            try
            {
                // Kullanıcının Belgeler klasörünü al
                string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                
                // Eğer Belgeler klasörü mevcut değilse veya boşsa, kullanıcının ana dizinini kullan
                if (string.IsNullOrEmpty(documentsPath) || !Directory.Exists(documentsPath))
                {
                    string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                    if (!string.IsNullOrEmpty(userProfile))
                    {
                        documentsPath = Path.Combine(userProfile, "Documents");
                        // Belgeler klasörünü oluştur
                        Directory.CreateDirectory(documentsPath);
                    }
                    else
                    {
                        // Son çare olarak ev dizinini kullan
                        documentsPath = Environment.GetEnvironmentVariable("HOME") ?? Path.GetTempPath();
                    }
                }
                
                // Uygulama için özel klasör oluştur
                string appDataPath = Path.Combine(documentsPath, "InventoryManagementSystem", "OfflineStorage");
                
                return appDataPath;
            }
            catch
            {
                // Eğer hiçbir yol çalışmazsa, geçici dizini kullan
                return Path.Combine(Path.GetTempPath(), "InventoryManagementSystem", "OfflineStorage");
            }
        }
        
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