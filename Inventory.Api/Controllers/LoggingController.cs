using Microsoft.AspNetCore.Mvc;
using Inventory.Api.Services;
using Swashbuckle.AspNetCore.Annotations;

namespace Inventory.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [SwaggerTag("Centralized logging operations - for agent and network scanner logs")]
    public class LoggingController : ControllerBase
    {
        private readonly ICentralizedLoggingService _loggingService;

        public LoggingController(ICentralizedLoggingService loggingService)
        {
            _loggingService = loggingService;
        }

        [HttpPost]
        [SwaggerOperation(Summary = "Submit log entry", Description = "Allows agents and network scanners to submit log entries centrally")]
        [SwaggerResponse(200, "Log entry submitted successfully")]
        [SwaggerResponse(400, "Invalid log entry")]
        public async Task<IActionResult> SubmitLog([FromBody] LogSubmissionDto logEntry)
        {
            try
            {
                await _loggingService.LogAsync(logEntry.Source, logEntry.Level, logEntry.Message, logEntry.Data);
                return Ok(new { message = "Log entry submitted successfully." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = $"Failed to submit log entry: {ex.Message}" });
            }
        }

        [HttpGet("recent")]
        [SwaggerOperation(Summary = "Get recent log entries", Description = "Returns the most recent log entries from all sources")]
        [SwaggerResponse(200, "Returns recent log entries")]
        public IActionResult GetRecentLogs([FromQuery] int count = 100)
        {
            var logs = _loggingService.GetRecentLogs(count);
            return Ok(logs);
        }

        [HttpGet("recent/{source}")]
        [SwaggerOperation(Summary = "Get recent log entries by source", Description = "Returns recent log entries filtered by source")]
        [SwaggerResponse(200, "Returns recent log entries for the specified source")]
        public IActionResult GetRecentLogsBySource(string source, [FromQuery] int count = 100)
        {
            var logs = _loggingService.GetRecentLogs(count * 2) // Get more to filter
                .Where(l => l.Source.Equals(source, StringComparison.OrdinalIgnoreCase))
                .Take(count)
                .ToList();
            return Ok(logs);
        }

        [HttpGet("sources")]
        [SwaggerOperation(Summary = "Get log sources", Description = "Returns all available log sources")]
        [SwaggerResponse(200, "Returns available log sources")]
        public IActionResult GetLogSources()
        {
            var logs = _loggingService.GetRecentLogs(1000);
            var sources = logs.Select(l => l.Source).Distinct().OrderBy(s => s).ToList();
            return Ok(sources);
        }
    }

    public class LogSubmissionDto
    {
        public string Source { get; set; } = string.Empty;
        public string Level { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public object? Data { get; set; }
    }
}