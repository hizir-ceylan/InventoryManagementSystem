using System.Text;
using Newtonsoft.Json;
using Inventory.Agent.Windows.Configuration;

namespace Inventory.Agent.Windows
{
    public class CentralizedLogger
    {
        private readonly string _apiUrl;
        private readonly string _source;
        private readonly HttpClient _httpClient;

        public CentralizedLogger(string apiUrl, string source)
        {
            _apiUrl = apiUrl;
            _source = source;
            _httpClient = new HttpClient();
        }

        public async Task LogAsync(string level, string message, object? data = null)
        {
            try
            {
                var logEntry = new
                {
                    Source = _source,
                    Level = level,
                    Message = message,
                    Data = data
                };

                var json = JsonConvert.SerializeObject(logEntry);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_apiUrl}/api/logging", content);
                
                if (!response.IsSuccessStatusCode)
                {
                    // API mevcut değilse yerel loglamaya geri dön
                    await LogLocallyAsync(level, message, data);
                }
            }
            catch (Exception ex)
            {
                // API çağrısı başarısız olursa yerel loglamaya geri dön
                await LogLocallyAsync(level, $"API logging failed: {ex.Message}. Original: {message}", data);
            }
        }

        public async Task LogErrorAsync(string message, Exception? exception = null)
        {
            var data = exception != null ? new { Exception = exception.ToString() } : null;
            await LogAsync("Error", message, data);
        }

        public async Task LogInfoAsync(string message, object? data = null)
        {
            await LogAsync("Info", message, data);
        }

        public async Task LogWarningAsync(string message, object? data = null)
        {
            await LogAsync("Warning", message, data);
        }

        private async Task LogLocallyAsync(string level, string message, object? data)
        {
            try
            {
                var logFolder = ApiSettings.GetDefaultLogPath();
                Directory.CreateDirectory(logFolder);

                // Ana loglama sistemiyle eşleşmesi için saatlik log dosyaları kullan
                var currentHour = DateTime.Now.ToString("yyyy-MM-dd-HH");
                var logFile = Path.Combine(logFolder, $"centralized-log-{currentHour}.log");
                var logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{level}] [{_source}] {message}";
                
                if (data != null)
                {
                    logEntry += $" | Data: {JsonConvert.SerializeObject(data)}";
                }

                await File.AppendAllTextAsync(logFile, logEntry + Environment.NewLine);

                // Eski log dosyalarını temizle (48 saatten eski)
                CleanupOldLogFiles(logFolder, "centralized-log-", 48);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to log locally: {ex.Message}");
            }
        }

        private void CleanupOldLogFiles(string logFolder, string filePrefix, int hoursToKeep)
        {
            try
            {
                var cutoffTime = DateTime.Now.AddHours(-hoursToKeep);
                var files = Directory.GetFiles(logFolder, $"{filePrefix}*.log");
                
                foreach (var file in files)
                {
                    var fileName = Path.GetFileNameWithoutExtension(file);
                    if (fileName.StartsWith(filePrefix))
                    {
                        var dateTimeStr = fileName.Substring(filePrefix.Length);
                        if (DateTime.TryParseExact(dateTimeStr, "yyyy-MM-dd-HH", null, System.Globalization.DateTimeStyles.None, out DateTime fileDateTime))
                        {
                            if (fileDateTime < cutoffTime)
                            {
                                try { File.Delete(file); } catch { }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to cleanup old log files: {ex.Message}");
            }
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}