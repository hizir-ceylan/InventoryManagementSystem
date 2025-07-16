using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Management;
using LibreHardwareMonitor.Hardware;
using Inventory.Agent.Windows.Models;

namespace Inventory.Agent.Windows
{
    public static class CrossPlatformSystemInfo
    {
        public static bool IsWindows => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        public static bool IsLinux => RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
        public static bool IsMacOS => RuntimeInformation.IsOSPlatform(OSPlatform.OSX);

        public static DeviceDto GatherSystemInformation()
        {
            if (IsWindows)
            {
                return GatherWindowsSystemInfo();
            }
            else if (IsLinux)
            {
                return GatherLinuxSystemInfo();
            }
            else
            {
                throw new PlatformNotSupportedException($"Platform {RuntimeInformation.OSDescription} is not supported yet.");
            }
        }

        private static DeviceDto GatherWindowsSystemInfo()
        {
            // Mevcut Windows kodu buraya taşınacak
            return GatherWindowsSystemInfoInternal();
        }

        private static DeviceDto GatherLinuxSystemInfo()
        {
            var device = new DeviceDto
            {
                Name = Environment.MachineName,
                DeviceType = "PC",
                Location = "Unknown",
                Status = 1,
                ChangeLogs = new List<ChangeLogDto>
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
                }
            };

            // Linux için sistem bilgilerini topla
            var hardwareInfo = GatherLinuxHardwareInfo();
            var softwareInfo = GatherLinuxSoftwareInfo();

            device.HardwareInfo = hardwareInfo;
            device.SoftwareInfo = softwareInfo;

            // MAC ve IP adreslerini ayarla
            var networkInfo = GetLinuxNetworkInfo();
            device.MacAddress = networkInfo.MacAddress;
            device.IpAddress = networkInfo.IpAddress;

            return device;
        }

        private static DeviceHardwareInfoDto GatherLinuxHardwareInfo()
        {
            var hardware = new DeviceHardwareInfoDto();

            try
            {
                // CPU bilgileri
                var cpuInfo = GetLinuxCpuInfo();
                hardware.Cpu = cpuInfo.Name;
                hardware.CpuCores = cpuInfo.Cores;
                hardware.CpuLogical = cpuInfo.LogicalCores;
                hardware.CpuClockMHz = cpuInfo.ClockMHz;

                // RAM bilgileri
                var memInfo = GetLinuxMemoryInfo();
                hardware.RamGB = memInfo.TotalGB;
                hardware.RamModules = memInfo.Modules;

                // Disk bilgileri
                var diskInfo = GetLinuxDiskInfo();
                hardware.DiskGB = diskInfo.TotalGB;
                hardware.Disks = diskInfo.Disks;

                // Network adaptörleri
                hardware.NetworkAdapters = GetLinuxNetworkAdapters();

                // GPU bilgileri (basit implementasyon)
                hardware.Gpus = GetLinuxGpuInfo();

                // BIOS ve Motherboard bilgileri
                var systemInfo = GetLinuxSystemInfo();
                hardware.BiosManufacturer = systemInfo.BiosManufacturer;
                hardware.BiosVersion = systemInfo.BiosVersion;
                hardware.Motherboard = systemInfo.Motherboard;
                hardware.MotherboardSerial = systemInfo.MotherboardSerial;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Linux donanım bilgisi toplama hatası: {ex.Message}");
            }

            return hardware;
        }

        private static DeviceSoftwareInfoDto GatherLinuxSoftwareInfo()
        {
            var software = new DeviceSoftwareInfoDto();

            try
            {
                // OS bilgileri
                var osInfo = GetLinuxOsInfo();
                software.OperatingSystem = osInfo.Name;
                software.OsVersion = osInfo.Version;
                software.OsArchitecture = RuntimeInformation.OSArchitecture.ToString();
                software.RegisteredUser = Environment.UserName;
                software.ActiveUser = Environment.UserName;

                // Yüklü paketler
                software.InstalledApps = GetLinuxInstalledPackages();

                // Kullanıcılar
                software.Users = GetLinuxUsers();

                // Updates (basit implementasyon)
                software.Updates = new List<string>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Linux yazılım bilgisi toplama hatası: {ex.Message}");
                software.InstalledApps = new List<string> { "Paket listesi alınamadı." };
                software.Users = new List<string> { Environment.UserName };
                software.Updates = new List<string>();
            }

            return software;
        }

