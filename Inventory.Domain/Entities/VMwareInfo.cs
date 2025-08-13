using System;

namespace Inventory.Domain.Entities
{
    public class VMwareInfo
    {
        public Guid Id { get; set; }
        public Guid DeviceId { get; set; }
        public Device? Device { get; set; }
        
        // vSphere/VMware specific information
        public string? VMwareId { get; set; }
        public string? InstanceUuid { get; set; }
        public string? PowerState { get; set; } // poweredOn, poweredOff, suspended
        public string? GuestOS { get; set; }
        public string? GuestFullName { get; set; }
        public string? HostName { get; set; } // ESXi host name
        public string? HostId { get; set; }
        public string? DatastoreName { get; set; }
        public string? DatastoreId { get; set; }
        public string? ResourcePoolName { get; set; }
        public string? ResourcePoolId { get; set; }
        public string? ClusterName { get; set; }
        public string? ClusterId { get; set; }
        
        // Virtual Hardware
        public int? CpuCount { get; set; }
        public int? CoresPerSocket { get; set; }
        public long? MemoryMB { get; set; }
        public long? ProvisionedSpaceGB { get; set; }
        public long? UsedSpaceGB { get; set; }
        public string? VirtualHardwareVersion { get; set; }
        
        // Tools
        public string? VMwareToolsStatus { get; set; }
        public string? VMwareToolsVersion { get; set; }
        public DateTime? VMwareToolsVersionDate { get; set; }
        
        // Network
        public string? NetworkName { get; set; }
        public string? NetworkType { get; set; } // distributed, standard
        public string? PortGroupName { get; set; }
        
        // Metadata
        public string? Annotation { get; set; }
        public string? Template { get; set; }
        public DateTime? CreatedDate { get; set; }
        public DateTime? LastBootTime { get; set; }
        public DateTime? LastSuspendTime { get; set; }
        
        // Snapshots
        public bool HasSnapshots { get; set; }
        public int? SnapshotCount { get; set; }
        public DateTime? LastSnapshotDate { get; set; }
        
        // Performance (optional)
        public double? CpuUsagePercent { get; set; }
        public double? MemoryUsagePercent { get; set; }
        public double? NetworkUsageKBps { get; set; }
        public double? DiskUsageKBps { get; set; }
        
        // Sync information
        public DateTime LastSyncDate { get; set; } = DateTime.UtcNow;
        public string? SyncStatus { get; set; }
        public string? SyncError { get; set; }
    }
}