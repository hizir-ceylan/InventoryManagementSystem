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
}