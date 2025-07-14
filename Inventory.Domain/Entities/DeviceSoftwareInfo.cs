using System.Collections.Generic;

namespace Inventory.Domain.Entities
{
    public class DeviceSoftwareInfo
    {
        public string OperatingSystem { get; set; }
        public string OsVersion { get; set; }
        public string OsArchitecture { get; set; }
        public string RegisteredUser { get; set; }
        public string SerialNumber { get; set; }
        public List<string> InstalledApps { get; set; }
        public List<string> Updates { get; set; }
        public List<string> Users { get; set; }
        public string ActiveUser { get; set; }
    }
}