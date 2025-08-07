using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Management;
using LibreHardwareMonitor.Hardware;
using Inventory.Agent.Windows.Models;
using System.Linq;
using Inventory.Domain.Entities;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.Win32;
using Inventory.Shared.Utils;

namespace Inventory.Agent.Windows
{
    public static class CrossPlatformSystemInfo
    {
        public static bool IsWindows => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        public static bool IsLinux => RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
        public static bool IsMacOS => RuntimeInformation.IsOSPlatform(OSPlatform.OSX);

        public static DeviceDto GatherSystemInformation()
        {
            var stopwatch = Stopwatch.StartNew();
            Console.WriteLine("Starting system information gathering...");
            
            DeviceDto result;
            if (IsWindows)
            {
                result = GatherWindowsSystemInfo();
            }
            else if (IsLinux)
            {
                result = GatherLinuxSystemInfo();
            }
            else
            {
                throw new PlatformNotSupportedException($"Platform {RuntimeInformation.OSDescription} is not supported yet.");
            }
            
            stopwatch.Stop();
            Console.WriteLine($"System information gathering completed in {stopwatch.ElapsedMilliseconds}ms");
            return result;
        }

        private static DeviceDto GatherWindowsSystemInfo()
        {
            // Execute this method asynchronously with performance monitoring
            return Task.Run(async () => await GatherWindowsSystemInfoAsync()).Result;
        }

