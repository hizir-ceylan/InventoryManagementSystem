using System.Net.Http;
using System.Text;
using Newtonsoft.Json;

namespace Inventory.Api.Services
{
    public interface ICentralizedLoggingService
    {
        Task LogAsync(string source, string level, string message, object? data = null);
        Task LogErrorAsync(string source, string message, Exception? exception = null);
        Task LogInfoAsync(string source, string message, object? data = null);
        Task LogWarningAsync(string source, string message, object? data = null);
        List<LogEntry> GetRecentLogs(int count = 100);
    }

    public class CentralizedLoggingService : ICentralizedLoggingService
    {
        private readonly ILogger<CentralizedLoggingService> _logger;
        private readonly List<LogEntry> _logs = new();
        private readonly object _lockObject = new();

        public CentralizedLoggingService(ILogger<CentralizedLoggingService> logger)
        {
            _logger = logger;
        }

        public async Task LogAsync(string source, string level, string message, object? data = null)
        {
            var logEntry = new LogEntry
            {
                Id = Guid.NewGuid(),
                Timestamp = DateTime.UtcNow,
                Source = source,
                Level = level,
                Message = message,
                Data = data
            };

            // Log to local collection
            lock (_lockObject)
            {
                _logs.Add(logEntry);
                
                // Keep only last 1000 logs to prevent memory issues
                if (_logs.Count > 1000)
                {
                    _logs.RemoveRange(0, _logs.Count - 1000);
                }
            }

            // Also log to standard logging
            switch (level.ToLower())
            {
                case "error":
                    _logger.LogError("[{Source}] {Message} {Data}", source, message, data);
                    break;
                case "warning":
                    _logger.LogWarning("[{Source}] {Message} {Data}", source, message, data);
                    break;
                case "info":
                    _logger.LogInformation("[{Source}] {Message} {Data}", source, message, data);
                    break;
                default:
                    _logger.LogDebug("[{Source}] {Message} {Data}", source, message, data);
                    break;
            }

            await Task.CompletedTask;
        }

        public async Task LogErrorAsync(string source, string message, Exception? exception = null)
        {
            var data = exception != null ? new { Exception = exception.ToString() } : null;
            await LogAsync(source, "Error", message, data);
        }

        public async Task LogInfoAsync(string source, string message, object? data = null)
        {
            await LogAsync(source, "Info", message, data);
        }

        public async Task LogWarningAsync(string source, string message, object? data = null)
        {
            await LogAsync(source, "Warning", message, data);
        }

        public List<LogEntry> GetRecentLogs(int count = 100)
        {
            lock (_lockObject)
            {
                return _logs.TakeLast(count).ToList();
            }
        }
    }

    public class LogEntry
    {
        public Guid Id { get; set; }
        public DateTime Timestamp { get; set; }
        public string Source { get; set; } = string.Empty;
        public string Level { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public object? Data { get; set; }
    }
}