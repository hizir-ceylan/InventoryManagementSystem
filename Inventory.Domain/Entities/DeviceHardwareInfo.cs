using System.Collections.Generic;

namespace Inventory.Domain.Entities
{
    public class DeviceHardwareInfo
    {
        public string? Cpu { get; set; }
        public int CpuCores { get; set; }
        public int CpuLogical { get; set; }
        public int CpuClockMHz { get; set; }
        public string? Motherboard { get; set; }
        public string? MotherboardSerial { get; set; }
        public string? BiosManufacturer { get; set; }
        public string? BiosVersion { get; set; }
        public string? BiosSerial { get; set; }
        public int RamGB { get; set; }
        public List<RamModule>? RamModules { get; set; }
        public int DiskGB { get; set; }
        public List<DiskInfo>? Disks { get; set; }
        public List<GpuInfo>? Gpus { get; set; }
        public List<NetworkAdapter>? NetworkAdapters { get; set; }
    }

    public class RamModule
    {
        public int Id { get; set; } // EF Core owned entity için gerekli anahtar
        public string? Slot { get; set; }
        public double CapacityGB { get; set; }
        public string? SpeedMHz { get; set; }
        public string? Manufacturer { get; set; }
        public string? PartNumber { get; set; }
        public string? SerialNumber { get; set; }
    }

    public class DiskInfo
    {
        public int Id { get; set; } // EF Core owned entity için gerekli anahtar
        public string? DeviceId { get; set; }
        public double TotalGB { get; set; }
        public double FreeGB { get; set; }
    }

    public class GpuInfo
    {
        public int Id { get; set; } // EF Core owned entity için gerekli anahtar
        public string? Name { get; set; }
        public float? MemoryGB { get; set; }
    }

    public class NetworkAdapter
    {
        public int Id { get; set; } // EF Core owned entity için gerekli anahtar
        public string? Description { get; set; }
        public string? MacAddress { get; set; }
        public string? IpAddress { get; set; }
    }
}