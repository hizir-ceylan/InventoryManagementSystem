using System;

namespace Inventory.Domain.Entities
{
    public class VMwareSyncLog
    {
        public Guid Id { get; set; }
        public Guid VMwareServerId { get; set; }
        public VMwareServer? VMwareServer { get; set; }
        
        // Sync information
        public DateTime SyncStartTime { get; set; }
        public DateTime? SyncEndTime { get; set; }
        public string Status { get; set; } = "InProgress"; // InProgress, Completed, Failed
        public string? ErrorMessage { get; set; }
        public string? ErrorDetails { get; set; }
        
        // Statistics
        public int VirtualMachinesFound { get; set; }
        public int VirtualMachinesCreated { get; set; }
        public int VirtualMachinesUpdated { get; set; }
        public int VirtualMachinesDeleted { get; set; }
        public int ErrorsEncountered { get; set; }
        
        // Performance metrics
        public TimeSpan? Duration { get; set; }
        public long? DataTransferredBytes { get; set; }
        
        // Additional information
        public string? SyncType { get; set; } // Manual, Automatic, Scheduled
        public string? SyncTrigger { get; set; } // User, Schedule, Event
        public string? Version { get; set; } // System version during sync
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}