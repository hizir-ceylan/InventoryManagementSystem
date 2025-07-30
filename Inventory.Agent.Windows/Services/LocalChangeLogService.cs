using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Inventory.Agent.Windows.Models;
using Inventory.Agent.Windows.Configuration;

namespace Inventory.Agent.Windows.Services
{
    /// <summary>
    /// Service for managing local change log files on the computer
    /// </summary>
    public class LocalChangeLogService
    {
        private readonly string _logDirectory;
        private readonly string _changeLogDirectory;

        public LocalChangeLogService(ApiSettings apiSettings)
        {
            // Get the base log path from ApiSettings
            var basePath = ApiSettings.GetDefaultLogPath();
            _logDirectory = Path.GetDirectoryName(basePath) ?? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            
            // Create a specific directory for change logs
            _changeLogDirectory = Path.Combine(_logDirectory, "ChangeLogs");
            
            // Ensure the directory exists
            Directory.CreateDirectory(_changeLogDirectory);
        }

        /// <summary>
        /// Save hardware change logs to a local file
        /// </summary>
        /// <param name="changes">List of change logs to save</param>
        /// <param name="deviceName">Name of the device for file naming</param>
        /// <returns>The full path to the saved log file</returns>
        public async Task<string> SaveChangeLogsAsync(List<ChangeLogDto> changes, string deviceName = "Device")
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            var sanitizedDeviceName = SanitizeFileName(deviceName);
            var fileName = $"{sanitizedDeviceName}_changes_{timestamp}.json";
            var filePath = Path.Combine(_changeLogDirectory, fileName);

            var logEntry = new
            {
                Timestamp = DateTime.UtcNow,
                DeviceName = deviceName,
                ChangeCount = changes.Count,
                Changes = changes
            };

            var json = JsonSerializer.Serialize(logEntry, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            await File.WriteAllTextAsync(filePath, json);
            return filePath;
        }

        /// <summary>
        /// Save a single hardware change to local log
        /// </summary>
        /// <param name="changeType">Type of change (e.g., "GPU Removed", "RAM Added")</param>
        /// <param name="oldValue">Previous value</param>
        /// <param name="newValue">New value</param>
        /// <param name="deviceName">Device name</param>
        /// <returns>The full path to the saved log file</returns>
        public async Task<string> LogHardwareChangeAsync(string changeType, string oldValue, string newValue, string deviceName = "Device")
        {
            var change = new ChangeLogDto
            {
                Id = Guid.NewGuid(),
                ChangeDate = DateTime.UtcNow,
                ChangeType = changeType,
                OldValue = oldValue,
                NewValue = newValue,
                ChangedBy = "Agent"
            };

            return await SaveChangeLogsAsync(new List<ChangeLogDto> { change }, deviceName);
        }

        /// <summary>
        /// Get all change log files from local storage
        /// </summary>
        /// <returns>List of file paths</returns>
        public List<string> GetChangeLogFiles()
        {
            if (!Directory.Exists(_changeLogDirectory))
                return new List<string>();

            return new List<string>(Directory.GetFiles(_changeLogDirectory, "*.json"));
        }

        /// <summary>
        /// Clean up old change log files
        /// </summary>
        /// <param name="retentionDays">Number of days to keep files</param>
        /// <returns>Number of files deleted</returns>
        public async Task<int> CleanupOldLogsAsync(int retentionDays = 30)
        {
            var deletedCount = 0;
            
            if (!Directory.Exists(_changeLogDirectory))
                return deletedCount;

            var cutoffDate = DateTime.Now.AddDays(-retentionDays);
            var files = Directory.GetFiles(_changeLogDirectory, "*.json");

            foreach (var file in files)
            {
                try
                {
                    var fileInfo = new FileInfo(file);
                    if (fileInfo.CreationTime < cutoffDate)
                    {
                        File.Delete(file);
                        deletedCount++;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error deleting old log file {file}: {ex.Message}");
                }
            }

            return deletedCount;
        }

        /// <summary>
        /// Get the directory path where change logs are stored
        /// </summary>
        /// <returns>Full path to change log directory</returns>
        public string GetChangeLogDirectory()
        {
            return _changeLogDirectory;
        }

        /// <summary>
        /// Sanitize a file name by removing invalid characters
        /// </summary>
        /// <param name="fileName">Input file name</param>
        /// <returns>Sanitized file name</returns>
        private static string SanitizeFileName(string fileName)
        {
            var invalidChars = Path.GetInvalidFileNameChars();
            var sanitized = fileName;
            
            foreach (var invalidChar in invalidChars)
            {
                sanitized = sanitized.Replace(invalidChar, '_');
            }
            
            return sanitized;
        }
    }
}