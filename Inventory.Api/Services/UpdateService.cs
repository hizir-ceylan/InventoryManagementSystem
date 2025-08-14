using Inventory.Data;
using Inventory.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Api.Services
{
    /// <summary>
    /// Güncelleme servisi interface'i
    /// </summary>
    public interface IUpdateService
    {
        Task<IEnumerable<SystemUpdate>> GetAllUpdatesAsync(UpdateStatus? status, UpdatePriority? priority, string? updateType, int page, int pageSize);
        Task<IEnumerable<SystemUpdate>> GetUpdatesByDeviceAsync(Guid deviceId);
        Task<IEnumerable<SystemUpdate>> GetAvailableUpdatesAsync();
        Task<IEnumerable<SystemUpdate>> GetCriticalUpdatesAsync();
        Task<object> GetUpdateStatisticsAsync();
        Task<object> GetUpdatesByTypeAsync();
        Task<int> SaveUpdatesAsync(List<SystemUpdate> updates);
        Task<Guid> StartUpdateScanAsync(Guid deviceId);
        Task<bool> UpdateStatusAsync(Guid updateId, UpdateStatus status, string? reason);
        Task<int> BulkUpdateStatusAsync(List<Guid> updateIds, UpdateStatus status, string? reason);
        Task<int> CleanupOldRecordsAsync(int daysOld);
    }

    /// <summary>
    /// Update Service Implementation
    /// </summary>
    public class UpdateService : IUpdateService
    {
        private readonly InventoryDbContext _context;
        private readonly ILogger<UpdateService> _logger;

        public UpdateService(InventoryDbContext context, ILogger<UpdateService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IEnumerable<SystemUpdate>> GetAllUpdatesAsync(UpdateStatus? status, UpdatePriority? priority, string? updateType, int page, int pageSize)
        {
            var query = _context.Set<SystemUpdate>().AsQueryable();

            if (status.HasValue)
                query = query.Where(u => u.Status == status.Value);

            if (priority.HasValue)
                query = query.Where(u => u.Priority == priority.Value);

            if (!string.IsNullOrEmpty(updateType))
                query = query.Where(u => u.UpdateType == updateType);

            return await query
                .OrderByDescending(u => u.DetectedDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<IEnumerable<SystemUpdate>> GetUpdatesByDeviceAsync(Guid deviceId)
        {
            return await _context.Set<SystemUpdate>()
                .Where(u => u.DeviceId == deviceId)
                .OrderByDescending(u => u.DetectedDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<SystemUpdate>> GetAvailableUpdatesAsync()
        {
            return await _context.Set<SystemUpdate>()
                .Where(u => u.Status == UpdateStatus.Available)
                .OrderByDescending(u => u.Priority)
                .ThenByDescending(u => u.DetectedDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<SystemUpdate>> GetCriticalUpdatesAsync()
        {
            return await _context.Set<SystemUpdate>()
                .Where(u => u.Priority >= UpdatePriority.Critical && u.Status == UpdateStatus.Available)
                .OrderByDescending(u => u.DetectedDate)
                .ToListAsync();
        }

        public async Task<object> GetUpdateStatisticsAsync()
        {
            var totalUpdates = await _context.Set<SystemUpdate>().CountAsync();
            var availableUpdates = await _context.Set<SystemUpdate>().CountAsync(u => u.Status == UpdateStatus.Available);
            var criticalUpdates = await _context.Set<SystemUpdate>().CountAsync(u => u.Priority >= UpdatePriority.Critical && u.Status == UpdateStatus.Available);
            var devicesWithUpdates = await _context.Set<SystemUpdate>()
                .Where(u => u.Status == UpdateStatus.Available)
                .Select(u => u.DeviceId)
                .Distinct()
                .CountAsync();

            return new
            {
                totalCount = totalUpdates,
                availableCount = availableUpdates,
                criticalCount = criticalUpdates,
                devicesWithUpdatesCount = devicesWithUpdates
            };
        }

        public async Task<object> GetUpdatesByTypeAsync()
        {
            var updatesByType = await _context.Set<SystemUpdate>()
                .GroupBy(u => u.UpdateType)
                .Select(g => new { Type = g.Key, Count = g.Count() })
                .ToListAsync();

            return updatesByType;
        }

        public async Task<int> SaveUpdatesAsync(List<SystemUpdate> updates)
        {
            if (updates == null || updates.Count == 0)
                return 0;

            try
            {
                await _context.Set<SystemUpdate>().AddRangeAsync(updates);
                return await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving updates");
                throw;
            }
        }

        public async Task<Guid> StartUpdateScanAsync(Guid deviceId)
        {
            try
            {
                var scanId = Guid.NewGuid();
                _logger.LogInformation("Starting update scan for device {DeviceId} with scan ID {ScanId}", deviceId, scanId);
                
                // Check if device exists
                var deviceExists = await _context.Set<Device>().AnyAsync(d => d.Id == deviceId);
                if (!deviceExists)
                {
                    throw new ArgumentException($"Device with ID {deviceId} not found");
                }
                
                // The Windows agent automatically runs update detection every 60 minutes
                // This API call acknowledges the scan request but doesn't trigger immediate detection
                _logger.LogInformation("Update scan request logged for device {DeviceId}. " +
                    "The Windows agent automatically detects and reports updates every 60 minutes. " +
                    "Check the update reports endpoint for detected updates.", deviceId);
                
                // TODO: For real-time triggering, implement agent communication via SignalR, message queue, 
                // or a polling mechanism where agents check for scan requests
                
                return scanId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start update scan for device {DeviceId}", deviceId);
                throw;
            }
        }

        public async Task<bool> UpdateStatusAsync(Guid updateId, UpdateStatus status, string? reason)
        {
            var update = await _context.Set<SystemUpdate>().FindAsync(updateId);
            if (update == null)
                return false;

            update.Status = status;
            update.UpdatedAt = DateTime.UtcNow;
            
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<int> BulkUpdateStatusAsync(List<Guid> updateIds, UpdateStatus status, string? reason)
        {
            var updates = await _context.Set<SystemUpdate>()
                .Where(u => updateIds.Contains(u.Id))
                .ToListAsync();

            foreach (var update in updates)
            {
                update.Status = status;
                update.UpdatedAt = DateTime.UtcNow;
            }

            return await _context.SaveChangesAsync();
        }

        public async Task<int> CleanupOldRecordsAsync(int daysOld)
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-daysOld);
            var oldUpdates = await _context.Set<SystemUpdate>()
                .Where(u => u.CreatedAt < cutoffDate && u.Status == UpdateStatus.Installed)
                .ToListAsync();

            _context.Set<SystemUpdate>().RemoveRange(oldUpdates);
            return await _context.SaveChangesAsync();
        }
    }
}