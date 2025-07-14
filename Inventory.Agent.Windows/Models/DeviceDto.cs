using System.Collections.Generic;

namespace Inventory.Agent.Windows.Models
{
    public class DeviceDto
    {
        public string Name { get; set; }
        public string MacAddress { get; set; }
        public string IpAddress { get; set; }
        public string DeviceType { get; set; }
        public string Model { get; set; }
        public string Location { get; set; }
        public int Status { get; set; }
        public List<ChangeLogDto> ChangeLogs { get; set; }
        public DeviceHardwareInfoDto HardwareInfo { get; set; }
        public DeviceSoftwareInfoDto SoftwareInfo { get; set; }
    }
}