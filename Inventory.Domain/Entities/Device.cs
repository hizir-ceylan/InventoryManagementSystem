using System;
using System.Collections.Generic;

namespace Inventory.Domain.Entities
{
    public class Device
    {
        public Guid Id { get; set; }
        public string? Name { get; set; }
        public string? MacAddress { get; set; }
        public string? IpAddress { get; set; }
        public DeviceType DeviceType { get; set; }
        public string? Model { get; set; }
        public string? Location { get; set; }
        public int Status { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public List<ChangeLog>? ChangeLogs { get; set; }
        public DeviceHardwareInfo? HardwareInfo { get; set; }
        public DeviceSoftwareInfo? SoftwareInfo { get; set; }
        
        // Manual fields (not affected by automatic processes)
        public string? BarcodeNumber { get; set; }
        public string? Notes { get; set; }
        
        // Agent/Agentless distinction fields
        public bool AgentInstalled { get; set; }
        public ManagementType ManagementType { get; set; }
        public DiscoveryMethod DiscoveryMethod { get; set; }
        public DateTime? LastSeen { get; set; }
        public DateTime? LastUpdate { get; set; }
        
        // VMware specific fields
        public bool IsVirtual { get; set; }
        public VMwareInfo? VMwareInfo { get; set; }
        
        // Helper property to check if device is a virtual machine
        public bool IsVirtualMachine => DeviceType == DeviceType.VirtualMachine || IsVirtual;
    }
}