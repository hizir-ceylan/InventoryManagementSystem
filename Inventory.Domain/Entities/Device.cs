using System;
using System.Collections.Generic;

namespace Inventory.Domain.Entities
{
    public class Device
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string MacAddress { get; set; }
        public string IpAddress { get; set; }
        public DeviceType DeviceType { get; set; }
        public string Model { get; set; }
        public string Location { get; set; }
        public int Status { get; set; }
        public List<ChangeLog> ChangeLogs { get; set; }
        public DeviceHardwareInfo HardwareInfo { get; set; }
        public DeviceSoftwareInfo SoftwareInfo { get; set; }
        
        // Agent/Agentless distinction fields
        public bool AgentInstalled { get; set; }
        public ManagementType ManagementType { get; set; }
        public DiscoveryMethod DiscoveryMethod { get; set; }
        public DateTime? LastSeen { get; set; }
    }
}