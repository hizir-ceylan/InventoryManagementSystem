using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Inventory.Agent.Windows.Configuration;
using Inventory.Agent.Windows.Services;

namespace Inventory.Agent.Windows.Services
{
    public class ConnectivityMonitorService
    {
        private readonly ApiSettings _apiSettings;
        private readonly OfflineStorageService _offlineStorage;
        private Timer? _monitorTimer;
        private bool _isOnline = false;
        private readonly object _lockObject = new object();

        public ConnectivityMonitorService(ApiSettings apiSettings, OfflineStorageService offlineStorage)
        {
            _apiSettings = apiSettings;
            _offlineStorage = offlineStorage;
        }

        public void StartMonitoring()
        {
            // Bağlantıyı hemen kontrol et
            _ = Task.Run(CheckConnectivityAndUploadAsync);

            // Periyodik izlemeyi başlat
            _monitorTimer = new Timer(async _ => await CheckConnectivityAndUploadAsync(), 
                null, 
                TimeSpan.FromSeconds(_apiSettings.BatchUploadInterval), 
                TimeSpan.FromSeconds(_apiSettings.BatchUploadInterval));

            Console.WriteLine($"Connectivity monitoring started. Checking every {_apiSettings.BatchUploadInterval} seconds.");
        }

        public void Stop()
        {
            _monitorTimer?.Dispose();
            Console.WriteLine("Connectivity monitoring stopped.");
        }

        public bool IsOnline()
        {
            lock (_lockObject)
            {
                return _isOnline;
            }
        }

        private async Task CheckConnectivityAndUploadAsync()
        {
            try
            {
                bool currentlyOnline = await TestConnectivityAsync();
                
                lock (_lockObject)
                {
                    bool wasOffline = !_isOnline;
                    _isOnline = currentlyOnline;
                    
                    if (currentlyOnline && wasOffline)
                    {
                        Console.WriteLine("Connection restored! Starting batch upload of offline data...");
                    }
                    else if (!currentlyOnline && !wasOffline)
                    {
                        Console.WriteLine("Connection lost. Future data will be stored offline.");
                    }
                }

                if (currentlyOnline)
                {
                    await UploadOfflineDataAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in connectivity monitoring: {ex.Message}");
                lock (_lockObject)
                {
                    _isOnline = false;
                }
            }
        }

        private async Task<bool> TestConnectivityAsync()
        {
            try
            {
                using var client = new HttpClient();
                client.Timeout = TimeSpan.FromSeconds(10);
                
                // Try to reach the API health endpoint or device endpoint
                var response = await client.GetAsync(_apiSettings.GetDeviceEndpoint());
                return response.IsSuccessStatusCode || 
                       response.StatusCode == System.Net.HttpStatusCode.MethodNotAllowed; // GET on POST endpoint
            }
            catch
            {
                return false;
            }
        }

        private async Task UploadOfflineDataAsync()
        {
            try
            {
                var offlineRecords = await _offlineStorage.GetStoredDeviceDataAsync();
                if (!offlineRecords.Any())
                {
                    return; // No offline data to upload
                }

                Console.WriteLine($"Found {offlineRecords.Count} offline records to upload.");
                var successfulUploads = new List<Guid>();

                foreach (var record in offlineRecords.Take(50)) // Process in batches of 50
                {
                    try
                    {
                        var success = await ApiClient.PostDeviceAsync(record.DeviceData, _apiSettings.GetDeviceEndpoint());
                        
                        if (success)
                        {
                            successfulUploads.Add(record.Id);
                            Console.WriteLine($"Successfully uploaded offline record: {record.DeviceData.Name}");
                        }
                        else
                        {
                            await _offlineStorage.IncrementAttemptCountAsync(record.Id);
                            Console.WriteLine($"Failed to upload offline record: {record.DeviceData.Name}");
                        }

                        // Small delay between uploads to avoid overwhelming the server
                        await Task.Delay(100);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error uploading offline record {record.DeviceData.Name}: {ex.Message}");
                        await _offlineStorage.IncrementAttemptCountAsync(record.Id);
                    }
                }

                // Remove successfully uploaded records
                if (successfulUploads.Any())
                {
                    await _offlineStorage.RemoveStoredDeviceDataAsync(successfulUploads);
                    Console.WriteLine($"Successfully uploaded and removed {successfulUploads.Count} offline records.");
                }

                // Clean up old failed records (older than 7 days or more than 10 attempts)
                await CleanupOldRecordsAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error uploading offline data: {ex.Message}");
            }
        }

        private async Task CleanupOldRecordsAsync()
        {
            try
            {
                var allRecords = await _offlineStorage.GetStoredDeviceDataAsync();
                var cutoffDate = DateTime.UtcNow.AddDays(-7);
                var recordsToRemove = allRecords
                    .Where(r => r.CreatedAt < cutoffDate || r.Attempts > 10)
                    .Select(r => r.Id)
                    .ToList();

                if (recordsToRemove.Any())
                {
                    await _offlineStorage.RemoveStoredDeviceDataAsync(recordsToRemove);
                    Console.WriteLine($"Cleaned up {recordsToRemove.Count} old offline records.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error cleaning up old records: {ex.Message}");
            }
        }
    }
}