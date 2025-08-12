using System.ComponentModel.DataAnnotations;

namespace Inventory.Domain.Entities
{
    public class NetworkScanHistory
    {
        public Guid Id { get; set; }
        public DateTime ScanTime { get; set; }
        
        [MaxLength(50)]
        public string ScanType { get; set; } = string.Empty;
        
        [MaxLength(50)]
        public string Status { get; set; } = string.Empty;
        
        public int DevicesFound { get; set; }
        
        [MaxLength(500)]
        public string? Error { get; set; }
        
        [MaxLength(50)]
        public string? NetworkRange { get; set; }
        
        public int TimeoutSeconds { get; set; } = 5;
        
        [MaxLength(20)]
        public string PortScanType { get; set; } = "common";
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}