using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json;
using System.Text;
using Microsoft.Extensions.Options;

namespace Inventory.WebApp.Pages.NetworkScan
{
    public class IndexModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<IndexModel> _logger;
        private readonly ApiSettings _apiSettings;

        [BindProperty]
        public NetworkScanRequest ScanRequest { get; set; } = new();

        public string? StatusMessage { get; set; }
        public string? ErrorMessage { get; set; }
        public object? ScanStatus { get; set; }
        public List<string> LocalNetworkRanges { get; set; } = new();
        public List<object> ScanHistory { get; set; } = new();

        public IndexModel(IHttpClientFactory httpClientFactory, ILogger<IndexModel> logger, IOptions<ApiSettings> apiSettings)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _apiSettings = apiSettings.Value;
        }

        public async Task OnGetAsync()
        {
            await LoadDataAsync();
        }

        public async Task<IActionResult> OnPostStartScanAsync()
        {
            try
            {
                using var client = _httpClientFactory.CreateClient();
                client.BaseAddress = new Uri(_apiSettings.BaseUrl);

                HttpResponseMessage response;
                
                if (string.IsNullOrEmpty(ScanRequest.NetworkRange))
                {
                    // Trigger general scan
                    response = await client.PostAsync("/api/networkscan/trigger", null);
                }
                else
                {
                    // Trigger scan for specific range
                    var requestBody = new { NetworkRange = ScanRequest.NetworkRange };
                    var json = JsonSerializer.Serialize(requestBody);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");
                    
                    response = await client.PostAsync("/api/networkscan/trigger-range", content);
                }

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<JsonElement>(responseContent);
                    
                    if (result.TryGetProperty("message", out var messageElement))
                    {
                        StatusMessage = messageElement.GetString();
                    }
                    else
                    {
                        StatusMessage = "Ağ taraması başarıyla başlatıldı.";
                    }
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    ErrorMessage = $"Ağ taraması başlatılamadı: {response.StatusCode} - {errorContent}";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ağ taraması başlatılırken hata oluştu");
                ErrorMessage = "Ağ taraması başlatılırken hata oluştu. Lütfen daha sonra tekrar deneyin.";
            }

            await LoadDataAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostStartPortScanAsync()
        {
            try
            {
                using var client = _httpClientFactory.CreateClient();
                client.BaseAddress = new Uri(_apiSettings.BaseUrl);

                var requestBody = new 
                { 
                    NetworkRange = ScanRequest.NetworkRange,
                    TargetPort = ScanRequest.TargetPort,
                    ScanType = "PortScan"
                };
                
                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                var response = await client.PostAsync("/api/networkscan/trigger-port-scan", content);

                if (response.IsSuccessStatusCode)
                {
                    StatusMessage = $"Port {ScanRequest.TargetPort} için ağ taraması başlatıldı.";
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    ErrorMessage = $"Port taraması başlatılamadı: {response.StatusCode} - {errorContent}";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Port taraması başlatılırken hata oluştu");
                ErrorMessage = "Port taraması başlatılırken hata oluştu. Lütfen daha sonra tekrar deneyin.";
            }

            await LoadDataAsync();
            return Page();
        }

        private async Task LoadDataAsync()
        {
            try
            {
                using var client = _httpClientFactory.CreateClient();
                client.BaseAddress = new Uri(_apiSettings.BaseUrl);

                // Get scan status
                try
                {
                    var statusResponse = await client.GetAsync("/api/networkscan/status");
                    if (statusResponse.IsSuccessStatusCode)
                    {
                        var statusJson = await statusResponse.Content.ReadAsStringAsync();
                        ScanStatus = JsonSerializer.Deserialize<object>(statusJson);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Scan status alınamadı");
                }

                // Get local network ranges
                try
                {
                    var rangesResponse = await client.GetAsync("/api/networkscan/local-ranges");
                    if (rangesResponse.IsSuccessStatusCode)
                    {
                        var rangesJson = await rangesResponse.Content.ReadAsStringAsync();
                        var ranges = JsonSerializer.Deserialize<List<string>>(rangesJson);
                        if (ranges != null)
                        {
                            LocalNetworkRanges = ranges;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Local network ranges alınamadı");
                }

                // Get scan history
                try
                {
                    var historyResponse = await client.GetAsync("/api/networkscan/history");
                    if (historyResponse.IsSuccessStatusCode)
                    {
                        var historyJson = await historyResponse.Content.ReadAsStringAsync();
                        var history = JsonSerializer.Deserialize<List<object>>(historyJson);
                        if (history != null)
                        {
                            ScanHistory = history;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Scan history alınamadı");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Network scan data yüklenirken hata oluştu");
            }
        }
    }

    public class NetworkScanRequest
    {
        public string NetworkRange { get; set; } = string.Empty;
        public int? TargetPort { get; set; }
    }
}