        private static (string Name, int Cores, int LogicalCores, int ClockMHz) GetLinuxCpuInfo()
        {
            string name = "Unknown CPU";
            int cores = Environment.ProcessorCount;
            int logicalCores = Environment.ProcessorCount;
            int clockMHz = 0;

            try
            {
                if (File.Exists("/proc/cpuinfo"))
                {
                    var lines = File.ReadAllLines("/proc/cpuinfo");
                    foreach (var line in lines)
                    {
                        if (line.StartsWith("model name") && name == "Unknown CPU")
                        {
                            var parts = line.Split(':');
                            if (parts.Length > 1)
                                name = parts[1].Trim();
                        }
                        else if (line.StartsWith("cpu MHz") && clockMHz == 0)
                        {
                            var parts = line.Split(':');
                            if (parts.Length > 1 && double.TryParse(parts[1].Trim(), out double mhz))
                                clockMHz = (int)mhz;
                        }
                    }
                }
            }
            catch
            {
                // Varsayılan değerleri kullan
            }

            return (name, cores, logicalCores, clockMHz);
        }

        private static (int TotalGB, List<RamModuleDto> Modules) GetLinuxMemoryInfo()
        {
            int totalGB = 0;
            var modules = new List<RamModuleDto>();

            try
            {
                if (File.Exists("/proc/meminfo"))
                {
                    var lines = File.ReadAllLines("/proc/meminfo");
                    foreach (var line in lines)
                    {
                        if (line.StartsWith("MemTotal:"))
                        {
                            var parts = line.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                            if (parts.Length > 1 && long.TryParse(parts[1], out long kb))
                            {
                                totalGB = (int)(kb / (1024 * 1024));
                                modules.Add(new RamModuleDto
                                {
                                    Slot = "DIMM0",
                                    CapacityGB = totalGB,
                                    SpeedMHz = "Unknown",
                                    Manufacturer = "Unknown",
                                    PartNumber = "Unknown",
                                    SerialNumber = "Unknown"
                                });
                            }
                            break;
                        }
                    }
                }
            }
            catch
            {
                totalGB = 4; // Varsayılan değer
                modules.Add(new RamModuleDto
                {
                    Slot = "DIMM0",
                    CapacityGB = totalGB,
                    SpeedMHz = "Unknown",
                    Manufacturer = "Unknown",
                    PartNumber = "Unknown",
                    SerialNumber = "Unknown"
                });
            }

            return (totalGB, modules);
        }

        private static (int TotalGB, List<DiskInfoDto> Disks) GetLinuxDiskInfo()
        {
            int totalGB = 0;
            var disks = new List<DiskInfoDto>();

            try
            {
                var drives = DriveInfo.GetDrives();
                foreach (var drive in drives)
                {
                    if (drive.IsReady && drive.DriveType == DriveType.Fixed)
                    {
                        double totalGBDisk = Math.Round(drive.TotalSize / (1024.0 * 1024.0 * 1024.0), 2);
                        double freeGBDisk = Math.Round(drive.TotalFreeSpace / (1024.0 * 1024.0 * 1024.0), 2);
                        
                        totalGB += (int)totalGBDisk;
                        
                        disks.Add(new DiskInfoDto
                        {
                            DeviceId = drive.Name,
                            TotalGB = totalGBDisk,
                            FreeGB = freeGBDisk
                        });
                    }
                }
            }
            catch
            {
                // Varsayılan değer
                disks.Add(new DiskInfoDto
                {
                    DeviceId = "/",
                    TotalGB = 100,
                    FreeGB = 50
                });
                totalGB = 100;
            }

            return (totalGB, disks);
        }