        private static async Task<DeviceDto> GatherWindowsSystemInfoAsync()
        {
            var overallStopwatch = Stopwatch.StartNew();
            Console.WriteLine("Starting Windows system information gathering...");

            // Execute all major information gathering tasks in parallel
            var tasks = new List<Task>();
            
            // Shared variables to be populated by parallel tasks
            string osCaption = "", osVersion = "", osArchitecture = "", osRegisteredUser = "", osSerialNumber = "";
            string biosManufacturer = "", biosVersion = "", biosSerial = "";
            string motherboardManufacturer = "", motherboardProduct = "", motherboardModel = "", motherboardSerial = "";
            string cpuName = "";
            int cpuCores = 0, cpuLogical = 0, cpuClock = 0;
            int totalRam = 0;
            var ramModules = new List<RamModuleDto>();
            int totalDisk = 0;
            var diskInfos = new List<DiskInfoDto>();
            var gpuInfos = new List<GpuInfoDto>();
            var netAdapters = new List<NetworkAdapterDto>();
            string macAddress = "", ipAddress = "";
            var installedApps = new List<string>();

            // Task 1: Operating System Information
            tasks.Add(Task.Run(() =>
            {
                var sw = Stopwatch.StartNew();
                try
                {
                    using var osSearcher = new ManagementObjectSearcher("select Caption,Version,OSArchitecture,RegisteredUser,SerialNumber from Win32_OperatingSystem");
                    foreach (ManagementObject obj in osSearcher.Get())
                    {
                        osCaption = obj["Caption"]?.ToString() ?? "";
                        osVersion = obj["Version"]?.ToString() ?? "";
                        osArchitecture = obj["OSArchitecture"]?.ToString() ?? "";
                        osRegisteredUser = obj["RegisteredUser"]?.ToString() ?? "";
                        osSerialNumber = obj["SerialNumber"]?.ToString() ?? "";
                        break; // Only need first result
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error gathering OS info: {ex.Message}");
                }
                Console.WriteLine($"OS info gathered in {sw.ElapsedMilliseconds}ms");
            }));

            // Task 2: BIOS Information
            tasks.Add(Task.Run(() =>
            {
                var sw = Stopwatch.StartNew();
                try
                {
                    using var biosSearcher = new ManagementObjectSearcher("select Manufacturer,SMBIOSBIOSVersion,SerialNumber from Win32_BIOS");
                    foreach (ManagementObject obj in biosSearcher.Get())
                    {
                        biosManufacturer = obj["Manufacturer"]?.ToString() ?? "";
                        biosVersion = obj["SMBIOSBIOSVersion"]?.ToString() ?? "";
                        biosSerial = obj["SerialNumber"]?.ToString() ?? "";
                        break; // Only need first result
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error gathering BIOS info: {ex.Message}");
                }
                Console.WriteLine($"BIOS info gathered in {sw.ElapsedMilliseconds}ms");
            }));

            // Task 3: Motherboard Information
            tasks.Add(Task.Run(() =>
            {
                var sw = Stopwatch.StartNew();
                try
                {
                    using var boardSearcher = new ManagementObjectSearcher("select Manufacturer,Product,SerialNumber from Win32_BaseBoard");
                    foreach (ManagementObject obj in boardSearcher.Get())
                    {
                        motherboardManufacturer = obj["Manufacturer"]?.ToString() ?? "";
                        motherboardProduct = obj["Product"]?.ToString() ?? "";
                        motherboardModel = $"{motherboardManufacturer} {motherboardProduct}";
                        motherboardSerial = obj["SerialNumber"]?.ToString() ?? "";
                        break; // Only need first result
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error gathering motherboard info: {ex.Message}");
                }
                Console.WriteLine($"Motherboard info gathered in {sw.ElapsedMilliseconds}ms");
            }));

            // Task 4: CPU Information
            tasks.Add(Task.Run(() =>
            {
                var sw = Stopwatch.StartNew();
                try
                {
                    using var cpuSearcher = new ManagementObjectSearcher("select Name,NumberOfCores,NumberOfLogicalProcessors,MaxClockSpeed from Win32_Processor");
                    foreach (ManagementObject obj in cpuSearcher.Get())
                    {
                        cpuName = obj["Name"]?.ToString() ?? "";
                        cpuCores = Convert.ToInt32(obj["NumberOfCores"] ?? 0);
                        cpuLogical = Convert.ToInt32(obj["NumberOfLogicalProcessors"] ?? 0);
                        cpuClock = Convert.ToInt32(obj["MaxClockSpeed"] ?? 0);
                        break; // Only need first result
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error gathering CPU info: {ex.Message}");
                }
                Console.WriteLine($"CPU info gathered in {sw.ElapsedMilliseconds}ms");
            }));

            // Task 5: RAM Information
            tasks.Add(Task.Run(() =>
            {
                var sw = Stopwatch.StartNew();
                try
                {
                    using var ramSearcher = new ManagementObjectSearcher("SELECT Capacity,BankLabel,Speed,Manufacturer,PartNumber,SerialNumber FROM Win32_PhysicalMemory");
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
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error gathering RAM info: {ex.Message}");
                }
                Console.WriteLine($"RAM info gathered in {sw.ElapsedMilliseconds}ms");
            }));

            // Task 6: Disk Information
            tasks.Add(Task.Run(() =>
            {
                var sw = Stopwatch.StartNew();
                try
                {
                    using var diskSearcher = new ManagementObjectSearcher("select DeviceID,Size,FreeSpace from Win32_LogicalDisk where DriveType=3");
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
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error gathering disk info: {ex.Message}");
                }
                Console.WriteLine($"Disk info gathered in {sw.ElapsedMilliseconds}ms");
            }));

            // Task 7: Network Information
            tasks.Add(Task.Run(() =>
            {
                var sw = Stopwatch.StartNew();
                try
                {
                    using var netSearcher = new ManagementObjectSearcher("select Description,MACAddress,IPAddress from Win32_NetworkAdapterConfiguration where IPEnabled=true");
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
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error gathering network info: {ex.Message}");
                }
                Console.WriteLine($"Network info gathered in {sw.ElapsedMilliseconds}ms");
            }));

            // Task 8: GPU Information (using LibreHardwareMonitor)
            tasks.Add(Task.Run(() =>
            {
                var sw = Stopwatch.StartNew();
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
                    Console.WriteLine($"GPU information collection failed: {ex.Message}");
                    gpuInfos.Add(new GpuInfoDto { Name = "Unknown GPU", MemoryGB = null });
                }
                Console.WriteLine($"GPU info gathered in {sw.ElapsedMilliseconds}ms");
            }));

            // Task 9: Installed Software (using faster registry method instead of Win32_Product)
            tasks.Add(Task.Run(() =>
            {
                var sw = Stopwatch.StartNew();
                try
                {
                    installedApps = GetInstalledSoftwareFast();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error gathering installed software: {ex.Message}");
                    installedApps.Add("Yüklü yazılım listesi alınamadı.");
                }
                Console.WriteLine($"Installed software gathered in {sw.ElapsedMilliseconds}ms");
            }));

            // Wait for all tasks to complete
            await Task.WhenAll(tasks);

            // Create change logs
            var changeLogs = new List<ChangeLogDto>
            {
                new ChangeLogDto
                {
                    Id = Guid.NewGuid(),
                    ChangeDate = TimeZoneHelper.GetTurkeyTime(),
                    ChangeType = "InitialRegistration",
                    OldValue = "",
                    NewValue = "Kaydedildi",
                    ChangedBy = Environment.UserName
                }
            };

            // Create hardware and software info objects
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
                Updates = new List<string>(), // Not collecting updates for performance
                Users = new List<string> { Environment.UserName },
                ActiveUser = Environment.UserName
            };

            // Device object oluştur - Agent kurulan cihaz olarak işaretle
            var device = new DeviceDto
            {
                Name = Environment.MachineName,
                MacAddress = macAddress,
                IpAddress = ipAddress,
                DeviceType = DeviceType.Desktop,
                Model = motherboardModel,
                Location = "Ev",
                Status = 1,
                AgentInstalled = true, // Bu cihazda agent yüklü
                ChangeLogs = changeLogs,
                HardwareInfo = hardwareInfo,
                SoftwareInfo = softwareInfoDto
            };

            overallStopwatch.Stop();
            Console.WriteLine($"Windows system information gathering completed in {overallStopwatch.ElapsedMilliseconds}ms");
            return device;
        }

        /// <summary>
        /// Fast method to get installed software using registry instead of slow Win32_Product WMI query
        /// </summary>
        private static List<string> GetInstalledSoftwareFast()
        {
            var installedApps = new List<string>();
            const int maxApps = 300; // Limit to improve performance

            try
            {
                // Check both 32-bit and 64-bit application registry keys
                string[] registryKeys = {
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
                    @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall"
                };

                foreach (string registryKey in registryKeys)
                {
                    using (RegistryKey key = Registry.LocalMachine.OpenSubKey(registryKey))
                    {
                        if (key != null)
                        {
                            foreach (string subkeyName in key.GetSubKeyNames())
                            {
                                if (installedApps.Count >= maxApps) break;

                                try
                                {
                                    using (RegistryKey subkey = key.OpenSubKey(subkeyName))
                                    {
                                        if (subkey != null)
                                        {
                                            var displayName = subkey.GetValue("DisplayName")?.ToString();
                                            var displayVersion = subkey.GetValue("DisplayVersion")?.ToString();
                                            var systemComponent = subkey.GetValue("SystemComponent");

                                            // Skip system components and entries without display names
                                            if (!string.IsNullOrEmpty(displayName) && 
                                                (systemComponent == null || systemComponent.ToString() != "1"))
                                            {
                                                var appInfo = string.IsNullOrEmpty(displayVersion) 
                                                    ? displayName 
                                                    : $"{displayName} {displayVersion}";
                                                
                                                if (!installedApps.Contains(appInfo))
                                                {
                                                    installedApps.Add(appInfo);
                                                }
                                            }
                                        }
                                    }
                                }
                                catch
                                {
                                    // Skip entries that can't be read
                                    continue;
                                }
                            }
                        }
                    }
                    
                    if (installedApps.Count >= maxApps) break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading registry for installed software: {ex.Message}");
                installedApps.Add("Yüklü yazılım listesi kısmen alınabildi.");
            }

            // Sort and limit results
            installedApps.Sort();
            return installedApps.Take(maxApps).ToList();
        }

        private static DeviceDto GatherLinuxSystemInfo()
        {
            return Task.Run(async () => await GatherLinuxSystemInfoAsync()).Result;
        }

        private static async Task<DeviceDto> GatherLinuxSystemInfoAsync()
        {
            var overallStopwatch = Stopwatch.StartNew();
            Console.WriteLine("Starting Linux system information gathering...");

            var device = new DeviceDto
            {
                Name = Environment.MachineName,
                DeviceType = DeviceType.Desktop,
                Model = "Unknown Linux System",
                Location = "Unknown",
                Status = 1,
                AgentInstalled = true, // Bu cihazda agent yüklü
                ChangeLogs = new List<ChangeLogDto>
                {
                    new ChangeLogDto
                    {
                        Id = Guid.NewGuid(),
                        ChangeDate = TimeZoneHelper.GetTurkeyTime(),
                        ChangeType = "InitialRegistration",
                        OldValue = "",
                        NewValue = "Kaydedildi",
                        ChangedBy = Environment.UserName
                    }
                }
            };

            // Execute all information gathering tasks in parallel
            var tasks = new List<Task>();
            
            // Shared variables to be populated by parallel tasks
            var hardwareInfo = new DeviceHardwareInfoDto();
            var softwareInfo = new DeviceSoftwareInfoDto();
            var networkInfo = (MacAddress: "", IpAddress: "");

            // Task 1: Hardware Information
            tasks.Add(Task.Run(() =>
            {
                var sw = Stopwatch.StartNew();
                try
                {
                    hardwareInfo = GatherLinuxHardwareInfoOptimized();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error gathering Linux hardware info: {ex.Message}");
                }
                Console.WriteLine($"Linux hardware info gathered in {sw.ElapsedMilliseconds}ms");
            }));

            // Task 2: Software Information
            tasks.Add(Task.Run(() =>
            {
                var sw = Stopwatch.StartNew();
                try
                {
                    softwareInfo = GatherLinuxSoftwareInfoOptimized();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error gathering Linux software info: {ex.Message}");
                    softwareInfo.InstalledApps = new List<string> { "Paket listesi alınamadı." };
                    softwareInfo.Users = new List<string> { Environment.UserName };
                    softwareInfo.Updates = new List<string>();
                    softwareInfo.SerialNumber = "Unknown";
                }
                Console.WriteLine($"Linux software info gathered in {sw.ElapsedMilliseconds}ms");
            }));

            // Task 3: Network Information
            tasks.Add(Task.Run(() =>
            {
                var sw = Stopwatch.StartNew();
                try
                {
                    networkInfo = GetLinuxNetworkInfoOptimized();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error gathering Linux network info: {ex.Message}");
                }
                Console.WriteLine($"Linux network info gathered in {sw.ElapsedMilliseconds}ms");
            }));

            // Task 4: System Model Information
            tasks.Add(Task.Run(() =>
            {
                var sw = Stopwatch.StartNew();
                try
                {
                    var dmiModel = GetLinuxDmiInfo("product_name");
                    if (!string.IsNullOrWhiteSpace(dmiModel))
                    {
                        device.Model = dmiModel;
                    }
                }
                catch
                {
                    // Keep default model if DMI read fails
                }
                Console.WriteLine($"Linux model info gathered in {sw.ElapsedMilliseconds}ms");
            }));

            // Wait for all tasks to complete
            await Task.WhenAll(tasks);

            // Assign the gathered information
            device.HardwareInfo = hardwareInfo;
            device.SoftwareInfo = softwareInfo;
            device.MacAddress = networkInfo.MacAddress;
            device.IpAddress = networkInfo.IpAddress;

            overallStopwatch.Stop();
            Console.WriteLine($"Linux system information gathering completed in {overallStopwatch.ElapsedMilliseconds}ms");
            return device;
        }

        private static DeviceHardwareInfoDto GatherLinuxHardwareInfoOptimized()
        {
            var hardware = new DeviceHardwareInfoDto();
            const int timeoutMs = 5000; // 5 second timeout for external commands

            try
            {
                // Execute all hardware gathering tasks in parallel with timeouts
                var tasks = new List<Task>();

                // CPU information
                tasks.Add(Task.Run(() =>
                {
                    try
                    {
                        var cpuInfo = GetLinuxCpuInfoOptimized();
                        hardware.Cpu = cpuInfo.Name;
                        hardware.CpuCores = cpuInfo.Cores;
                        hardware.CpuLogical = cpuInfo.LogicalCores;
                        hardware.CpuClockMHz = cpuInfo.ClockMHz;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error gathering CPU info: {ex.Message}");
                        hardware.Cpu = "Unknown CPU";
                        hardware.CpuCores = Environment.ProcessorCount;
                        hardware.CpuLogical = Environment.ProcessorCount;
                    }
                }));

                // Memory information
                tasks.Add(Task.Run(() =>
                {
                    try
                    {
                        var memInfo = GetLinuxMemoryInfoOptimized();
                        hardware.RamGB = memInfo.TotalGB;
                        hardware.RamModules = memInfo.Modules;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error gathering memory info: {ex.Message}");
                        hardware.RamGB = 4; // Default fallback
                        hardware.RamModules = new List<RamModuleDto>
                        {
                            new RamModuleDto
                            {
                                Slot = "DIMM0",
                                CapacityGB = 4,
                                SpeedMHz = "Unknown",
                                Manufacturer = "Unknown",
                                PartNumber = "Unknown",
                                SerialNumber = "Unknown"
                            }
                        };
                    }
                }));

                // Disk information
                tasks.Add(Task.Run(() =>
                {
                    try
                    {
                        var diskInfo = GetLinuxDiskInfo();
                        hardware.DiskGB = diskInfo.TotalGB;
                        hardware.Disks = diskInfo.Disks;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error gathering disk info: {ex.Message}");
                        hardware.DiskGB = 100;
                        hardware.Disks = new List<DiskInfoDto>
                        {
                            new DiskInfoDto { DeviceId = "/", TotalGB = 100, FreeGB = 50 }
                        };
                    }
                }));

                // Network adapters
                tasks.Add(Task.Run(() =>
                {
                    try
                    {
                        hardware.NetworkAdapters = GetLinuxNetworkAdaptersOptimized();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error gathering network adapters: {ex.Message}");
                        hardware.NetworkAdapters = new List<NetworkAdapterDto>
                        {
                            new NetworkAdapterDto
                            {
                                Description = "Unknown Network Adapter",
                                MacAddress = "00:00:00:00:00:00",
                                IpAddress = "127.0.0.1"
                            }
                        };
                    }
                }));

                // GPU information
                tasks.Add(Task.Run(() =>
                {
                    try
                    {
                        hardware.Gpus = GetLinuxGpuInfoOptimized();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error gathering GPU info: {ex.Message}");
                        hardware.Gpus = new List<GpuInfoDto>
                        {
                            new GpuInfoDto { Name = "Unknown GPU", MemoryGB = null }
                        };
                    }
                }));

                // System information
                tasks.Add(Task.Run(() =>
                {
                    try
                    {
                        var systemInfo = GetLinuxSystemInfoOptimized();
                        hardware.BiosManufacturer = systemInfo.BiosManufacturer;
                        hardware.BiosVersion = systemInfo.BiosVersion;
                        hardware.BiosSerial = systemInfo.BiosSerial ?? "Unknown";
                        hardware.Motherboard = systemInfo.Motherboard;
                        hardware.MotherboardSerial = systemInfo.MotherboardSerial;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error gathering system info: {ex.Message}");
                        hardware.BiosManufacturer = "Unknown";
                        hardware.BiosVersion = "Unknown";
                        hardware.BiosSerial = "Unknown";
                        hardware.Motherboard = "Unknown";
                        hardware.MotherboardSerial = "Unknown";
                    }
                }));

                // Wait for all tasks with timeout
                if (!Task.WaitAll(tasks.ToArray(), timeoutMs))
                {
                    Console.WriteLine("Warning: Some hardware information gathering tasks timed out");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Linux hardware information gathering error: {ex.Message}");
            }

            return hardware;
        }

        private static DeviceSoftwareInfoDto GatherLinuxSoftwareInfoOptimized()
        {
            var software = new DeviceSoftwareInfoDto();
            const int timeoutMs = 10000; // 10 second timeout for software gathering

            try
            {
                // Execute software gathering tasks in parallel
                var tasks = new List<Task>();

                // OS information
                tasks.Add(Task.Run(() =>
                {
                    try
                    {
                        var osInfo = GetLinuxOsInfoOptimized();
                        software.OperatingSystem = osInfo.Name;
                        software.OsVersion = osInfo.Version;
                        software.OsArchitecture = RuntimeInformation.OSArchitecture.ToString();
                        software.RegisteredUser = Environment.UserName;
                        software.ActiveUser = Environment.UserName;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error gathering OS info: {ex.Message}");
                        software.OperatingSystem = "Linux";
                        software.OsVersion = "Unknown";
                        software.OsArchitecture = RuntimeInformation.OSArchitecture.ToString();
                        software.RegisteredUser = Environment.UserName;
                        software.ActiveUser = Environment.UserName;
                    }
                }));

                // Serial number
                tasks.Add(Task.Run(() =>
                {
                    try
                    {
                        software.SerialNumber = GetLinuxSerialNumberOptimized();
                    }
                    catch
                    {
                        software.SerialNumber = "Unknown";
                    }
                }));

                // Installed packages (with limit for performance)
                tasks.Add(Task.Run(() =>
                {
                    try
                    {
                        software.InstalledApps = GetLinuxInstalledPackagesOptimized();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error gathering installed packages: {ex.Message}");
                        software.InstalledApps = new List<string> { "Package listing not available" };
                    }
                }));

                // Users
                tasks.Add(Task.Run(() =>
                {
                    try
                    {
                        software.Users = GetLinuxUsersOptimized();
                    }
                    catch
                    {
                        software.Users = new List<string> { Environment.UserName };
                    }
                }));

                // Wait for all tasks with timeout
                if (!Task.WaitAll(tasks.ToArray(), timeoutMs))
                {
                    Console.WriteLine("Warning: Some software information gathering tasks timed out");
                }

                // Updates - skip for performance
                software.Updates = new List<string>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Linux software information gathering error: {ex.Message}");
                software.InstalledApps = new List<string> { "Software information not available" };
                software.Users = new List<string> { Environment.UserName };
                software.Updates = new List<string>();
                software.SerialNumber = "Unknown";
            }

            return software;
        }

        private static (string MacAddress, string IpAddress) GetLinuxNetworkInfoOptimized()
        {
            string macAddress = "";
            string ipAddress = "";

            try
            {
                // Try using ip command first with timeout
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                var task = Task.Run(() => 
                {
                    try
                    {
                        var process = new Process
                        {
                            StartInfo = new ProcessStartInfo
                            {
                                FileName = "ip",
                                Arguments = "addr show",
                                RedirectStandardOutput = true,
                                UseShellExecute = false,
                                CreateNoWindow = true
                            }
                        };

                        process.Start();
                        string output = process.StandardOutput.ReadToEnd();
                        process.WaitForExit();

                        if (process.ExitCode == 0)
                        {
                            ParseIpAddrOutput(output, ref macAddress, ref ipAddress);
                        }
                    }
                    catch
                    {
                        // Fallback will be used
                    }
                }, cts.Token);

                try
                {
                    task.Wait(cts.Token);
                }
                catch (OperationCanceledException)
                {
                    Console.WriteLine("ip command timed out, using fallback method");
                }
            }
            catch
            {
                // Continue to fallback
            }

            // Fallback to reading /sys/class/net if ip command fails or times out
            if (string.IsNullOrEmpty(macAddress) || string.IsNullOrEmpty(ipAddress))
            {
                try
                {
                    var interfaces = Directory.GetDirectories("/sys/class/net");
                    foreach (var interfaceDir in interfaces)
                    {
                        var interfaceName = Path.GetFileName(interfaceDir);
                        if (interfaceName == "lo") continue; // Skip loopback

                        if (string.IsNullOrEmpty(macAddress))
                        {
                            var addressPath = Path.Combine(interfaceDir, "address");
                            if (File.Exists(addressPath))
                            {
                                var address = File.ReadAllText(addressPath).Trim();
                                if (!string.IsNullOrEmpty(address) && address != "00:00:00:00:00:00")
                                {
                                    macAddress = address;
                                }
                            }
                        }
                        
                        if (!string.IsNullOrEmpty(macAddress)) break;
                    }

                    // Get IP using hostname command if still empty
                    if (string.IsNullOrEmpty(ipAddress))
                    {
                        try
                        {
                            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
                            var hostnameTask = Task.Run(() =>
                            {
                                var process = new Process
                                {
                                    StartInfo = new ProcessStartInfo
                                    {
                                        FileName = "hostname",
                                        Arguments = "-I",
                                        RedirectStandardOutput = true,
                                        UseShellExecute = false,
                                        CreateNoWindow = true
                                    }
                                };

                                process.Start();
                                string output = process.StandardOutput.ReadToEnd().Trim();
                                process.WaitForExit();

                                if (process.ExitCode == 0 && !string.IsNullOrEmpty(output))
                                {
                                    var ips = output.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                                    if (ips.Length > 0)
                                    {
                                        ipAddress = ips[0];
                                    }
                                }
                            }, cts.Token);

                            hostnameTask.Wait(cts.Token);
                        }
                        catch (OperationCanceledException)
                        {
                            Console.WriteLine("hostname command timed out");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error reading network info from /sys: {ex.Message}");
                }
            }

            // Use fallback values if nothing found
            if (string.IsNullOrEmpty(macAddress)) macAddress = "00:00:00:00:00:00";
            if (string.IsNullOrEmpty(ipAddress)) ipAddress = "127.0.0.1";

            return (macAddress, ipAddress);
        }

        private static void ParseIpAddrOutput(string output, ref string macAddress, ref string ipAddress)
        {
            var lines = output.Split('\n');
            string currentInterface = "";
            
            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();
                
                // Find interface line (contains 'state UP')
                if (trimmedLine.Contains("state UP") && !trimmedLine.Contains("lo:"))
                {
                    var parts = trimmedLine.Split(':');
                    if (parts.Length > 1)
                    {
                        currentInterface = parts[1].Trim();
                    }
                }
                // Find MAC address line
                else if (trimmedLine.StartsWith("link/ether") && !string.IsNullOrEmpty(currentInterface))
                {
                    var parts = trimmedLine.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length > 1 && string.IsNullOrEmpty(macAddress))
                    {
                        macAddress = parts[1];
                    }
                }
                // Find IP address line
                else if (trimmedLine.StartsWith("inet ") && !trimmedLine.Contains("127.0.0.1"))
                {
                    var parts = trimmedLine.Split(new char[] { ' ', '/' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length > 1 && string.IsNullOrEmpty(ipAddress))
                    {
                        ipAddress = parts[1];
                    }
                }
            }
        }

        private static string GetLinuxSerialNumberOptimized()
        {
            try
            {
                // Try to get serial number from DMI (fastest method)
                var serialNumber = GetLinuxDmiInfo("product_serial");
                if (!string.IsNullOrWhiteSpace(serialNumber) && serialNumber.Length > 3)
                {
                    return serialNumber;
                }
                
                // Try machine-id as fallback
                if (File.Exists("/etc/machine-id"))
                {
                    var machineId = File.ReadAllText("/etc/machine-id").Trim();
                    if (!string.IsNullOrWhiteSpace(machineId))
                    {
                        return machineId;
                    }
                }
                
                // Generate a unique identifier based on hostname
                return $"LINUX-{Environment.MachineName}-{TimeZoneHelper.GetTurkeyTime():yyyyMM}";
            }
            catch
            {
                return "Unknown";
            }
        }

        private static (string Name, int Cores, int LogicalCores, int ClockMHz) GetLinuxCpuInfoOptimized()
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
                    bool foundName = false;
                    bool foundClock = false;
                    
                    foreach (var line in lines)
                    {
                        if (!foundName && line.StartsWith("model name"))
                        {
                            var parts = line.Split(':');
                            if (parts.Length > 1)
                            {
                                name = parts[1].Trim();
                                foundName = true;
                            }
                        }
                        else if (!foundClock && line.StartsWith("cpu MHz"))
                        {
                            var parts = line.Split(':');
                            if (parts.Length > 1 && double.TryParse(parts[1].Trim(), out double mhz))
                            {
                                clockMHz = (int)mhz;
                                foundClock = true;
                            }
                        }
                        
                        // Break early if we have both pieces of information
                        if (foundName && foundClock) break;
                    }
                }
            }
            catch
            {
                // Use default values
            }

            return (name, cores, logicalCores, clockMHz);
        }

        private static (int TotalGB, List<RamModuleDto> Modules) GetLinuxMemoryInfoOptimized()
        {
            int totalGB = 0;
            var modules = new List<RamModuleDto>();

            try
            {
                // Get total memory from /proc/meminfo first (fast)
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
                            }
                            break;
                        }
                    }
                }

