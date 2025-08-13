using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Inventory.Data;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Api.Controllers
{
    /// <summary>
    /// Health Check Controller
    /// Provides health status information for the API and database
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [SwaggerTag("Health check operations - API and database status")]
    public class HealthController : ControllerBase
    {
        private readonly InventoryDbContext _context;
        private readonly ILogger<HealthController> _logger;

        public HealthController(InventoryDbContext context, ILogger<HealthController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Simple health check endpoint
        /// </summary>
        [HttpGet]
        [SwaggerOperation(Summary = "Health check", Description = "Returns API health status")]
        [SwaggerResponse(200, "API is healthy")]
        [SwaggerResponse(503, "API is unhealthy")]
        public async Task<ActionResult<object>> GetHealth()
        {
            try
            {
                // Test database connection
                var canConnect = await _context.Database.CanConnectAsync();
                
                var health = new
                {
                    Status = canConnect ? "Healthy" : "Unhealthy",
                    Timestamp = DateTime.UtcNow,
                    Version = "1.0.0",
                    Database = canConnect ? "Connected" : "Disconnected",
                    Uptime = DateTime.UtcNow.Subtract(System.Diagnostics.Process.GetCurrentProcess().StartTime).ToString(@"dd\.hh\:mm\:ss")
                };

                if (canConnect)
                {
                    return Ok(health);
                }
                else
                {
                    return StatusCode(503, health);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Health check failed");
                
                var health = new
                {
                    Status = "Unhealthy",
                    Timestamp = DateTime.UtcNow,
                    Version = "1.0.0",
                    Database = "Error",
                    Error = ex.Message
                };
                
                return StatusCode(503, health);
            }
        }
    }
}