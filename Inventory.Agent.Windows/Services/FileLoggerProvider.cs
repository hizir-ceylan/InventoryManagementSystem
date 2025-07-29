using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Inventory.Agent.Windows.Services
{
    public class FileLoggerProvider : ILoggerProvider
    {
        private readonly string _logDirectory;
        private readonly ConcurrentDictionary<string, FileLogger> _loggers = new();
        private readonly CancellationTokenSource _cancellationTokenSource = new();

        public FileLoggerProvider(string logDirectory)
        {
            _logDirectory = logDirectory;
        }

        public ILogger CreateLogger(string categoryName)
        {
            return _loggers.GetOrAdd(categoryName, name => new FileLogger(name, _logDirectory, _cancellationTokenSource.Token));
        }

        public void Dispose()
        {
            _cancellationTokenSource.Cancel();
            _loggers.Clear();
            _cancellationTokenSource.Dispose();
        }
    }

    public class FileLogger : ILogger
    {
        private readonly string _categoryName;
        private readonly string _logFilePath;
        private readonly CancellationToken _cancellationToken;
        private readonly SemaphoreSlim _semaphore = new(1, 1);

        public FileLogger(string categoryName, string logDirectory, CancellationToken cancellationToken)
        {
            _categoryName = categoryName;
            _cancellationToken = cancellationToken;
            
            var fileName = $"service-{DateTime.Now:yyyy-MM-dd}.log";
            _logFilePath = Path.Combine(logDirectory, fileName);
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Information;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel) || _cancellationToken.IsCancellationRequested)
                return;

            var message = formatter(state, exception);
            var logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{logLevel}] [{_categoryName}] {message}";
            
            if (exception != null)
            {
                logEntry += $"\nException: {exception}";
            }
            
            logEntry += Environment.NewLine;

            // Fire and forget to avoid blocking
            _ = Task.Run(async () =>
            {
                try
                {
                    await _semaphore.WaitAsync(_cancellationToken);
                    try
                    {
                        await File.AppendAllTextAsync(_logFilePath, logEntry, _cancellationToken);
                    }
                    finally
                    {
                        _semaphore.Release();
                    }
                }
                catch
                {
                    // Ignore file write errors to prevent cascading failures
                }
            }, _cancellationToken);
        }
    }
}