                // Try to get detailed RAM module information with timeout
                try
                {
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
                    var dmidecodeTask = Task.Run(() =>
                    {
                        var process = new Process
                        {
                            StartInfo = new ProcessStartInfo
                            {
                                FileName = "dmidecode",
                                Arguments = "-t memory",
                                RedirectStandardOutput = true,
                                RedirectStandardError = true,
                                UseShellExecute = false,
                                CreateNoWindow = true
                            }
                        };

                        process.Start();
                        string output = process.StandardOutput.ReadToEnd();
                        process.WaitForExit();

                        if (process.ExitCode == 0 && !string.IsNullOrEmpty(output))
                        {
                            ParseDmidecodeMemoryOutput(output, modules);
                        }
                    }, cts.Token);

                    dmidecodeTask.Wait(cts.Token);
                }
                catch (OperationCanceledException)
                {
                    Console.WriteLine("dmidecode memory query timed out");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"dmidecode not available or failed: {ex.Message}");
                }

                // If no modules found via dmidecode, create a generic one based on total memory
                if (modules.Count == 0 && totalGB > 0)
                {
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
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting optimized memory info: {ex.Message}");
            }

            // Fallback values if nothing was found
            if (totalGB == 0)
            {
                totalGB = 4; // Default fallback
                modules.Clear();
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

        private static void ParseDmidecodeMemoryOutput(string output, List<RamModuleDto> modules)
        {
            var lines = output.Split('\n');
            bool inMemoryDevice = false;
            var currentModule = new RamModuleDto();
            
            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();
                
                if (trimmedLine.StartsWith("Memory Device"))
                {
                    // Save previous module if it has valid data
                    if (inMemoryDevice && !string.IsNullOrEmpty(currentModule.Slot) && currentModule.CapacityGB > 0)
                    {
                        modules.Add(currentModule);
                    }
                    
                    inMemoryDevice = true;
                    currentModule = new RamModuleDto();
                }
                else if (inMemoryDevice)
                {
                    if (trimmedLine.StartsWith("Locator:"))
                    {
                        currentModule.Slot = trimmedLine.Substring(8).Trim();
                    }
                    else if (trimmedLine.StartsWith("Size:"))
                    {
                        var sizeStr = trimmedLine.Substring(5).Trim();
                        if (sizeStr.Contains("GB"))
                        {
                            var sizeNumStr = sizeStr.Replace("GB", "").Trim();
                            if (double.TryParse(sizeNumStr, out double gb))
                            {
                                currentModule.CapacityGB = gb;
                            }
                        }
                        else if (sizeStr.Contains("MB"))
                        {
                            var sizeNumStr = sizeStr.Replace("MB", "").Trim();
                            if (double.TryParse(sizeNumStr, out double mb))
                            {
                                currentModule.CapacityGB = Math.Round(mb / 1024.0, 2);
                            }
                        }
                    }
                    else if (trimmedLine.StartsWith("Speed:"))
                    {
                        currentModule.SpeedMHz = trimmedLine.Substring(6).Trim();
                    }
                    else if (trimmedLine.StartsWith("Manufacturer:"))
                    {
                        currentModule.Manufacturer = trimmedLine.Substring(13).Trim();
                    }
                    else if (trimmedLine.StartsWith("Part Number:"))
                    {
                        currentModule.PartNumber = trimmedLine.Substring(12).Trim();
                    }
                    else if (trimmedLine.StartsWith("Serial Number:"))
                    {
                        currentModule.SerialNumber = trimmedLine.Substring(14).Trim();
                    }
                }
            }
            
            // Add last module if valid
            if (inMemoryDevice && !string.IsNullOrEmpty(currentModule.Slot) && currentModule.CapacityGB > 0)
            {
                modules.Add(currentModule);
            }
        }

