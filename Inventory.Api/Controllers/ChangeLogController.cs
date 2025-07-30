using Microsoft.AspNetCore.Mvc;
using Inventory.Domain.Entities;
using Inventory.Data;
using Microsoft.EntityFrameworkCore;
using Swashbuckle.AspNetCore.Annotations;

namespace Inventory.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [SwaggerTag("Change log management operations for tracking device modifications")]
    public class ChangeLogController : ControllerBase
    {
        private readonly InventoryDbContext _context;
        private readonly ILogger<ChangeLogController> _logger;

        public ChangeLogController(InventoryDbContext context, ILogger<ChangeLogController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        [SwaggerOperation(Summary = "Get all change logs", Description = "Returns all change logs in the system")]
        [SwaggerResponse(200, "Returns the list of change logs", typeof(IEnumerable<ChangeLog>))]
        public async Task<ActionResult<IEnumerable<ChangeLog>>> GetAll()
        {
            var changeLogs = await _context.ChangeLogs
                .OrderByDescending(c => c.ChangeDate)
                .ToListAsync();
            return Ok(changeLogs);
        }

        [HttpGet("device/{deviceId}")]
        [SwaggerOperation(Summary = "Get change logs for a specific device", Description = "Returns change logs for a specific device by device ID")]
        [SwaggerResponse(200, "Returns the list of change logs for the device", typeof(IEnumerable<ChangeLog>))]
        [SwaggerResponse(404, "Device not found")]
        public async Task<ActionResult<IEnumerable<ChangeLog>>> GetByDeviceId(Guid deviceId)
        {
            // Check if device exists
            var deviceExists = await _context.Devices.AnyAsync(d => d.Id == deviceId);
            if (!deviceExists)
                return NotFound(new { error = "Device not found" });

            var changeLogs = await _context.ChangeLogs
                .Where(c => c.DeviceId == deviceId)
                .OrderByDescending(c => c.ChangeDate)
                .ToListAsync();
            
            return Ok(changeLogs);
        }

        [HttpPost("device/{deviceId}")]
        [SwaggerOperation(Summary = "Create change logs for a device", Description = "Creates multiple change logs for a specific device")]
        [SwaggerResponse(201, "Change logs created successfully")]
        [SwaggerResponse(400, "Invalid data")]
        [SwaggerResponse(404, "Device not found")]
        public async Task<ActionResult> CreateChangeLogsForDevice(Guid deviceId, [FromBody] List<ChangeLogRequestDto> changeLogDtos)
        {
            // Check if device exists
            var device = await _context.Devices.FindAsync(deviceId);
            if (device == null)
                return NotFound(new { error = "Device not found" });

            if (changeLogDtos == null || !changeLogDtos.Any())
                return BadRequest(new { error = "No change logs provided" });

            var changeLogs = new List<ChangeLog>();
            
            foreach (var dto in changeLogDtos)
            {
                var changeLog = new ChangeLog
                {
                    Id = Guid.NewGuid(),
                    DeviceId = deviceId,
                    ChangeDate = dto.ChangeDate,
                    ChangeType = dto.ChangeType,
                    OldValue = dto.OldValue,
                    NewValue = dto.NewValue,
                    ChangedBy = dto.ChangedBy
                };
                changeLogs.Add(changeLog);
            }

            _context.ChangeLogs.AddRange(changeLogs);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created {ChangeLogCount} change logs for device {DeviceId}", changeLogs.Count, deviceId);

            return CreatedAtAction(nameof(GetByDeviceId), new { deviceId }, new { message = $"Created {changeLogs.Count} change logs", count = changeLogs.Count });
        }

        [HttpPost("batch")]
        [SwaggerOperation(Summary = "Create change logs by device identification", Description = "Creates change logs by finding device through IP or MAC address")]
        [SwaggerResponse(201, "Change logs created successfully")]
        [SwaggerResponse(400, "Invalid data")]
        [SwaggerResponse(404, "Device not found")]
        public async Task<ActionResult> CreateChangeLogsBatch([FromBody] DeviceChangeLogBatchDto batchDto)
        {
            if (batchDto?.ChangeLogs == null || !batchDto.ChangeLogs.Any())
                return BadRequest(new { error = "No change logs provided" });

            // Find device by IP or MAC address
            Device? device = null;
            
            if (!string.IsNullOrWhiteSpace(batchDto.DeviceIpAddress))
            {
                device = await _context.Devices.FirstOrDefaultAsync(d => d.IpAddress == batchDto.DeviceIpAddress);
            }
            
            if (device == null && !string.IsNullOrWhiteSpace(batchDto.DeviceMacAddress))
            {
                device = await _context.Devices.FirstOrDefaultAsync(d => d.MacAddress == batchDto.DeviceMacAddress);
            }

            if (device == null)
                return NotFound(new { error = "Device not found by provided IP or MAC address" });

            var changeLogs = new List<ChangeLog>();
            
            foreach (var dto in batchDto.ChangeLogs)
            {
                var changeLog = new ChangeLog
                {
                    Id = Guid.NewGuid(),
                    DeviceId = device.Id,
                    ChangeDate = dto.ChangeDate,
                    ChangeType = dto.ChangeType,
                    OldValue = dto.OldValue,
                    NewValue = dto.NewValue,
                    ChangedBy = dto.ChangedBy
                };
                changeLogs.Add(changeLog);
            }

            _context.ChangeLogs.AddRange(changeLogs);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created {ChangeLogCount} change logs for device {DeviceName} (ID: {DeviceId})", 
                changeLogs.Count, device.Name, device.Id);

            return CreatedAtAction(nameof(GetByDeviceId), new { deviceId = device.Id }, 
                new { message = $"Created {changeLogs.Count} change logs for device {device.Name}", count = changeLogs.Count, deviceId = device.Id });
        }

        [HttpDelete("{id}")]
        [SwaggerOperation(Summary = "Delete a change log", Description = "Deletes a specific change log")]
        [SwaggerResponse(204, "Change log deleted successfully")]
        [SwaggerResponse(404, "Change log not found")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var changeLog = await _context.ChangeLogs.FindAsync(id);
            if (changeLog == null)
                return NotFound();

            _context.ChangeLogs.Remove(changeLog);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Deleted change log {ChangeLogId}", id);
            return NoContent();
        }
    }

    public class ChangeLogRequestDto
    {
        public DateTime ChangeDate { get; set; }
        public string ChangeType { get; set; } = string.Empty;
        public string OldValue { get; set; } = string.Empty;
        public string NewValue { get; set; } = string.Empty;
        public string ChangedBy { get; set; } = string.Empty;
    }

    public class DeviceChangeLogBatchDto
    {
        public string? DeviceIpAddress { get; set; }
        public string? DeviceMacAddress { get; set; }
        public List<ChangeLogRequestDto> ChangeLogs { get; set; } = new();
    }
}