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
            // Try multiple persistent storage locations in order of preference
            var persistentPaths = new[]
            {
                // 1. User's Documents folder (most preferred)
                () => {
                    var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                    if (!string.IsNullOrEmpty(documentsPath) && Directory.Exists(documentsPath))
                        return Path.Combine(documentsPath, "InventoryManagementSystem", "OfflineStorage");
                    return null;
                },
                
                // 2. User Profile with Documents subfolder
                () => {
                    var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                    if (!string.IsNullOrEmpty(userProfile))
                    {
                        var documentsPath = Path.Combine(userProfile, "Documents");
                        return Path.Combine(documentsPath, "InventoryManagementSystem", "OfflineStorage");
                    }
                    return null;
                },
                
                // 3. Application Data folder (Windows)
                () => {
                    var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                    if (!string.IsNullOrEmpty(appData))
                        return Path.Combine(appData, "InventoryManagementSystem", "OfflineStorage");
                    return null;
                },
                
                // 4. Common Application Data folder (Windows, system-wide)
                () => {
                    var commonAppData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
                    if (!string.IsNullOrEmpty(commonAppData))
                        return Path.Combine(commonAppData, "InventoryManagementSystem", "OfflineStorage");
                    return null;
                },
                
                // 5. Home directory (Linux/Unix)
                () => {
                    var home = Environment.GetEnvironmentVariable("HOME");
                    if (!string.IsNullOrEmpty(home))
                        return Path.Combine(home, ".local", "share", "InventoryManagementSystem", "OfflineStorage");
                    return null;
                },
                
                // 6. /var/lib system directory (Linux, requires permissions)
                () => {
                    if (Directory.Exists("/var/lib"))
                        return Path.Combine("/var", "lib", "InventoryManagementSystem", "OfflineStorage");
                    return null;
                }
            };

            foreach (var pathProvider in persistentPaths)
            {
                try
                {
                    var path = pathProvider();
                    if (path != null && ValidateAndCreatePersistentPath(path))
                    {
                        LogStorageLocation("OfflineStorage", path);
                        return path;
                    }
                }
                catch (Exception ex)
                {
                    // Log the error but continue trying other paths
                    Console.WriteLine($"Failed to create storage path: {ex.Message}");
                }
            }

            // If all persistent paths fail, use a temp path as last resort but warn user
            var tempPath = Path.Combine(Path.GetTempPath(), "InventoryManagementSystem", "OfflineStorage");
            Console.WriteLine("WARNING: Could not create persistent storage directory. Using temporary directory - data will be lost on restart!");
            Console.WriteLine($"Temporary storage location: {tempPath}");
            return tempPath;
        }

        public static string GetDefaultLogPath()
        {
            // Try multiple persistent storage locations in order of preference
            var persistentPaths = new[]
            {
                // 1. User's Documents folder (most preferred)
                () => {
                    var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                    if (!string.IsNullOrEmpty(documentsPath) && Directory.Exists(documentsPath))
                        return Path.Combine(documentsPath, "InventoryManagementSystem", "LocalLogs");
                    return null;
                },
                
                // 2. User Profile with Documents subfolder
                () => {
                    var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                    if (!string.IsNullOrEmpty(userProfile))
                    {
                        var documentsPath = Path.Combine(userProfile, "Documents");
                        return Path.Combine(documentsPath, "InventoryManagementSystem", "LocalLogs");
                    }
                    return null;
                },
                
                // 3. Application Data folder (Windows)
                () => {
                    var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                    if (!string.IsNullOrEmpty(appData))
                        return Path.Combine(appData, "InventoryManagementSystem", "LocalLogs");
                    return null;
                },
                
                // 4. Common Application Data folder (Windows, system-wide)
                () => {
                    var commonAppData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
                    if (!string.IsNullOrEmpty(commonAppData))
                        return Path.Combine(commonAppData, "InventoryManagementSystem", "LocalLogs");
                    return null;
                },
                
                // 5. Home directory (Linux/Unix)
                () => {
                    var home = Environment.GetEnvironmentVariable("HOME");
                    if (!string.IsNullOrEmpty(home))
                        return Path.Combine(home, ".local", "share", "InventoryManagementSystem", "LocalLogs");
                    return null;
                },
                
                // 6. /var/log system directory (Linux, requires permissions)
                () => {
                    if (Directory.Exists("/var/log"))
                        return Path.Combine("/var", "log", "InventoryManagementSystem");
                    return null;
                }
            };

            foreach (var pathProvider in persistentPaths)
            {
                try
                {
                    var path = pathProvider();
                    if (path != null && ValidateAndCreatePersistentPath(path))
                    {
                        LogStorageLocation("LocalLogs", path);
                        return path;
                    }
                }
                catch (Exception ex)
                {
                    // Log the error but continue trying other paths
                    Console.WriteLine($"Failed to create log path: {ex.Message}");
                }
            }

            // If all persistent paths fail, use a temp path as last resort but warn user
            var tempPath = Path.Combine(Path.GetTempPath(), "InventoryManagementSystem", "LocalLogs");
            Console.WriteLine("WARNING: Could not create persistent log directory. Using temporary directory - logs will be lost on restart!");
            Console.WriteLine($"Temporary log location: {tempPath}");
            return tempPath;
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
        
        /// <summary>
        /// Validates that a path is persistent (not in temp directory) and creates it if possible
        /// </summary>
        private static bool ValidateAndCreatePersistentPath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return false;

            try
            {
                // Check if path is in temp directory - this indicates non-persistent storage
                var tempPath = Path.GetTempPath();
                var fullPath = Path.GetFullPath(path);
                var fullTempPath = Path.GetFullPath(tempPath);
                
                if (fullPath.StartsWith(fullTempPath, StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine($"Skipping temp directory path: {path}");
                    return false;
                }

                // Try to create the directory
                Directory.CreateDirectory(path);
                
                // Test if we can write to the directory
                var testFile = Path.Combine(path, "test_write_access.tmp");
                File.WriteAllText(testFile, "test");
                File.Delete(testFile);
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Path validation failed for {path}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Logs the storage location being used to console for user awareness
        /// </summary>
        private static void LogStorageLocation(string storageType, string path)
        {
            Console.WriteLine($"✓ {storageType} location: {path}");
        }
    }
}