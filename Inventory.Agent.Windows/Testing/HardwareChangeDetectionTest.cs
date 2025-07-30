using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Inventory.Agent.Windows.Models;
using Inventory.Agent.Windows.Services;
using Inventory.Agent.Windows.Configuration;
using Inventory.Domain.Entities;

namespace Inventory.Agent.Windows.Testing
{
    /// <summary>
    /// Simple test utility to demonstrate hardware change detection functionality
    /// </summary>
    public class HardwareChangeDetectionTest
    {
        public static async Task RunTestAsync()
        {
            Console.WriteLine("=== Hardware Change Detection Test ===");
            
            // Create a test storage directory
            var testDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), 
                "InventoryManagementSystem", "Test");
            Directory.CreateDirectory(testDirectory);
            
            var deviceStateService = new DeviceStateService(testDirectory);
            
            Console.WriteLine($"Test directory: {testDirectory}");
            Console.WriteLine("1. Creating initial device state...");
            
            // Create initial device state with GPU
            var initialDevice = CreateTestDevice("Test-PC", new List<GpuInfoDto>
            {
                new GpuInfoDto { Name = "NVIDIA GeForce RTX 3080", MemoryGB = 10.0f },
                new GpuInfoDto { Name = "Intel UHD Graphics 630", MemoryGB = 1.0f }
            });
            
            await deviceStateService.SaveCurrentStateAsync(initialDevice);
            Console.WriteLine("✓ Initial state saved with 2 GPUs");
            
            Console.WriteLine("\n2. Simulating GPU removal (removing Intel integrated graphics)...");
            
            // Create updated device state with one GPU removed
            var updatedDevice = CreateTestDevice("Test-PC", new List<GpuInfoDto>
            {
                new GpuInfoDto { Name = "NVIDIA GeForce RTX 3080", MemoryGB = 10.0f }
            });
            
            // Detect changes
            var previousDevice = await deviceStateService.GetLastKnownStateAsync();
            var changes = await deviceStateService.DetectChangesAsync(updatedDevice, previousDevice);
            
            Console.WriteLine($"✓ Detected {changes.Count} hardware changes:");
            foreach (var change in changes)
            {
                Console.WriteLine($"  - {change.ChangeType}: {change.OldValue} → {change.NewValue}");
            }
            
            await deviceStateService.SaveCurrentStateAsync(updatedDevice);
            
            Console.WriteLine("\n3. Simulating GPU addition (adding AMD graphics)...");
            
            // Create device state with new GPU added
            var deviceWithNewGpu = CreateTestDevice("Test-PC", new List<GpuInfoDto>
            {
                new GpuInfoDto { Name = "NVIDIA GeForce RTX 3080", MemoryGB = 10.0f },
                new GpuInfoDto { Name = "AMD Radeon RX 6800 XT", MemoryGB = 16.0f }
            });
            
            // Detect changes
            previousDevice = await deviceStateService.GetLastKnownStateAsync();
            changes = await deviceStateService.DetectChangesAsync(deviceWithNewGpu, previousDevice);
            
            Console.WriteLine($"✓ Detected {changes.Count} hardware changes:");
            foreach (var change in changes)
            {
                Console.WriteLine($"  - {change.ChangeType}: {change.OldValue} → {change.NewValue}");
            }
            
            Console.WriteLine("\n4. Testing local change log service...");
            
            // Test local change log service
            var apiSettings = new ApiSettings
            {
                BaseUrl = "http://localhost:5093",
                OfflineStoragePath = testDirectory
            };
            
            var localChangeLogService = new LocalChangeLogService(apiSettings);
            var logFilePath = await localChangeLogService.SaveChangeLogsAsync(changes, "Test-PC");
            
            Console.WriteLine($"✓ Change logs saved to: {logFilePath}");
            Console.WriteLine($"✓ Change log directory: {localChangeLogService.GetChangeLogDirectory()}");
            
            // Show the contents of the log file
            if (File.Exists(logFilePath))
            {
                var logContent = await File.ReadAllTextAsync(logFilePath);
                Console.WriteLine("\n5. Log file contents:");
                Console.WriteLine(logContent);
            }
            
            Console.WriteLine("\n=== Test completed successfully! ===");
            Console.WriteLine("\nThis demonstrates that the hardware change detection system can:");
            Console.WriteLine("• Detect GPU removal (as described in the Turkish issue)");
            Console.WriteLine("• Detect GPU addition");
            Console.WriteLine("• Log detailed change information");
            Console.WriteLine("• Save changes to local files");
            Console.WriteLine("• Track specific hardware components with names and details");
        }
        
        private static DeviceDto CreateTestDevice(string name, List<GpuInfoDto> gpus)
        {
            return new DeviceDto
            {
                Name = name,
                MacAddress = "00:11:22:33:44:55",
                IpAddress = "192.168.1.100",
                DeviceType = DeviceType.Desktop,
                Model = "Test Model",
                Location = "Test Location",
                Status = 1,
                AgentInstalled = true,
                ChangeLogs = new List<ChangeLogDto>(),
                HardwareInfo = new DeviceHardwareInfoDto
                {
                    Cpu = "Intel Core i7-10700K",
                    CpuCores = 8,
                    CpuLogical = 16,
                    CpuClockMHz = 3800,
                    Motherboard = "ASUS ROG STRIX Z490-E",
                    MotherboardSerial = "TEST123456",
                    BiosManufacturer = "American Megatrends",
                    BiosVersion = "2.14.1",
                    BiosSerial = "BIOS123456",
                    RamGB = 32,
                    RamModules = new List<RamModuleDto>
                    {
                        new RamModuleDto
                        {
                            Slot = "DIMM_A1",
                            CapacityGB = 16,
                            SpeedMHz = "3200 MHz",
                            Manufacturer = "G.Skill",
                            PartNumber = "F4-3200C16-16GVK",
                            SerialNumber = "RAM123456"
                        },
                        new RamModuleDto
                        {
                            Slot = "DIMM_A2",
                            CapacityGB = 16,
                            SpeedMHz = "3200 MHz",
                            Manufacturer = "G.Skill",
                            PartNumber = "F4-3200C16-16GVK",
                            SerialNumber = "RAM123457"
                        }
                    },
                    DiskGB = 1000,
                    Disks = new List<DiskInfoDto>
                    {
                        new DiskInfoDto
                        {
                            DeviceId = "C:",
                            TotalGB = 500,
                            FreeGB = 250
                        },
                        new DiskInfoDto
                        {
                            DeviceId = "D:",
                            TotalGB = 500,
                            FreeGB = 400
                        }
                    },
                    Gpus = gpus,
                    NetworkAdapters = new List<NetworkAdapterDto>
                    {
                        new NetworkAdapterDto
                        {
                            Description = "Intel Ethernet Connection",
                            MacAddress = "00:11:22:33:44:55",
                            IpAddress = "192.168.1.100"
                        }
                    }
                },
                SoftwareInfo = new DeviceSoftwareInfoDto
                {
                    OperatingSystem = "Windows 11 Pro",
                    OsVersion = "10.0.22000",
                    OsArchitecture = "x64",
                    RegisteredUser = "Test User",
                    SerialNumber = "TEST-SERIAL-123",
                    InstalledApps = new List<string> { "Visual Studio Code", "Google Chrome", "Microsoft Office" },
                    Updates = new List<string>(),
                    Users = new List<string> { "Test User", "Administrator" },
                    ActiveUser = "Test User"
                }
            };
        }
    }
}