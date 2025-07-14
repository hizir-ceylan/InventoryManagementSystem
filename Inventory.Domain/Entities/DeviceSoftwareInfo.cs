namespace Inventory.Domain.Entities
{
    public class DeviceSoftwareInfo
    {
        public string OperatingSystem { get; set; }
        public string OsVersion { get; set; }
        public string[] InstalledApps { get; set; }
        public string[] Updates { get; set; }
        public string[] Users { get; set; }
        public string ActiveUser { get; set; }
    }
}