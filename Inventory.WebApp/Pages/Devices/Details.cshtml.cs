using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Inventory.Domain.Entities;
using Inventory.Shared.Helpers;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace Inventory.WebApp.Pages.Devices
{
    public class DetailsModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<DetailsModel> _logger;
        private readonly ApiSettings _apiSettings;

        [BindProperty(SupportsGet = true)]
        public Guid Id { get; set; }

        public DeviceDetailsItem? Device { get; set; }
        public string? ErrorMessage { get; set; }

        public DetailsModel(IHttpClientFactory httpClientFactory, ILogger<DetailsModel> logger, IOptions<ApiSettings> apiSettings)
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

                var response = await client.GetAsync($"/api/device/{Id}");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var device = JsonSerializer.Deserialize<Device>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (device != null)
                    {
                        Device = new DeviceDetailsItem
                        {
                            Id = device.Id,
                            Name = device.Name ?? "Bilinmeyen",
                            IpAddress = device.IpAddress ?? "-",
                            MacAddress = device.MacAddress ?? "-",
                            DeviceType = device.DeviceType.ToString(),
                            Model = device.Model ?? "-",
                            Location = device.Location ?? "-",
                            LastSeen = device.LastSeen,
                            LastSeenFormatted = device.LastSeen.HasValue 
                                ? TimezoneHelper.FormatInTurkeyTime(device.LastSeen.Value)
                                : "Hiç görülmedi",
                            Status = TimezoneHelper.IsActiveInLast12Hours(device.LastSeen) 
                                ? "Çalışıyor" 
                                : "Kapalı",
                            IsActive = TimezoneHelper.IsActiveInLast12Hours(device.LastSeen),
                            AgentInstalled = device.AgentInstalled,
                            ManagementType = device.ManagementType.ToString(),
                            DiscoveryMethod = device.DiscoveryMethod.ToString(),
                            HardwareInfo = device.HardwareInfo,
                            SoftwareInfo = device.SoftwareInfo
                        };
                    }
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return NotFound();
                }
                else
                {
                    ErrorMessage = $"API'den veri alınamadı: {response.StatusCode}";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Cihaz detayları yüklenirken hata oluştu: {DeviceId}", Id);
                ErrorMessage = "Cihaz detayları yüklenirken hata oluştu. Lütfen daha sonra tekrar deneyin.";
            }

            return Page();
        }
    }

    public class DeviceDetailsItem
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
        public string MacAddress { get; set; } = string.Empty;
        public string DeviceType { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public DateTime? LastSeen { get; set; }
        public string LastSeenFormatted { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public bool AgentInstalled { get; set; }
        public string ManagementType { get; set; } = string.Empty;
        public string DiscoveryMethod { get; set; } = string.Empty;
        public DeviceHardwareInfo? HardwareInfo { get; set; }
        public DeviceSoftwareInfo? SoftwareInfo { get; set; }
    }
}