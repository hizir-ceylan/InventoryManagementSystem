using System;

namespace Inventory.Domain.Entities
{
    public class VMwareServer
    {
        public Guid Id { get; set; }
        public string? Name { get; set; }
        public string? ServerAddress { get; set; }
        public int Port { get; set; } = 443;
        public string? Username { get; set; }
        
        // Connection status
        public bool IsConnected { get; set; }
        public DateTime? LastConnection { get; set; }
        public string? ConnectionError { get; set; }
        
        // Server information
        public string? Version { get; set; }
        public string? Build { get; set; }
        public string? ProductType { get; set; } // vCenter, ESXi
        public string? LicenseType { get; set; }
        
        // Resource information
        public long? TotalMemoryMB { get; set; }
        public long? UsedMemoryMB { get; set; }
        public long? TotalStorageGB { get; set; }
        public long? UsedStorageGB { get; set; }
        public long? FreeStorageGB { get; set; }
        public int? TotalCpuCores { get; set; }
        public double? CpuUsagePercent { get; set; }
        
        // VM Statistics
        public int? TotalVMs { get; set; }
        public int? RunningVMs { get; set; }
        public int? StoppedVMs { get; set; }
        public int? SuspendedVMs { get; set; }
        
        // Sync configuration
        public bool AutoSync { get; set; } = true;
        public int SyncIntervalMinutes { get; set; } = 30;
        public DateTime? LastSyncDate { get; set; }
        public string? LastSyncStatus { get; set; }
        public string? LastSyncError { get; set; }
        
        // Audit fields
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public bool IsActive { get; set; } = true;
    }
}