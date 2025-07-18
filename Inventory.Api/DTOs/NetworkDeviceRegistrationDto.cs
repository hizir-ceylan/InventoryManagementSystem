using Inventory.Domain.Entities;

namespace Inventory.Api.DTOs
{
    public class NetworkDeviceRegistrationDto
    {
        public string? Name { get; set; }
        public string? IpAddress { get; set; }
        public string? MacAddress { get; set; }
        public DeviceType DeviceType { get; set; }
        public string? Model { get; set; }
        public string? Location { get; set; }
        public ManagementType ManagementType { get; set; }
        public bool AgentInstalled { get; set; }
    }
}