using Inventory.Data;
using Inventory.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Api.Services
{
    public interface IPredefinedNetworkRangeService
    {
        Task<IEnumerable<PredefinedNetworkRange>> GetAllAsync();
        Task<IEnumerable<PredefinedNetworkRange>> GetActiveAsync();
        Task<PredefinedNetworkRange?> GetByIdAsync(Guid id);
        Task<PredefinedNetworkRange?> GetByNetworkRangeAsync(string networkRange);
        Task<PredefinedNetworkRange> CreateAsync(PredefinedNetworkRange predefinedRange);
        Task<PredefinedNetworkRange> UpdateAsync(PredefinedNetworkRange predefinedRange);
        Task<bool> DeleteAsync(Guid id);
        Task UpdateLastScanTimeAsync(Guid id);
    }

    public class PredefinedNetworkRangeService : IPredefinedNetworkRangeService
    {
        private readonly ILogger<PredefinedNetworkRangeService> _logger;
        private readonly InventoryDbContext _context;

        public PredefinedNetworkRangeService(ILogger<PredefinedNetworkRangeService> logger, InventoryDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<IEnumerable<PredefinedNetworkRange>> GetAllAsync()
        {
            return await _context.PredefinedNetworkRanges
                .OrderBy(p => p.Name)
                .ToListAsync();
        }

        public async Task<IEnumerable<PredefinedNetworkRange>> GetActiveAsync()
        {
            return await _context.PredefinedNetworkRanges
                .Where(p => p.IsActive)
                .OrderBy(p => p.Name)
                .ToListAsync();
        }

        public async Task<PredefinedNetworkRange?> GetByIdAsync(Guid id)
        {
            return await _context.PredefinedNetworkRanges
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<PredefinedNetworkRange?> GetByNetworkRangeAsync(string networkRange)
        {
            return await _context.PredefinedNetworkRanges
                .FirstOrDefaultAsync(p => p.NetworkRange == networkRange);
        }

        public async Task<PredefinedNetworkRange> CreateAsync(PredefinedNetworkRange predefinedRange)
        {
            predefinedRange.Id = Guid.NewGuid();
            predefinedRange.CreatedAt = DateTime.UtcNow;
            
            _context.PredefinedNetworkRanges.Add(predefinedRange);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Predefined network range created: {Name} ({NetworkRange})", 
                predefinedRange.Name, predefinedRange.NetworkRange);
            
            return predefinedRange;
        }

        public async Task<PredefinedNetworkRange> UpdateAsync(PredefinedNetworkRange predefinedRange)
        {
            _context.Entry(predefinedRange).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Predefined network range updated: {Name} ({NetworkRange})", 
                predefinedRange.Name, predefinedRange.NetworkRange);
            
            return predefinedRange;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var predefinedRange = await _context.PredefinedNetworkRanges.FindAsync(id);
            if (predefinedRange == null)
                return false;

            _context.PredefinedNetworkRanges.Remove(predefinedRange);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Predefined network range deleted: {Name} ({NetworkRange})", 
                predefinedRange.Name, predefinedRange.NetworkRange);
            
            return true;
        }

        public async Task UpdateLastScanTimeAsync(Guid id)
        {
            var predefinedRange = await _context.PredefinedNetworkRanges.FindAsync(id);
            if (predefinedRange != null)
            {
                predefinedRange.LastScanTime = DateTime.UtcNow;
                predefinedRange.ScanCount++;
                await _context.SaveChangesAsync();
            }
        }
    }
}