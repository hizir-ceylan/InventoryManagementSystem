namespace Inventory.Domain.Entities
{
    public class DeviceHardwareInfo
    {
        public string Cpu { get; set; }
        public int RamGB { get; set; }
        public int DiskGB { get; set; }
        public string Motherboard { get; set; }
        public string Gpu { get; set; }
    }
}