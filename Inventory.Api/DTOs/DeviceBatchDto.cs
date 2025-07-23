using Inventory.Domain.Entities;

namespace Inventory.Api.DTOs
{
    public class DeviceBatchDto
    {
        public string Name { get; set; } = "";
        public string? MacAddress { get; set; }
        public string? IpAddress { get; set; }
        public DeviceType DeviceType { get; set; } = DeviceType.Unknown;
        public string? Model { get; set; }
        public string? Location { get; set; }
        public DeviceHardwareInfo? HardwareInfo { get; set; }
        public DeviceSoftwareInfo? SoftwareInfo { get; set; }
    }
}