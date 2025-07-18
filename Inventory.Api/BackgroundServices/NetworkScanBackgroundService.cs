using Inventory.Api.Services;

namespace Inventory.Api.BackgroundServices
{
    public class NetworkScanBackgroundService : BackgroundService
    {
        private readonly ILogger<NetworkScanBackgroundService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly IConfiguration _configuration;

        public NetworkScanBackgroundService(
            ILogger<NetworkScanBackgroundService> logger,
            IServiceProvider serviceProvider,
            IConfiguration configuration)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _configuration = configuration;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Network Scan Background Service started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var networkScanService = scope.ServiceProvider.GetRequiredService<INetworkScanService>();
                    
                    var schedule = networkScanService.GetSchedule() as dynamic;
                    
                    if (schedule?.Enabled == true)
                    {
                        _logger.LogInformation("Executing scheduled network scan...");
                        await networkScanService.TriggerManualScanAsync();
                        _logger.LogInformation("Scheduled network scan completed.");
                    }

                    // Get scan interval from configuration or use default
                    var scanInterval = _configuration.GetValue<TimeSpan>("NetworkScan:Interval", TimeSpan.FromHours(1));
                    await Task.Delay(scanInterval, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Network Scan Background Service is stopping.");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred during scheduled network scan");
                    
                    // Wait before retrying
                    await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
                }
            }

            _logger.LogInformation("Network Scan Background Service stopped.");
        }
    }
}