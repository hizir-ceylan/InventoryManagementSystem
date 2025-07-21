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
                DeviceType = DeviceType.Desktop,
                Model = "Unknown Linux System", // Fix: Set default model
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

            // Try to get actual model from DMI if available
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
                hardware.BiosSerial = systemInfo.BiosSerial ?? "Unknown"; // Fix: Add BiosSerial
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
                software.SerialNumber = GetLinuxSerialNumber(); // Fix: Add SerialNumber

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
                software.SerialNumber = "Unknown"; // Fix: Set default SerialNumber
            }

            return software;
        }

        private static string GetLinuxSerialNumber()
        {
            try
            {
                // Try to get serial number from DMI
                var serialNumber = GetLinuxDmiInfo("product_serial");
                if (!string.IsNullOrWhiteSpace(serialNumber))
                {
                    return serialNumber;
                }
                
                // Try machine-id as fallback
                if (File.Exists("/etc/machine-id"))
                {
                    return File.ReadAllText("/etc/machine-id").Trim();
                }
                
                // Generate a unique identifier based on hostname
                return $"LINUX-{Environment.MachineName}-{DateTime.UtcNow:yyyyMM}";
            }
            catch
            {
                return "Unknown";
            }
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
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

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
                DeviceType = DeviceType.Desktop,
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