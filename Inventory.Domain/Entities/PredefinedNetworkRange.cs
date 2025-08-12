using System.ComponentModel.DataAnnotations;

namespace Inventory.Domain.Entities
{
    public class PredefinedNetworkRange
    {
        public Guid Id { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(50)]
        public string NetworkRange { get; set; } = string.Empty;
        
        [MaxLength(200)]
        public string? Description { get; set; }
        
        public bool IsActive { get; set; } = true;
        
        public int TimeoutSeconds { get; set; } = 5;
        
        [MaxLength(20)]
        public string PortScanType { get; set; } = "common";
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime? LastScanTime { get; set; }
        
        public int ScanCount { get; set; } = 0;
    }
}