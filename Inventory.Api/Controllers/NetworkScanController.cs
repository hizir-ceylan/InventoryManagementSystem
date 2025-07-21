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
        [SwaggerOperation(Summary = "Trigger manual network scan", Description = "Manually triggers a network scan to discover devices")]
        [SwaggerResponse(200, "Network scan triggered successfully")]
        [SwaggerResponse(400, "Failed to trigger network scan")]
        public async Task<IActionResult> TriggerNetworkScan()
        {
            try
            {
                await _networkScanService.TriggerManualScanAsync();
                return Ok(new { message = "Network scan triggered successfully." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = $"Failed to trigger network scan: {ex.Message}" });
            }
        }

        [HttpPost("trigger-range")]
        [SwaggerOperation(Summary = "Trigger manual network scan for specific range", Description = "Manually triggers a network scan for a specific network range")]
        [SwaggerResponse(200, "Network scan triggered successfully")]
        [SwaggerResponse(400, "Failed to trigger network scan")]
        public async Task<IActionResult> TriggerNetworkScanForRange([FromBody] NetworkRangeDto request)
        {
            try
            {
                await _networkScanService.TriggerManualScanAsync(request.NetworkRange);
                return Ok(new { message = $"Network scan triggered successfully for range: {request.NetworkRange}" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = $"Failed to trigger network scan: {ex.Message}" });
            }
        }

        [HttpPost("trigger-all")]
        [SwaggerOperation(Summary = "Trigger network scan for all local networks", Description = "Manually triggers a network scan for all detected local network ranges")]
        [SwaggerResponse(200, "Network scan triggered successfully")]
        [SwaggerResponse(400, "Failed to trigger network scan")]
        public async Task<IActionResult> TriggerScanAllNetworks()
        {
            try
            {
                await _networkScanService.TriggerScanAllNetworksAsync();
                return Ok(new { message = "Network scan triggered successfully for all local networks." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = $"Failed to trigger network scan: {ex.Message}" });
            }
        }

        [HttpGet("status")]
        [SwaggerOperation(Summary = "Get network scan status", Description = "Returns the current status of network scanning")]
        [SwaggerResponse(200, "Returns scan status information")]
        public IActionResult GetScanStatus()
        {
            var status = _networkScanService.GetScanStatus();
            return Ok(status);
        }

        [HttpGet("history")]
        [SwaggerOperation(Summary = "Get network scan history", Description = "Returns the history of network scans")]
        [SwaggerResponse(200, "Returns scan history")]
        public IActionResult GetScanHistory()
        {
            var history = _networkScanService.GetScanHistory();
            return Ok(history);
        }

        [HttpPost("schedule")]
        [SwaggerOperation(Summary = "Set network scan schedule", Description = "Configures the schedule for automatic network scanning")]
        [SwaggerResponse(200, "Scan schedule updated successfully")]
        [SwaggerResponse(400, "Failed to update schedule")]
        public IActionResult SetSchedule([FromBody] NetworkScanScheduleDto schedule)
        {
            try
            {
                _networkScanService.SetSchedule(schedule);
                return Ok(new { message = "Scan schedule updated successfully." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = $"Failed to update schedule: {ex.Message}" });
            }
        }

        [HttpGet("network-ranges")]
        [SwaggerOperation(Summary = "Get local network ranges", Description = "Returns the detected local network ranges")]
        [SwaggerResponse(200, "Returns detected network ranges")]
        public IActionResult GetNetworkRanges()
        {
            var ranges = _networkScanService.GetLocalNetworkRanges();
            return Ok(new { networkRanges = ranges });
        }

        [HttpGet("schedule")]
        [SwaggerOperation(Summary = "Get network scan schedule", Description = "Returns the current network scan schedule configuration")]
        [SwaggerResponse(200, "Returns schedule configuration")]
        public IActionResult GetSchedule()
        {
            var schedule = _networkScanService.GetSchedule();
            return Ok(schedule);
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
}