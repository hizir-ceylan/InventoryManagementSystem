using System.Text;
using Newtonsoft.Json;

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
                    // Fall back to local logging if API is unavailable
                    await LogLocallyAsync(level, message, data);
                }
            }
            catch (Exception ex)
            {
                // Fall back to local logging if API call fails
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
                var logFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "LocalLogs");
                Directory.CreateDirectory(logFolder);

                var logFile = Path.Combine(logFolder, $"centralized-log-{DateTime.Now:yyyy-MM-dd}.log");
                var logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{level}] [{_source}] {message}";
                
                if (data != null)
                {
                    logEntry += $" | Data: {JsonConvert.SerializeObject(data)}";
                }

                await File.AppendAllTextAsync(logFile, logEntry + Environment.NewLine);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to log locally: {ex.Message}");
            }
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}