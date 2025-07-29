using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Inventory.Agent.Windows.Models;

namespace Inventory.Agent.Windows.Services
{
    public class OfflineStorageService
    {
        private readonly string _storageDirectory;
        private readonly int _maxRecords;
        private readonly string _deviceDataFile;

        public OfflineStorageService(string storageDirectory, int maxRecords = 10000)
        {
            _storageDirectory = storageDirectory;
            _maxRecords = maxRecords;
            _deviceDataFile = Path.Combine(storageDirectory, "offline_devices.json");
            
            // Depolama dizininin var olduğundan emin ol ve kalıcı olduğunu doğrula
            if (!ValidateStorageDirectory(storageDirectory))
            {
                throw new InvalidOperationException($"Storage directory is not persistent or accessible: {storageDirectory}");
            }
            
            Directory.CreateDirectory(storageDirectory);
            
            // Kullanıcıyı depolama konumu hakkında bilgilendir
            Console.WriteLine($"Offline storage initialized: {storageDirectory}");
        }

        /// <summary>
        /// Validates that the storage directory is persistent and writable
        /// </summary>
        private bool ValidateStorageDirectory(string directory)
        {
            try
            {
                // Check if directory is in temp path (non-persistent)
                var tempPath = Path.GetTempPath();
                var fullDirectory = Path.GetFullPath(directory);
                var fullTempPath = Path.GetFullPath(tempPath);
                
                if (fullDirectory.StartsWith(fullTempPath, StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine($"WARNING: Offline storage is in temporary directory and will be lost on restart: {directory}");
                    return false; // Still allow but warn user
                }

                // Test write access
                Directory.CreateDirectory(directory);
                var testFile = Path.Combine(directory, "test_write.tmp");
                File.WriteAllText(testFile, "test");
                File.Delete(testFile);
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Storage directory validation failed: {ex.Message}");
                return false;
            }
        }

        public async Task StoreDeviceDataAsync(DeviceDto device)
        {
            try
            {
                var offlineRecord = new OfflineDeviceRecord
                {
                    Id = Guid.NewGuid(),
                    DeviceData = device,
                    CreatedAt = DateTime.UtcNow,
                    Attempts = 0
                };

                var existingRecords = await GetStoredDeviceDataAsync();
                existingRecords.Add(offlineRecord);

                // Depolanan kayıt sayısını sınırla
                if (existingRecords.Count > _maxRecords)
                {
                    existingRecords = existingRecords.GetRange(existingRecords.Count - _maxRecords, _maxRecords);
                }

                await SaveStoredDeviceDataAsync(existingRecords);
                Console.WriteLine($"Device data stored offline: {device.Name}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error storing device data offline: {ex.Message}");
            }
        }

        public async Task<List<OfflineDeviceRecord>> GetStoredDeviceDataAsync()
        {
            try
            {
                if (!File.Exists(_deviceDataFile))
                {
                    return new List<OfflineDeviceRecord>();
                }

                var json = await File.ReadAllTextAsync(_deviceDataFile);
                if (string.IsNullOrEmpty(json))
                {
                    return new List<OfflineDeviceRecord>();
                }

                var records = JsonSerializer.Deserialize<List<OfflineDeviceRecord>>(json);
                return records ?? new List<OfflineDeviceRecord>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading offline device data: {ex.Message}");
                return new List<OfflineDeviceRecord>();
            }
        }

        public async Task RemoveStoredDeviceDataAsync(List<Guid> recordIds)
        {
            try
            {
                var existingRecords = await GetStoredDeviceDataAsync();
                existingRecords.RemoveAll(r => recordIds.Contains(r.Id));
                await SaveStoredDeviceDataAsync(existingRecords);
                Console.WriteLine($"Removed {recordIds.Count} offline records");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error removing offline device data: {ex.Message}");
            }
        }

        public async Task IncrementAttemptCountAsync(Guid recordId)
        {
            try
            {
                var existingRecords = await GetStoredDeviceDataAsync();
                var record = existingRecords.Find(r => r.Id == recordId);
                if (record != null)
                {
                    record.Attempts++;
                    record.LastAttemptAt = DateTime.UtcNow;
                    await SaveStoredDeviceDataAsync(existingRecords);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error incrementing attempt count: {ex.Message}");
            }
        }

        public async Task<int> GetStoredRecordCountAsync()
        {
            var records = await GetStoredDeviceDataAsync();
            return records.Count;
        }

        private async Task SaveStoredDeviceDataAsync(List<OfflineDeviceRecord> records)
        {
            var json = JsonSerializer.Serialize(records, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            await File.WriteAllTextAsync(_deviceDataFile, json);
        }
    }

    public class OfflineDeviceRecord
    {
        public Guid Id { get; set; }
        public DeviceDto DeviceData { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public DateTime? LastAttemptAt { get; set; }
        public int Attempts { get; set; }
    }
}