        private static List<NetworkAdapterDto> GetLinuxNetworkAdapters()
        {
            var adapters = new List<NetworkAdapterDto>();

            try
            {
                // ip addr show komutu kullanılabilir ancak şimdilik basit implementation
                adapters.Add(new NetworkAdapterDto
                {
                    Description = "Unknown Network Adapter",
                    MacAddress = "00:00:00:00:00:00",
                    IpAddress = "127.0.0.1"
                });
            }
            catch
            {
                adapters.Add(new NetworkAdapterDto
                {
                    Description = "Unknown Network Adapter",
                    MacAddress = "00:00:00:00:00:00",
                    IpAddress = "127.0.0.1"
                });
            }

            return adapters;
        }

        private static (string MacAddress, string IpAddress) GetLinuxNetworkInfo()
        {
            // Basit implementasyon - gerçek network bilgilerini almak için daha gelişmiş yöntemler kullanılabilir
            return ("00:00:00:00:00:00", "127.0.0.1");
        }

        private static List<GpuInfoDto> GetLinuxGpuInfo()
        {
            var gpus = new List<GpuInfoDto>();

            try
            {
                // lspci komutu veya /proc/driver/nvidia/gpus gibi yöntemler kullanılabilir
                gpus.Add(new GpuInfoDto
                {
                    Name = "Unknown GPU",
                    MemoryGB = null
                });
            }
            catch
            {
                gpus.Add(new GpuInfoDto
                {
                    Name = "Unknown GPU",
                    MemoryGB = null
                });
            }

            return gpus;
        }

        private static (string BiosManufacturer, string BiosVersion, string Motherboard, string MotherboardSerial) GetLinuxSystemInfo()
        {
            string biosManufacturer = "Unknown";
            string biosVersion = "Unknown";
            string motherboard = "Unknown";
            string motherboardSerial = "Unknown";

            try
            {
                // dmidecode komutu kullanılabilir ancak root yetkisi gerekebilir
                // Şimdilik varsayılan değerler
            }
            catch
            {
                // Varsayılan değerler
            }

            return (biosManufacturer, biosVersion, motherboard, motherboardSerial);
        }

        private static (string Name, string Version) GetLinuxOsInfo()
        {
            string name = "Linux";
            string version = "Unknown";

            try
            {
                if (File.Exists("/etc/os-release"))
                {
                    var lines = File.ReadAllLines("/etc/os-release");
                    foreach (var line in lines)
                    {
                        if (line.StartsWith("PRETTY_NAME="))
                        {
                            name = line.Substring(12).Trim('"');
                        }
                        else if (line.StartsWith("VERSION="))
                        {
                            version = line.Substring(8).Trim('"');
                        }
                    }
                }
            }
            catch
            {
                // Varsayılan değerler
            }

            return (name, version);
        }

        private static List<string> GetLinuxInstalledPackages()
        {
            var packages = new List<string>();

            try
            {
                // dpkg, rpm, pacman gibi paket yöneticilerini kontrol et
                // Şimdilik basit bir implementasyon
                packages.Add("Package listing requires specific package manager support");
            }
            catch
            {
                packages.Add("Package listing not available");
            }

            return packages;
        }

        private static List<string> GetLinuxUsers()
        {
            var users = new List<string>();

            try
            {
                if (File.Exists("/etc/passwd"))
                {
                    var lines = File.ReadAllLines("/etc/passwd");
                    foreach (var line in lines)
                    {
                        var parts = line.Split(':');
                        if (parts.Length > 0 && !parts[0].StartsWith("#"))
                        {
                            // Sadece normal kullanıcıları ekle (UID >= 1000)
                            if (parts.Length > 2 && int.TryParse(parts[2], out int uid) && uid >= 1000)
                            {
                                users.Add(parts[0]);
                            }
                        }
                    }
                }
            }
            catch
            {
                users.Add(Environment.UserName);
            }

            if (users.Count == 0)
                users.Add(Environment.UserName);

            return users;
        }

        // Windows sistem bilgilerini toplamak için mevcut kodu kullan
        private static DeviceDto GatherWindowsSystemInfoInternal()
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
            try
            {
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
                computer.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GPU bilgisi alınamadı: {ex.Message}");
                gpuInfos.Add(new GpuInfoDto { Name = "Unknown GPU", MemoryGB = null });
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

            return device;
        }
    }
}