        private static List<NetworkAdapterDto> GetLinuxNetworkAdaptersOptimized()
        {
            var adapters = new List<NetworkAdapterDto>();

            try
            {
                // Fast method using /sys/class/net first
                var interfaces = Directory.GetDirectories("/sys/class/net");
                foreach (var interfaceDir in interfaces)
                {
                    var interfaceName = Path.GetFileName(interfaceDir);
                    if (interfaceName == "lo") continue; // Skip loopback
                    
                    string macAddress = "00:00:00:00:00:00";
                    string description = $"Linux Network Interface {interfaceName}";
                    
                    var addressPath = Path.Combine(interfaceDir, "address");
                    if (File.Exists(addressPath))
                    {
                        macAddress = File.ReadAllText(addressPath).Trim();
                    }
                    
                    adapters.Add(new NetworkAdapterDto
                    {
                        Description = description,
                        MacAddress = macAddress,
                        IpAddress = "N/A" // IP will be filled by network info gathering
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading network interfaces from /sys: {ex.Message}");
            }

            // If no adapters found, add a default one
            if (adapters.Count == 0)
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

        private static List<GpuInfoDto> GetLinuxGpuInfoOptimized()
        {
            var gpus = new List<GpuInfoDto>();

            try
            {
                // Use lspci with timeout
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
                var lspciTask = Task.Run(() =>
                {
                    var process = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = "lspci",
                            Arguments = "-v",
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            UseShellExecute = false,
                            CreateNoWindow = true
                        }
                    };

                    process.Start();
                    string output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit();

                    if (process.ExitCode == 0 && !string.IsNullOrEmpty(output))
                    {
                        ParseLspciOutput(output, gpus);
                    }
                }, cts.Token);

                try
                {
                    lspciTask.Wait(cts.Token);
                }
                catch (OperationCanceledException)
                {
                    Console.WriteLine("lspci command timed out");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting GPU info with lspci: {ex.Message}");
            }

            // If no GPUs found, add a placeholder
            if (gpus.Count == 0)
            {
                gpus.Add(new GpuInfoDto
                {
                    Name = "Unknown GPU",
                    MemoryGB = null
                });
            }

            return gpus;
        }

        private static void ParseLspciOutput(string output, List<GpuInfoDto> gpus)
        {
            var lines = output.Split('\n');
            string currentDevice = "";
            bool isGpu = false;
            
            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();
                
                // New PCI device line
                if (!line.StartsWith(" ") && !line.StartsWith("\t") && line.Contains(":"))
                {
                    // Save previous GPU if found
                    if (isGpu && !string.IsNullOrEmpty(currentDevice))
                    {
                        float? memoryGB = null;
                        
                        // Try to get memory info for NVIDIA GPUs
                        if (currentDevice.ToLower().Contains("nvidia"))
                        {
                            memoryGB = GetNvidiaGpuMemoryOptimized();
                        }
                        
                        gpus.Add(new GpuInfoDto
                        {
                            Name = currentDevice,
                            MemoryGB = memoryGB
                        });
                    }
                    
                    // Check if this is a GPU device
                    if (trimmedLine.ToLower().Contains("vga") || 
                        trimmedLine.ToLower().Contains("3d") || 
                        trimmedLine.ToLower().Contains("display"))
                    {
                        isGpu = true;
                        // Extract device name (everything after the first space after the ID)
                        var parts = trimmedLine.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length > 2)
                        {
                            currentDevice = string.Join(" ", parts.Skip(2));
                        }
                    }
                    else
                    {
                        isGpu = false;
                        currentDevice = "";
                    }
                }
            }
            
            // Add last GPU if found
            if (isGpu && !string.IsNullOrEmpty(currentDevice))
            {
                float? memoryGB = null;
                
                if (currentDevice.ToLower().Contains("nvidia"))
                {
                    memoryGB = GetNvidiaGpuMemoryOptimized();
                }
                
                gpus.Add(new GpuInfoDto
                {
                    Name = currentDevice,
                    MemoryGB = memoryGB
                });
            }
        }

        private static float? GetNvidiaGpuMemoryOptimized()
        {
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
                var task = Task.Run(() =>
                {
                    var process = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = "nvidia-smi",
                            Arguments = "--query-gpu=memory.total --format=csv,noheader,nounits",
                            RedirectStandardOutput = true,
                            UseShellExecute = false,
                            CreateNoWindow = true
                        }
                    };

                    process.Start();
                    string output = process.StandardOutput.ReadToEnd().Trim();
                    process.WaitForExit();

                    if (process.ExitCode == 0 && !string.IsNullOrEmpty(output))
                    {
                        if (float.TryParse(output, out float memoryMB))
                        {
                            return memoryMB / 1024.0f; // Convert MB to GB
                        }
                    }
                    return (float?)null;
                }, cts.Token);

