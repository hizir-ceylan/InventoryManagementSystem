using Microsoft.AspNetCore.Mvc.RazorPages;
using Inventory.Domain.Entities;
using Inventory.Shared.Helpers;
using System.Text.Json;

namespace Inventory.WebApp.Pages.Devices
{
    public class IndexModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<IndexModel> _logger;

        public List<DeviceListItem> Devices { get; set; } = new();
        public string? ErrorMessage { get; set; }

        public IndexModel(IHttpClientFactory httpClientFactory, ILogger<IndexModel> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public async Task OnGetAsync()
        {
            try
            {
                using var client = _httpClientFactory.CreateClient();
                client.BaseAddress = new Uri("http://localhost:5093");

                var response = await client.GetAsync("/api/device/status");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var deviceStatuses = JsonSerializer.Deserialize<List<DeviceStatusDto>>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (deviceStatuses != null)
                    {
                        Devices = deviceStatuses.Select(d => new DeviceListItem
                        {
                            Id = d.Id,
                            Name = d.Name,
                            IpAddress = d.IpAddress,
                            DeviceType = d.DeviceType,
                            Model = d.Model,
                            Location = d.Location,
                            LastSeen = d.LastSeen,
                            LastSeenFormatted = d.LastSeenFormatted,
                            Status = d.Status,
                            IsActive = d.IsActive,
                            AgentInstalled = d.AgentInstalled,
                            ManagementType = d.ManagementType
                        }).ToList();
                    }
                }
                else
                {
                    ErrorMessage = $"API'den veri alınamadı: {response.StatusCode}";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Cihazlar yüklenirken hata oluştu");
                ErrorMessage = "Cihazlar yüklenirken hata oluştu. Lütfen daha sonra tekrar deneyin.";
            }
        }
    }

    public class DeviceListItem
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
        public string DeviceType { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public DateTime? LastSeen { get; set; }
        public string LastSeenFormatted { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public bool AgentInstalled { get; set; }
        public string ManagementType { get; set; } = string.Empty;
    }

    public class DeviceStatusDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
        public string DeviceType { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public DateTime? LastSeen { get; set; }
        public string LastSeenFormatted { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public bool AgentInstalled { get; set; }
        public string ManagementType { get; set; } = string.Empty;
    }
}