using System;
using System.Management;
using System.Threading.Tasks;
using Inventory.Agent.Windows.Models;
using System.Collections.Generic;
using LibreHardwareMonitor.Hardware;

namespace Inventory.Agent.Windows
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // --- İşletim Sistemi Bilgileri ---
            string osCaption = "";
            string osVersion = "";
            string osArchitecture = "";
            string osRegisteredUser = "";
            string osSerialNumber = "";
            var osSearcher = new ManagementObjectSearcher("select * from Win32_OperatingSystem");
            foreach (ManagementObject obj in osSearcher.Get())
            {
                osCaption = obj["Caption"]?.ToString() ?? "";
                osVersion = obj["Version"]?.ToString() ?? "";
                osArchitecture = obj["OSArchitecture"]?.ToString() ?? "";
                osRegisteredUser = obj["RegisteredUser"]?.ToString() ?? "";
                osSerialNumber = obj["SerialNumber"]?.ToString() ?? "";
            }

            // --- BIOS Bilgileri ---
            string biosManufacturer = "";
            string biosVersion = "";
            string biosSerial = "";
            var biosSearcher = new ManagementObjectSearcher("select * from Win32_BIOS");
            foreach (ManagementObject obj in biosSearcher.Get())
            {
                biosManufacturer = obj["Manufacturer"]?.ToString() ?? "";
                biosVersion = obj["SMBIOSBIOSVersion"]?.ToString() ?? "";
                biosSerial = obj["SerialNumber"]?.ToString() ?? "";
            }

            // --- Anakart Bilgileri ---
            string motherboardManufacturer = "";
            string motherboardProduct = "";
            string motherboardModel = "";
            string motherboardSerial = "";
            var boardSearcher = new ManagementObjectSearcher("select * from Win32_BaseBoard");
            foreach (ManagementObject obj in boardSearcher.Get())
            {
                motherboardManufacturer = obj["Manufacturer"]?.ToString() ?? "";
                motherboardProduct = obj["Product"]?.ToString() ?? "";
                motherboardModel = $"{motherboardManufacturer} {motherboardProduct}";
                motherboardSerial = obj["SerialNumber"]?.ToString() ?? "";
            }

            // --- CPU Bilgileri ---
            string cpuName = "";
            int cpuCores = 0;
            int cpuLogical = 0;
            int cpuClock = 0;
            var cpuSearcher = new ManagementObjectSearcher("select * from Win32_Processor");
            foreach (ManagementObject obj in cpuSearcher.Get())
            {
                cpuName = obj["Name"]?.ToString() ?? "";
                cpuCores = Convert.ToInt32(obj["NumberOfCores"] ?? 0);
                cpuLogical = Convert.ToInt32(obj["NumberOfLogicalProcessors"] ?? 0);
                cpuClock = Convert.ToInt32(obj["MaxClockSpeed"] ?? 0);
            }

            // --- RAM Slotları ve Toplam RAM ---
            int totalRam = 0;
            var ramModules = new List<RamModuleDto>();
            var ramSearcher = new ManagementObjectSearcher("SELECT * FROM Win32_PhysicalMemory");
            foreach (ManagementObject obj in ramSearcher.Get())
            {
                double capacity = Math.Round(Convert.ToDouble(obj["Capacity"]) / (1024 * 1024 * 1024), 2);
                totalRam += (int)capacity;
                ramModules.Add(new RamModuleDto
                {
                    Slot = obj["BankLabel"]?.ToString() ?? "",
                    CapacityGB = capacity,
                    SpeedMHz = obj["Speed"]?.ToString() ?? "",
                    Manufacturer = obj["Manufacturer"]?.ToString() ?? "",
                    PartNumber = obj["PartNumber"]?.ToString() ?? "",
                    SerialNumber = obj["SerialNumber"]?.ToString() ?? ""
                });
            }

            // --- Diskler Detaylı ---
            int totalDisk = 0;
            var diskInfos = new List<DiskInfoDto>();
            var diskSearcher = new ManagementObjectSearcher("select * from Win32_LogicalDisk where DriveType=3");
            foreach (ManagementObject obj in diskSearcher.Get())
            {
                double size = Math.Round(Convert.ToDouble(obj["Size"]) / (1024 * 1024 * 1024), 2);
                double freeSpace = Math.Round(Convert.ToDouble(obj["FreeSpace"]) / (1024 * 1024 * 1024), 2);
                totalDisk += (int)size;
                diskInfos.Add(new DiskInfoDto
                {
                    DeviceId = obj["DeviceID"]?.ToString() ?? "",
                    TotalGB = size,
                    FreeGB = freeSpace
                });
            }

            // --- GPU'lar (Hepsi) ---
            var gpuInfos = new List<GpuInfoDto>();
            var computer = new Computer { IsGpuEnabled = true };
            computer.Open();
            foreach (var hardware in computer.Hardware)
            {
                if (hardware.HardwareType == HardwareType.GpuNvidia || hardware.HardwareType == HardwareType.GpuAmd)
                {
                    hardware.Update();
                    string gpuName = hardware.Name;
                    float? totalMemory = null;
                    foreach (var sensor in hardware.Sensors)
                    {
                        if (sensor.SensorType == SensorType.SmallData &&
                            (sensor.Name.Contains("Memory Total") || sensor.Name.Contains("GPU Memory Total")))
                        {
                            totalMemory = sensor.Value;
                            break;
                        }
                    }
                    gpuInfos.Add(new GpuInfoDto
                    {
                        Name = gpuName,
                        MemoryGB = totalMemory.HasValue ? (totalMemory.Value / 1024) : (float?)null
                    });
                }
            }

            // --- Ağ Adaptörleri (Hepsi) ---
            var netAdapters = new List<NetworkAdapterDto>();
            string macAddress = "";
            string ipAddress = "";
            var netSearcher = new ManagementObjectSearcher("select * from Win32_NetworkAdapterConfiguration where IPEnabled=true");
            foreach (ManagementObject obj in netSearcher.Get())
            {
                string[] ipAddresses = obj["IPAddress"] as string[];
                string ip = ipAddresses != null && ipAddresses.Length > 0 ? ipAddresses[0] : "";
                string mac = obj["MACAddress"]?.ToString() ?? "";
                netAdapters.Add(new NetworkAdapterDto
                {
                    Description = obj["Description"]?.ToString() ?? "",
                    MacAddress = mac,
                    IpAddress = ip
                });
                if (string.IsNullOrEmpty(macAddress) && !string.IsNullOrEmpty(mac))
                    macAddress = mac;
                if (string.IsNullOrEmpty(ipAddress) && !string.IsNullOrEmpty(ip))
                    ipAddress = ip;
            }

            // --- Yüklü Yazılımlar (TAMAMI) ---
            var installedApps = new List<string>();
            try
            {
                var softwareSearcher = new ManagementObjectSearcher("SELECT * FROM Win32_Product");
                foreach (ManagementObject obj in softwareSearcher.Get())
                {
                    installedApps.Add($"{obj["Name"]} {obj["Version"]}");
                }
            }
            catch (Exception)
            {
                installedApps.Add("Yüklü yazılım listesi alınamadı.");
            }

            // --- ChangeLogs (örnek) ---
            var changeLogs = new List<ChangeLogDto>
            {
                new ChangeLogDto
                {
                    Id = Guid.NewGuid(),
                    ChangeDate = DateTime.UtcNow,
                    ChangeType = "InitialRegistration",
                    OldValue = "",
                    NewValue = "Kaydedildi",
                    ChangedBy = Environment.UserName
                }
            };

            // --- Hardware ve Software Info nesneleri (detaylı) ---
            var hardwareInfo = new DeviceHardwareInfoDto
            {
                Cpu = cpuName,
                CpuCores = cpuCores,
                CpuLogical = cpuLogical,
                CpuClockMHz = cpuClock,
                Motherboard = motherboardModel,
                MotherboardSerial = motherboardSerial,
                BiosManufacturer = biosManufacturer,
                BiosVersion = biosVersion,
                BiosSerial = biosSerial,
                RamGB = totalRam,
                RamModules = ramModules,
                DiskGB = totalDisk,
                Disks = diskInfos,
                Gpus = gpuInfos,
                NetworkAdapters = netAdapters
            };

            var softwareInfoDto = new DeviceSoftwareInfoDto
            {
                OperatingSystem = osCaption,
                OsVersion = osVersion,
                OsArchitecture = osArchitecture,
                RegisteredUser = osRegisteredUser,
                SerialNumber = osSerialNumber,
                InstalledApps = installedApps,
                Updates = new List<string>(), // İstersen update bilgisini de ekleyebilirsin
                Users = new List<string> { Environment.UserName },
                ActiveUser = Environment.UserName
            };

            // --- DeviceDto nesnesini hazırla ---
            var device = new DeviceDto
            {
                Name = Environment.MachineName,
                MacAddress = macAddress,
                IpAddress = ipAddress,
                DeviceType = "PC",
                Model = motherboardModel,
                Location = "Ev",
                Status = 1,
                ChangeLogs = changeLogs,
                HardwareInfo = hardwareInfo,
                SoftwareInfo = softwareInfoDto
            };

            // --- Cihazı logla ---
            DeviceLogger.LogDevice(device);

            // --- API'ye gönder ---
            string apiUrl = "https://localhost:7296/api/device";
            bool success = await ApiClient.PostDeviceAsync(device, apiUrl);
            Console.WriteLine(success ? "Cihaz başarıyla API'ye gönderildi!" : "Gönderim başarısız.");

            Console.WriteLine("\nİşlem tamamlandı. Çıkmak için bir tuşa basın.");
            Console.ReadKey();
        }
    }
}