                try
                {
                    task.Wait(cts.Token);
                    return task.Result;
                }
                catch (OperationCanceledException)
                {
                    return null;
                }
            }
            catch
            {
                return null;
            }
        }

        private static (string BiosManufacturer, string BiosVersion, string BiosSerial, string Motherboard, string MotherboardSerial) GetLinuxSystemInfoOptimized()
        {
            string biosManufacturer = "Unknown";
            string biosVersion = "Unknown";
            string biosSerial = "Unknown";
            string motherboard = "Unknown";
            string motherboardSerial = "Unknown";

            try
            {
                // Fast method: try reading from /sys/class/dmi/id/ first (available without root)
                var dmiPath = "/sys/class/dmi/id/";
                
                var biosVendorPath = Path.Combine(dmiPath, "bios_vendor");
                if (File.Exists(biosVendorPath))
                {
                    biosManufacturer = File.ReadAllText(biosVendorPath).Trim();
                }
                
                var biosVersionPath = Path.Combine(dmiPath, "bios_version");
                if (File.Exists(biosVersionPath))
                {
                    biosVersion = File.ReadAllText(biosVersionPath).Trim();
                }
                
                var boardVendorPath = Path.Combine(dmiPath, "board_vendor");
                var boardNamePath = Path.Combine(dmiPath, "board_name");
                
                string boardVendor = "";
                string boardName = "";
                
                if (File.Exists(boardVendorPath))
                {
                    boardVendor = File.ReadAllText(boardVendorPath).Trim();
                }
                
                if (File.Exists(boardNamePath))
                {
                    boardName = File.ReadAllText(boardNamePath).Trim();
                }
                
                if (!string.IsNullOrEmpty(boardVendor) && !string.IsNullOrEmpty(boardName))
                {
                    motherboard = $"{boardVendor} {boardName}";
                }
                else if (!string.IsNullOrEmpty(boardName))
                {
                    motherboard = boardName;
                }
                else if (!string.IsNullOrEmpty(boardVendor))
                {
                    motherboard = boardVendor;
                }
                
                var boardSerialPath = Path.Combine(dmiPath, "board_serial");
                if (File.Exists(boardSerialPath))
                {
                    motherboardSerial = File.ReadAllText(boardSerialPath).Trim();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading DMI information from /sys: {ex.Message}");
            }

            return (biosManufacturer, biosVersion, biosSerial, motherboard, motherboardSerial);
        }

        private static (string Name, string Version) GetLinuxOsInfoOptimized()
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
                        
                        // Break early if we have both pieces of information
                        if (name != "Linux" && version != "Unknown") break;
                    }
                }
            }
            catch
            {
                // Use default values
            }

            return (name, version);
        }

        private static List<string> GetLinuxInstalledPackagesOptimized()
        {
            var packages = new List<string>();
            const int maxPackages = 30; // Reduced for performance

            try
            {
                // Try different package managers with timeout
                var packageManagers = new[]
                {
                    ("dpkg", "--get-selections"),
                    ("rpm", "-qa"),
                    ("pacman", "-Q")
                };

                foreach (var (command, args) in packageManagers)
                {
                    try
                    {
                        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                        var task = Task.Run(() =>
                        {
                            if (TryExecuteCommandOptimized(command, args, out string output))
                            {
                                ProcessPackageManagerOutput(command, output, packages, maxPackages);
                                return true;
                            }
                            return false;
                        }, cts.Token);

                        try
                        {
                            task.Wait(cts.Token);
                            if (task.Result && packages.Count > 0)
                            {
                                break; // Found packages, stop trying other package managers
                            }
                        }
                        catch (OperationCanceledException)
                        {
                            Console.WriteLine($"{command} command timed out");
                        }
                    }
                    catch
                    {
                        // Try next package manager
                        continue;
                    }
                }

                if (packages.Count == 0)
                {
                    packages.Add("No package manager found or no packages detected");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting installed packages: {ex.Message}");
                packages.Add("Package listing not available");
            }

            return packages.Take(maxPackages).ToList();
        }

        private static void ProcessPackageManagerOutput(string command, string output, List<string> packages, int maxPackages)
        {
            var lines = output.Split('\n');
            
            switch (command)
            {
                case "dpkg":
                    foreach (var line in lines)
                    {
                        if (packages.Count >= maxPackages) break;
                        if (!string.IsNullOrWhiteSpace(line) && line.Contains("\tinstall"))
                        {
                            var parts = line.Split('\t');
                            if (parts.Length > 0)
                            {
                                packages.Add(parts[0].Trim());
                            }
                        }
                    }
                    break;

                case "rpm":
                case "pacman":
                    foreach (var line in lines)
                    {
                        if (packages.Count >= maxPackages) break;
                        if (!string.IsNullOrWhiteSpace(line))
                        {
                            packages.Add(line.Trim());
                        }
                    }
                    break;
            }
        }

        private static bool TryExecuteCommandOptimized(string fileName, string arguments, out string output)
        {
            output = "";
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = fileName,
                        Arguments = arguments,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                return process.ExitCode == 0 && !string.IsNullOrWhiteSpace(output);
            }
            catch
            {
                return false;
            }
        }

        private static List<string> GetLinuxUsersOptimized()
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
                        if (parts.Length > 2 && !parts[0].StartsWith("#"))
                        {
                            // Only add normal users (UID >= 1000)
                            if (int.TryParse(parts[2], out int uid) && uid >= 1000)
                            {
                                users.Add(parts[0]);
                                if (users.Count >= 10) break; // Limit for performance
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

        private static string GetLinuxDmiInfo(string key)
        {
            try
            {
                var dmiPath = $"/sys/class/dmi/id/{key}";
                if (File.Exists(dmiPath))
                {
                    return File.ReadAllText(dmiPath).Trim();
                }
            }
            catch
            {
                // Ignore DMI read errors
            }
            return "";
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
                // First get total memory from /proc/meminfo
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
                            }
                            break;
                        }
                    }
                }

                // Try to get detailed RAM module information using dmidecode
                try
                {
                    var process = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = "dmidecode",
                            Arguments = "-t memory",
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            UseShellExecute = false,
                            CreateNoWindow = true
                        }
                    };

                    process.Start();
                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();
                    process.WaitForExit();

                    if (process.ExitCode == 0 && !string.IsNullOrEmpty(output))
                    {
                        var lines = output.Split('\n');
                        bool inMemoryDevice = false;
                        var currentModule = new RamModuleDto();
                        
                        foreach (var line in lines)
                        {
                            var trimmedLine = line.Trim();
                            
                            if (trimmedLine.StartsWith("Memory Device"))
                            {
                                // Save previous module if it has valid data
                                if (inMemoryDevice && !string.IsNullOrEmpty(currentModule.Slot) && currentModule.CapacityGB > 0)
                                {
                                    modules.Add(currentModule);
                                }
                                
                                inMemoryDevice = true;
                                currentModule = new RamModuleDto();
                            }
                            else if (inMemoryDevice)
                            {
                                if (trimmedLine.StartsWith("Locator:"))
                                {
                                    currentModule.Slot = trimmedLine.Substring(8).Trim();
                                }
                                else if (trimmedLine.StartsWith("Size:"))
                                {
                                    var sizeStr = trimmedLine.Substring(5).Trim();
                                    if (sizeStr.Contains("GB"))
                                    {
                                        var sizeNumStr = sizeStr.Replace("GB", "").Trim();
                                        if (double.TryParse(sizeNumStr, out double gb))
                                        {
                                            currentModule.CapacityGB = gb;
                                        }
                                    }
                                    else if (sizeStr.Contains("MB"))
                                    {
                                        var sizeNumStr = sizeStr.Replace("MB", "").Trim();
                                        if (double.TryParse(sizeNumStr, out double mb))
                                        {
                                            currentModule.CapacityGB = Math.Round(mb / 1024.0, 2);
                                        }
                                    }
                                }
                                else if (trimmedLine.StartsWith("Speed:"))
                                {
                                    currentModule.SpeedMHz = trimmedLine.Substring(6).Trim();
                                }
                                else if (trimmedLine.StartsWith("Manufacturer:"))
                                {
                                    currentModule.Manufacturer = trimmedLine.Substring(13).Trim();
                                }
                                else if (trimmedLine.StartsWith("Part Number:"))
                                {
                                    currentModule.PartNumber = trimmedLine.Substring(12).Trim();
                                }
                                else if (trimmedLine.StartsWith("Serial Number:"))
                                {
                                    currentModule.SerialNumber = trimmedLine.Substring(14).Trim();
                                }
                            }
                        }
                        
                        // Add last module if valid
                        if (inMemoryDevice && !string.IsNullOrEmpty(currentModule.Slot) && currentModule.CapacityGB > 0)
                        {
                            modules.Add(currentModule);
                        }
                    }
                    else if (!string.IsNullOrEmpty(error) && error.Contains("Permission denied"))
                    {
                        Console.WriteLine("dmidecode requires root privileges for detailed memory information");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error running dmidecode for memory: {ex.Message}");
                }

                // If no modules found via dmidecode, create a generic one based on total memory
                if (modules.Count == 0 && totalGB > 0)
                {
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
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting memory info: {ex.Message}");
            }

            // Fallback values if nothing was found
            if (totalGB == 0)
            {
                totalGB = 4; // Default fallback
                modules.Clear();
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
                // Get network interfaces using 'ip addr show' command
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "ip",
                        Arguments = "addr show",
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                if (process.ExitCode == 0)
                {
                    var lines = output.Split('\n');
                    string currentInterface = "";
                    string currentMac = "";
                    string currentIp = "";
                    
                    foreach (var line in lines)
                    {
                        var trimmedLine = line.Trim();
                        
                        // New interface line
                        if (Char.IsDigit(trimmedLine.FirstOrDefault()) && trimmedLine.Contains(":"))
                        {
                            // Save previous interface if we have data
                            if (!string.IsNullOrEmpty(currentInterface) && currentInterface != "lo")
                            {
                                adapters.Add(new NetworkAdapterDto
                                {
                                    Description = $"Linux Network Interface {currentInterface}",
                                    MacAddress = !string.IsNullOrEmpty(currentMac) ? currentMac : "00:00:00:00:00:00",
                                    IpAddress = !string.IsNullOrEmpty(currentIp) ? currentIp : "N/A"
                                });
                            }
                            
                            // Reset for new interface
                            var parts = trimmedLine.Split(':');
                            if (parts.Length > 1)
                            {
                                currentInterface = parts[1].Trim().Split(' ')[0];
                                currentMac = "";
                                currentIp = "";
                            }
                        }
                        // Find MAC address
                        else if (trimmedLine.StartsWith("link/ether"))
                        {
                            var parts = trimmedLine.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                            if (parts.Length > 1)
                            {
                                currentMac = parts[1];
                            }
                        }
                        // Find IP address
                        else if (trimmedLine.StartsWith("inet ") && !trimmedLine.Contains("127.0.0.1"))
                        {
                            var parts = trimmedLine.Split(new char[] { ' ', '/' }, StringSplitOptions.RemoveEmptyEntries);
                            if (parts.Length > 1)
                            {
                                currentIp = parts[1];
                            }
                        }
                    }
                    
                    // Add last interface if valid
                    if (!string.IsNullOrEmpty(currentInterface) && currentInterface != "lo")
                    {
                        adapters.Add(new NetworkAdapterDto
                        {
                            Description = $"Linux Network Interface {currentInterface}",
                            MacAddress = !string.IsNullOrEmpty(currentMac) ? currentMac : "00:00:00:00:00:00",
                            IpAddress = !string.IsNullOrEmpty(currentIp) ? currentIp : "N/A"
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting network adapters: {ex.Message}");
            }

            // Fallback - read from /sys/class/net if no adapters found
            if (adapters.Count == 0)
            {
                try
                {
                    var interfaces = Directory.GetDirectories("/sys/class/net");
                    foreach (var interfaceDir in interfaces)
                    {
                        var interfaceName = Path.GetFileName(interfaceDir);
                        if (interfaceName == "lo") continue; // Skip loopback
                        
                        string macAddress = "00:00:00:00:00:00";
                        string description = $"Linux Network Interface {interfaceName}";
                        
                        var addressPath = Path.Combine(interfaceDir, "address");
                        if (File.Exists(addressPath))
                        {
                            macAddress = File.ReadAllText(addressPath).Trim();
                        }
                        
                        adapters.Add(new NetworkAdapterDto
                        {
                            Description = description,
                            MacAddress = macAddress,
                            IpAddress = "N/A"
                        });
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error reading network interfaces from /sys: {ex.Message}");
                }
            }

            // If still no adapters, add a default one
            if (adapters.Count == 0)
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
            string macAddress = "";
            string ipAddress = "";

            try
            {
                // Get network interfaces using 'ip addr show' command
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "ip",
                        Arguments = "addr show",
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                if (process.ExitCode == 0)
                {
                    var lines = output.Split('\n');
                    string currentInterface = "";
                    
                    foreach (var line in lines)
                    {
                        var trimmedLine = line.Trim();
                        
                        // Find interface line (contains 'state UP')
                        if (trimmedLine.Contains("state UP") && !trimmedLine.Contains("lo:"))
                        {
                            // Extract interface name
                            var parts = trimmedLine.Split(':');
                            if (parts.Length > 1)
                            {
                                currentInterface = parts[1].Trim();
                            }
                        }
                        // Find MAC address line
                        else if (trimmedLine.StartsWith("link/ether") && !string.IsNullOrEmpty(currentInterface))
                        {
                            var parts = trimmedLine.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                            if (parts.Length > 1 && string.IsNullOrEmpty(macAddress))
                            {
                                macAddress = parts[1];
                            }
                        }
                        // Find IP address line
                        else if (trimmedLine.StartsWith("inet ") && !trimmedLine.Contains("127.0.0.1"))
                        {
                            var parts = trimmedLine.Split(new char[] { ' ', '/' }, StringSplitOptions.RemoveEmptyEntries);
                            if (parts.Length > 1 && string.IsNullOrEmpty(ipAddress))
                            {
                                ipAddress = parts[1];
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting network info: {ex.Message}");
            }

            // Fallback to reading /sys/class/net if ip command fails
            if (string.IsNullOrEmpty(macAddress) || string.IsNullOrEmpty(ipAddress))
            {
                try
                {
                    var interfaces = Directory.GetDirectories("/sys/class/net");
                    foreach (var interfaceDir in interfaces)
                    {
                        var interfaceName = Path.GetFileName(interfaceDir);
                        if (interfaceName == "lo") continue; // Skip loopback
                        
                        var operstatePath = Path.Combine(interfaceDir, "operstate");
                        if (File.Exists(operstatePath))
                        {
                            var operstate = File.ReadAllText(operstatePath).Trim();
                            if (operstate == "up")
                            {
                                // Get MAC address
                                var addressPath = Path.Combine(interfaceDir, "address");
                                if (File.Exists(addressPath) && string.IsNullOrEmpty(macAddress))
                                {
                                    macAddress = File.ReadAllText(addressPath).Trim();
                                }
                            }
                        }
                    }
                    
                    // Get IP using hostname command if still empty
                    if (string.IsNullOrEmpty(ipAddress))
                    {
                        var process = new Process
                        {
                            StartInfo = new ProcessStartInfo
                            {
                                FileName = "hostname",
                                Arguments = "-I",
                                RedirectStandardOutput = true,
                                UseShellExecute = false,
                                CreateNoWindow = true
                            }
                        };

                        process.Start();
                        string output = process.StandardOutput.ReadToEnd().Trim();
                        process.WaitForExit();

                        if (process.ExitCode == 0 && !string.IsNullOrEmpty(output))
                        {
                            // Take first IP address
                            var ips = output.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                            if (ips.Length > 0)
                            {
                                ipAddress = ips[0];
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error reading network info from /sys: {ex.Message}");
                }
            }

            // Use fallback values if nothing found
            if (string.IsNullOrEmpty(macAddress)) macAddress = "00:00:00:00:00:00";
            if (string.IsNullOrEmpty(ipAddress)) ipAddress = "127.0.0.1";

            return (macAddress, ipAddress);
        }

        private static List<GpuInfoDto> GetLinuxGpuInfo()
        {
            var gpus = new List<GpuInfoDto>();

            try
            {
                // Try using lspci command to get GPU information
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "lspci",
                        Arguments = "-v",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                string errorOutput = process.StandardError.ReadToEnd();
                process.WaitForExit();

                // Handle specific libkmod error gracefully
                if (!string.IsNullOrEmpty(errorOutput))
                {
                    if (errorOutput.Contains("libkmod") || errorOutput.Contains("error -2"))
                    {
                        Console.WriteLine("lspci: libkmod resources not available, falling back to alternative GPU detection methods");
                    }
                    else if (errorOutput.Trim().Length > 0)
                    {
                        Console.WriteLine($"lspci warning: {errorOutput.Trim()}");
                    }
                }

                if (process.ExitCode == 0 && !string.IsNullOrEmpty(output))
                {
                    var lines = output.Split('\n');
                    string currentDevice = "";
                    bool isGpu = false;
                    
                    foreach (var line in lines)
                    {
                        var trimmedLine = line.Trim();
                        
                        // New PCI device line
                        if (!line.StartsWith(" ") && !line.StartsWith("\t") && line.Contains(":"))
                        {
                            // Save previous GPU if found
                            if (isGpu && !string.IsNullOrEmpty(currentDevice))
                            {
                                float? memoryGB = null;
                                
                                // Try to get memory info for NVIDIA GPUs using nvidia-smi
                                if (currentDevice.ToLower().Contains("nvidia"))
                                {
                                    memoryGB = GetNvidiaGpuMemory();
                                }
                                
                                gpus.Add(new GpuInfoDto
                                {
                                    Name = currentDevice,
                                    MemoryGB = memoryGB
                                });
                            }
                            
                            // Check if this is a GPU device
                            if (trimmedLine.ToLower().Contains("vga") || 
                                trimmedLine.ToLower().Contains("3d") || 
                                trimmedLine.ToLower().Contains("display"))
                            {
                                isGpu = true;
                                // Extract device name (everything after the first space after the ID)
                                var parts = trimmedLine.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                                if (parts.Length > 2)
                                {
                                    currentDevice = string.Join(" ", parts.Skip(2));
                                }
                            }
                            else
                            {
                                isGpu = false;
                                currentDevice = "";
                            }
                        }
                    }
                    
                    // Add last GPU if found
                    if (isGpu && !string.IsNullOrEmpty(currentDevice))
                    {
                        float? memoryGB = null;
                        
                        if (currentDevice.ToLower().Contains("nvidia"))
                        {
                            memoryGB = GetNvidiaGpuMemory();
                        }
                        
                        gpus.Add(new GpuInfoDto
                        {
                            Name = currentDevice,
                            MemoryGB = memoryGB
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting GPU info with lspci: {ex.Message}");
            }

            // Fallback: try reading /proc/driver/nvidia/gpus for NVIDIA cards
            if (gpus.Count == 0)
            {
                try
                {
                    if (Directory.Exists("/proc/driver/nvidia/gpus"))
                    {
                        var gpuDirs = Directory.GetDirectories("/proc/driver/nvidia/gpus");
                        foreach (var gpuDir in gpuDirs)
                        {
                            var informationPath = Path.Combine(gpuDir, "information");
                            if (File.Exists(informationPath))
                            {
                                var content = File.ReadAllText(informationPath);
                                var lines = content.Split('\n');
                                
                                string gpuName = "NVIDIA GPU";
                                foreach (var line in lines)
                                {
                                    if (line.StartsWith("Model:"))
                                    {
                                        gpuName = line.Substring(6).Trim();
                                        break;
                                    }
                                }
                                
                                float? memoryGB = GetNvidiaGpuMemory();
                                
                                gpus.Add(new GpuInfoDto
                                {
                                    Name = gpuName,
                                    MemoryGB = memoryGB
                                });
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error reading NVIDIA GPU info: {ex.Message}");
                }
            }

            // If no GPUs found, add a placeholder
            if (gpus.Count == 0)
            {
                gpus.Add(new GpuInfoDto
                {
                    Name = "Unknown GPU",
                    MemoryGB = null
                });
            }

            return gpus;
        }

        private static float? GetNvidiaGpuMemory()
        {
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "nvidia-smi",
                        Arguments = "--query-gpu=memory.total --format=csv,noheader,nounits",
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                string output = process.StandardOutput.ReadToEnd().Trim();
                process.WaitForExit();

                if (process.ExitCode == 0 && !string.IsNullOrEmpty(output))
                {
                    if (float.TryParse(output, out float memoryMB))
                    {
                        return memoryMB / 1024.0f; // Convert MB to GB
                    }
                }
            }
            catch
            {
                // nvidia-smi not available or failed
            }

            return null;
        }

        private static (string BiosManufacturer, string BiosVersion, string BiosSerial, string Motherboard, string MotherboardSerial) GetLinuxSystemInfo()
        {
            string biosManufacturer = "Unknown";
            string biosVersion = "Unknown";
            string biosSerial = "Unknown"; // Fix: Add BiosSerial
            string motherboard = "Unknown";
            string motherboardSerial = "Unknown";

            try
            {
                // Try using dmidecode command (requires root privileges on some systems)
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "dmidecode",
                        Arguments = "-t bios -t baseboard",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (process.ExitCode == 0 && !string.IsNullOrEmpty(output))
                {
                    var lines = output.Split('\n');
                    string currentSection = "";
                    
                    foreach (var line in lines)
                    {
                        var trimmedLine = line.Trim();
                        
                        if (trimmedLine.StartsWith("BIOS Information"))
                        {
                            currentSection = "BIOS";
                        }
                        else if (trimmedLine.StartsWith("Base Board Information"))
                        {
                            currentSection = "BASEBOARD";
                        }
                        else if (currentSection == "BIOS")
                        {
                            if (trimmedLine.StartsWith("Vendor:") && biosManufacturer == "Unknown")
                            {
                                biosManufacturer = trimmedLine.Substring(7).Trim();
                            }
                            else if (trimmedLine.StartsWith("Version:") && biosVersion == "Unknown")
                            {
                                biosVersion = trimmedLine.Substring(8).Trim();
                            }
                        }
                        else if (currentSection == "BASEBOARD")
                        {
                            if (trimmedLine.StartsWith("Manufacturer:") || trimmedLine.StartsWith("Product Name:"))
                            {
                                var value = trimmedLine.Contains(":") ? trimmedLine.Split(':')[1].Trim() : "";
                                if (!string.IsNullOrEmpty(value) && value != "Unknown" && motherboard == "Unknown")
                                {
                                    motherboard = value;
                                }
                            }
                            else if (trimmedLine.StartsWith("Serial Number:") && motherboardSerial == "Unknown")
                            {
                                motherboardSerial = trimmedLine.Substring(14).Trim();
                            }
                        }
                    }
                }
                else if (!string.IsNullOrEmpty(error) && error.Contains("Permission denied"))
                {
                    Console.WriteLine("dmidecode requires root privileges for full system information");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error running dmidecode: {ex.Message}");
            }

            // Fallback: try reading from /sys/class/dmi/id/ (available without root)
            if (biosManufacturer == "Unknown" || biosVersion == "Unknown" || motherboard == "Unknown")
            {
                try
                {
                    var dmiPath = "/sys/class/dmi/id/";
                    
                    if (biosManufacturer == "Unknown")
                    {
                        var biosVendorPath = Path.Combine(dmiPath, "bios_vendor");
                        if (File.Exists(biosVendorPath))
                        {
                            biosManufacturer = File.ReadAllText(biosVendorPath).Trim();
                        }
                    }
                    
                    if (biosVersion == "Unknown")
                    {
                        var biosVersionPath = Path.Combine(dmiPath, "bios_version");
                        if (File.Exists(biosVersionPath))
                        {
                            biosVersion = File.ReadAllText(biosVersionPath).Trim();
                        }
                    }
                    
                    if (motherboard == "Unknown")
                    {
                        var boardVendorPath = Path.Combine(dmiPath, "board_vendor");
                        var boardNamePath = Path.Combine(dmiPath, "board_name");
                        
                        string boardVendor = "";
                        string boardName = "";
                        
                        if (File.Exists(boardVendorPath))
                        {
                            boardVendor = File.ReadAllText(boardVendorPath).Trim();
                        }
                        
                        if (File.Exists(boardNamePath))
                        {
                            boardName = File.ReadAllText(boardNamePath).Trim();
                        }
                        
                        if (!string.IsNullOrEmpty(boardVendor) && !string.IsNullOrEmpty(boardName))
                        {
                            motherboard = $"{boardVendor} {boardName}";
                        }
                        else if (!string.IsNullOrEmpty(boardName))
                        {
                            motherboard = boardName;
                        }
                        else if (!string.IsNullOrEmpty(boardVendor))
                        {
                            motherboard = boardVendor;
                        }
                    }
                    
                    if (motherboardSerial == "Unknown")
                    {
                        var boardSerialPath = Path.Combine(dmiPath, "board_serial");
                        if (File.Exists(boardSerialPath))
                        {
                            motherboardSerial = File.ReadAllText(boardSerialPath).Trim();
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error reading DMI information from /sys: {ex.Message}");
                }
            }

            return (biosManufacturer, biosVersion, biosSerial, motherboard, motherboardSerial);
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
                // Try different package managers in order of preference
                
                // 1. Try dpkg (Debian/Ubuntu)
                if (TryExecuteCommand("dpkg", "--get-selections", out string dpkgOutput))
                {
                    var lines = dpkgOutput.Split('\n');
                    foreach (var line in lines)
                    {
                        if (!string.IsNullOrWhiteSpace(line) && line.Contains("\tinstall"))
                        {
                            var parts = line.Split('\t');
                            if (parts.Length > 0)
                            {
                                packages.Add(parts[0].Trim());
                            }
                        }
                    }
                    
                    if (packages.Count > 0)
                    {
                        return packages.Take(100).ToList(); // Limit to first 100 packages
                    }
                }

                // 2. Try rpm (Red Hat/CentOS/Fedora)
                if (TryExecuteCommand("rpm", "-qa", out string rpmOutput))
                {
                    var lines = rpmOutput.Split('\n');
                    foreach (var line in lines)
                    {
                        if (!string.IsNullOrWhiteSpace(line))
                        {
                            packages.Add(line.Trim());
                        }
                    }
                    
                    if (packages.Count > 0)
                    {
                        return packages.Take(100).ToList(); // Limit to first 100 packages
                    }
                }

                // 3. Try pacman (Arch Linux)
                if (TryExecuteCommand("pacman", "-Q", out string pacmanOutput))
                {
                    var lines = pacmanOutput.Split('\n');
                    foreach (var line in lines)
                    {
                        if (!string.IsNullOrWhiteSpace(line))
                        {
                            packages.Add(line.Trim());
                        }
                    }
                    
                    if (packages.Count > 0)
                    {
                        return packages.Take(100).ToList(); // Limit to first 100 packages
                    }
                }

                // 4. Try yum (older Red Hat systems)
                if (TryExecuteCommand("yum", "list installed", out string yumOutput))
                {
                    var lines = yumOutput.Split('\n');
                    bool installedSection = false;
                    
                    foreach (var line in lines)
                    {
                        if (line.Contains("Installed Packages"))
                        {
                            installedSection = true;
                            continue;
                        }
                        
                        if (installedSection && !string.IsNullOrWhiteSpace(line) && !line.StartsWith("Loading"))
                        {
                            var parts = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                            if (parts.Length > 0)
                            {
                                packages.Add(parts[0].Trim());
                            }
                        }
                    }
                    
                    if (packages.Count > 0)
                    {
                        return packages.Take(100).ToList();
                    }
                }

                // 5. Try snap packages
                if (TryExecuteCommand("snap", "list", out string snapOutput))
                {
                    var lines = snapOutput.Split('\n');
                    foreach (var line in lines)
                    {
                        if (!string.IsNullOrWhiteSpace(line) && !line.StartsWith("Name") && !line.StartsWith("core"))
                        {
                            var parts = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                            if (parts.Length > 0)
                            {
                                packages.Add($"snap: {parts[0].Trim()}");
                            }
                        }
                    }
                }

                // 6. Try flatpak packages
                if (TryExecuteCommand("flatpak", "list", out string flatpakOutput))
                {
                    var lines = flatpakOutput.Split('\n');
                    foreach (var line in lines)
                    {
                        if (!string.IsNullOrWhiteSpace(line))
                        {
                            var parts = line.Split('\t');
                            if (parts.Length > 0)
                            {
                                packages.Add($"flatpak: {parts[0].Trim()}");
                            }
                        }
                    }
                }

                if (packages.Count == 0)
                {
                    packages.Add("No package manager found or no packages detected");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting installed packages: {ex.Message}");
                packages.Add("Package listing not available");
            }

            return packages.Take(100).ToList(); // Limit total packages returned
        }

        private static bool TryExecuteCommand(string fileName, string arguments, out string output)
        {
            output = "";
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = fileName,
                        Arguments = arguments,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                return process.ExitCode == 0 && !string.IsNullOrWhiteSpace(output);
            }
            catch
            {
                return false;
            }
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
                    ChangeDate = TimeZoneHelper.GetTurkeyTime(),
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

            // DeviceDto nesnesini hazırla - Agent kurulan cihaz olarak işaretle
            var device = new DeviceDto
            {
                Name = Environment.MachineName,
                MacAddress = macAddress,
                IpAddress = ipAddress,
                DeviceType = DeviceType.Desktop,
                Model = motherboardModel,
                Location = "Ev",
                Status = 1,
                AgentInstalled = true, // Bu cihazda agent yüklü
                ChangeLogs = changeLogs,
                HardwareInfo = hardwareInfo,
                SoftwareInfo = softwareInfoDto
            };

            return device;
        }
    }
}