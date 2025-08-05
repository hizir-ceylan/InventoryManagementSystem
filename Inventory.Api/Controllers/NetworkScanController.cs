using Microsoft.AspNetCore.Mvc;
using Inventory.Api.Services;
using Swashbuckle.AspNetCore.Annotations;

namespace Inventory.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [SwaggerTag("Network scanning operations - manual and scheduled network discovery")]
    public class NetworkScanController : ControllerBase
    {
        private readonly INetworkScanService _networkScanService;

        public NetworkScanController(INetworkScanService networkScanService)
        {
            _networkScanService = networkScanService;
        }

        [HttpPost("trigger")]
        [SwaggerOperation(Summary = "Manuel ağ taraması başlat", Description = "Cihazları keşfetmek için manuel olarak ağ taraması başlatır")]
        [SwaggerResponse(200, "Ağ taraması başarıyla başlatıldı")]
        [SwaggerResponse(400, "Ağ taraması başlatılamadı")]
        public async Task<IActionResult> TriggerNetworkScan()
        {
            try
            {
                await _networkScanService.TriggerManualScanAsync();
                return Ok(new { message = "Ağ taraması başarıyla başlatıldı." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = $"Ağ taraması başlatılamadı: {ex.Message}" });
            }
        }

        [HttpPost("trigger-range")]
        [SwaggerOperation(Summary = "Belirli aralık için manuel ağ taraması başlat", Description = "Belirli bir ağ aralığı için manuel ağ taraması başlatır")]
        [SwaggerResponse(200, "Ağ taraması başarıyla başlatıldı")]
        [SwaggerResponse(400, "Ağ taraması başlatılamadı")]
        public async Task<IActionResult> TriggerNetworkScanForRange([FromBody] NetworkRangeDto request)
        {
            try
            {
                await _networkScanService.TriggerManualScanAsync(request.NetworkRange);
                return Ok(new { message = $"Ağ taraması {request.NetworkRange} aralığı için başarıyla başlatıldı" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = $"Ağ taraması başlatılamadı: {ex.Message}" });
            }
        }

        [HttpPost("trigger-all")]
        [SwaggerOperation(Summary = "Tüm yerel ağlar için ağ taraması başlat", Description = "Tespit edilen tüm yerel ağ aralıkları için manuel ağ taraması başlatır")]
        [SwaggerResponse(200, "Ağ taraması başarıyla başlatıldı")]
        [SwaggerResponse(400, "Ağ taraması başlatılamadı")]
        public async Task<IActionResult> TriggerScanAllNetworks()
        {
            try
            {
                await _networkScanService.TriggerScanAllNetworksAsync();
                return Ok(new { message = "Tüm yerel ağlar için ağ taraması başarıyla başlatıldı." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = $"Ağ taraması başlatılamadı: {ex.Message}" });
            }
        }

        [HttpGet("status")]
        [SwaggerOperation(Summary = "Ağ tarama durumunu getir", Description = "Ağ taramasının mevcut durumunu döndürür")]
        [SwaggerResponse(200, "Tarama durumu bilgisini döndürür")]
        public IActionResult GetScanStatus()
        {
            var status = _networkScanService.GetScanStatus();
            return Ok(status);
        }

        [HttpGet("history")]
        [SwaggerOperation(Summary = "Ağ tarama geçmişini getir", Description = "Ağ taramalarının geçmişini döndürür")]
        [SwaggerResponse(200, "Tarama geçmişini döndürür")]
        public IActionResult GetScanHistory()
        {
            var history = _networkScanService.GetScanHistory();
            return Ok(history);
        }

        [HttpPost("schedule")]
        [SwaggerOperation(Summary = "Ağ tarama zamanlaması ayarla", Description = "Otomatik ağ taraması için zamanlamayı yapılandırır")]
        [SwaggerResponse(200, "Tarama zamanlaması başarıyla güncellendi")]
        [SwaggerResponse(400, "Zamanlama güncellenemedi")]
        public IActionResult SetSchedule([FromBody] NetworkScanScheduleDto schedule)
        {
            try
            {
                _networkScanService.SetSchedule(schedule);
                return Ok(new { message = "Tarama zamanlaması başarıyla güncellendi." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = $"Zamanlama güncellenemedi: {ex.Message}" });
            }
        }

        [HttpGet("network-ranges")]
        [SwaggerOperation(Summary = "Yerel ağ aralıklarını getir", Description = "Tespit edilen yerel ağ aralıklarını döndürür")]
        [SwaggerResponse(200, "Tespit edilen ağ aralıklarını döndürür")]
        public IActionResult GetNetworkRanges()
        {
            var ranges = _networkScanService.GetLocalNetworkRanges();
            return Ok(new { networkRanges = ranges });
        }

        [HttpGet("schedule")]
        [SwaggerOperation(Summary = "Ağ tarama zamanlamasını getir", Description = "Mevcut ağ tarama zamanlaması yapılandırmasını döndürür")]
        [SwaggerResponse(200, "Zamanlama yapılandırmasını döndürür")]
        public IActionResult GetSchedule()
        {
            var schedule = _networkScanService.GetSchedule();
            return Ok(schedule);
        }

        [HttpGet("local-ranges")]
        [SwaggerOperation(Summary = "Yerel ağ aralıklarını getir", Description = "Tespit edilen yerel ağ aralıklarını döndürür")]
        [SwaggerResponse(200, "Yerel ağ aralıklarını döndürür")]
        public IActionResult GetLocalNetworkRanges()
        {
            var ranges = _networkScanService.GetLocalNetworkRanges();
            return Ok(ranges);
        }

        [HttpPost("trigger-port-scan")]
        [SwaggerOperation(Summary = "Port taraması başlat", Description = "Belirli port için cihaz taraması başlatır")]
        [SwaggerResponse(200, "Port taraması başarıyla başlatıldı")]
        [SwaggerResponse(400, "Port taraması başlatılamadı")]
        public async Task<IActionResult> TriggerPortScan([FromBody] PortScanRequestDto request)
        {
            try
            {
                // For now, we'll treat port scan similar to regular scan with additional metadata
                if (!string.IsNullOrEmpty(request.NetworkRange))
                {
                    await _networkScanService.TriggerManualScanAsync(request.NetworkRange);
                }
                else
                {
                    await _networkScanService.TriggerManualScanAsync();
                }
                
                return Ok(new { message = $"Port {request.TargetPort} için ağ taraması başarıyla başlatıldı" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = $"Port taraması başlatılamadı: {ex.Message}" });
            }
        }
    }

    public class NetworkScanScheduleDto
    {
        public bool Enabled { get; set; }
        public TimeSpan Interval { get; set; }
        public string? NetworkRange { get; set; }
    }

    public class NetworkRangeDto
    {
        public string NetworkRange { get; set; } = string.Empty;
    }

    public class PortScanRequestDto
    {
        public string NetworkRange { get; set; } = string.Empty;
        public int? TargetPort { get; set; }
        public string ScanType { get; set; } = string.Empty;
    }
}