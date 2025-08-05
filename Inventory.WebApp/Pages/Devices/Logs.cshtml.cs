using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Inventory.Domain.Entities;
using Inventory.Shared.Helpers;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace Inventory.WebApp.Pages.Devices
{
    public class LogsModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<LogsModel> _logger;
        private readonly ApiSettings _apiSettings;

        [BindProperty(SupportsGet = true)]
        public Guid Id { get; set; }

        public string DeviceName { get; set; } = string.Empty;
        public List<ChangeLogItem> ChangeLogs { get; set; } = new();
        public string? ErrorMessage { get; set; }

        public LogsModel(IHttpClientFactory httpClientFactory, ILogger<LogsModel> logger, IOptions<ApiSettings> apiSettings)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _apiSettings = apiSettings.Value;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            if (Id == Guid.Empty)
            {
                return NotFound();
            }

            try
            {
                using var client = _httpClientFactory.CreateClient();
                client.BaseAddress = new Uri(_apiSettings.BaseUrl);

                // First, get device info to show device name
                var deviceResponse = await client.GetAsync($"/api/device/{Id}");
                if (deviceResponse.IsSuccessStatusCode)
                {
                    var deviceJson = await deviceResponse.Content.ReadAsStringAsync();
                    var device = JsonSerializer.Deserialize<Device>(deviceJson, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    DeviceName = device?.Name ?? "Bilinmeyen Cihaz";
                }

                // Get change logs for the device
                var logsResponse = await client.GetAsync($"/api/changelog/device/{Id}");
                if (logsResponse.IsSuccessStatusCode)
                {
                    var logsJson = await logsResponse.Content.ReadAsStringAsync();
                    var changeLogDtos = JsonSerializer.Deserialize<List<ChangeLogDto>>(logsJson, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (changeLogDtos != null)
                    {
                        ChangeLogs = changeLogDtos.Select(c => new ChangeLogItem
                        {
                            Id = c.Id,
                            DeviceId = c.DeviceId,
                            ChangeDate = c.ChangeDate,
                            ChangeDateFormatted = c.ChangeDateFormatted, // Already formatted in Turkey time
                            ChangeType = c.ChangeType,
                            OldValue = c.OldValue,
                            NewValue = c.NewValue,
                            ChangedBy = c.ChangedBy
                        }).ToList();
                    }
                }
                else if (logsResponse.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    // Device exists but no logs found
                }
                else
                {
                    ErrorMessage = $"Loglar alınamadı: {logsResponse.StatusCode}";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Cihaz logları yüklenirken hata oluştu: {DeviceId}", Id);
                ErrorMessage = "Cihaz logları yüklenirken hata oluştu. Lütfen daha sonra tekrar deneyin.";
            }

            return Page();
        }
    }

    public class ChangeLogItem
    {
        public Guid Id { get; set; }
        public Guid DeviceId { get; set; }
        public DateTime ChangeDate { get; set; }
        public string ChangeDateFormatted { get; set; } = string.Empty;
        public string ChangeType { get; set; } = string.Empty;
        public string OldValue { get; set; } = string.Empty;
        public string NewValue { get; set; } = string.Empty;
        public string ChangedBy { get; set; } = string.Empty;
    }

    public class ChangeLogDto
    {
        public Guid Id { get; set; }
        public Guid DeviceId { get; set; }
        public DateTime ChangeDate { get; set; }
        public string ChangeDateFormatted { get; set; } = string.Empty;
        public string ChangeType { get; set; } = string.Empty;
        public string OldValue { get; set; } = string.Empty;
        public string NewValue { get; set; } = string.Empty;
        public string ChangedBy { get; set; } = string.Empty;
    }
}