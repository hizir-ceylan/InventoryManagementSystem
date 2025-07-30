using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Inventory.Agent.Windows.Models;
using Inventory.Agent.Windows.Services;

namespace Inventory.Agent.Windows.Testing
{
    /// <summary>
    /// Test utility to demonstrate enhanced software change detection functionality
    /// </summary>
    public class SoftwareChangeDetectionTest
    {
        public static async Task RunTestAsync()
        {
            Console.WriteLine("=== Software Change Detection Test ===");
            
            // Create a test storage directory
            var testDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), 
                "InventoryManagementSystem", "SoftwareTest");
            Directory.CreateDirectory(testDirectory);
            
            var deviceStateService = new DeviceStateService(testDirectory);
            
            Console.WriteLine($"Test directory: {testDirectory}");
            Console.WriteLine("1. Creating initial device state...");
            
            // Create initial device state with software
            var initialDevice = CreateTestDevice(
                new List<string> { "Visual Studio Code", "Chrome", "Office 365" },
                new List<string> { "KB5000001", "KB5000002" },
                new List<string> { "Admin", "TestUser1" },
                "Windows 11",
                "22H2",
                "TestUser1"
            );
            
            await deviceStateService.SaveCurrentStateAsync(initialDevice);
            Console.WriteLine("✓ Initial state saved with 3 apps, 2 updates, 2 users");
            
            Console.WriteLine("\n2. Simulating software changes...");
            Console.WriteLine("   - Installing Teams application");
            Console.WriteLine("   - Installing new Windows update (KB5000003)");
            Console.WriteLine("   - Adding new user (TestUser2)");
            Console.WriteLine("   - Changing OS version (22H2 -> 23H2)");
            Console.WriteLine("   - Changing active user (TestUser1 -> TestUser2)");
            
            // Create updated device state with software changes
            var updatedDevice = CreateTestDevice(
                new List<string> { "Visual Studio Code", "Chrome", "Office 365", "Teams" },
                new List<string> { "KB5000001", "KB5000002", "KB5000003" },
                new List<string> { "Admin", "TestUser1", "TestUser2" },
                "Windows 11",
                "23H2",
                "TestUser2"
            );
            
            // Detect changes
            var previousDevice = await deviceStateService.GetLastKnownStateAsync();
            var changes = await deviceStateService.DetectChangesAsync(updatedDevice, previousDevice);
            
            Console.WriteLine($"\n3. Change detection results: {changes.Count} changes detected");
            
            if (changes.Count > 0)
            {
                Console.WriteLine("\nDetailed changes:");
                foreach (var change in changes.OrderBy(c => c.ChangeType))
                {
                    if (string.IsNullOrEmpty(change.OldValue))
                    {
                        Console.WriteLine($"   ✓ {change.ChangeType}: '{change.NewValue}' (newly added)");
                    }
                    else if (string.IsNullOrEmpty(change.NewValue))
                    {
                        Console.WriteLine($"   ✗ {change.ChangeType}: '{change.OldValue}' (removed)");
                    }
                    else
                    {
                        Console.WriteLine($"   → {change.ChangeType}: '{change.OldValue}' → '{change.NewValue}'");
                    }
                }
                
                // Verify expected changes
                var expectedChanges = new Dictionary<string, string>
                {
                    { "Application Installed", "Teams" },
                    { "Application Count", "4" },
                    { "Software Update Installed", "KB5000003" },
                    { "Software Update Count", "3" },
                    { "User Installed", "TestUser2" },
                    { "User Count", "3" },
                    { "OS Version", "23H2" },
                    { "Active User", "TestUser2" }
                };
                
                Console.WriteLine("\n4. Validation:");
                foreach (var expected in expectedChanges)
                {
                    var found = changes.Any(c => c.ChangeType == expected.Key && 
                        (c.NewValue == expected.Value || c.OldValue == expected.Value));
                    Console.WriteLine($"   {(found ? "✓" : "✗")} {expected.Key}: Expected '{expected.Value}'");
                }
            }
            else
            {
                Console.WriteLine("   No changes detected (unexpected!)");
            }
            
            Console.WriteLine("\n5. Saving updated state for next test...");
            await deviceStateService.SaveCurrentStateAsync(updatedDevice);
            
            Console.WriteLine("\n=== Test completed ===");
        }
        
        private static DeviceDto CreateTestDevice(
            List<string> installedApps, 
            List<string> updates, 
            List<string> users,
            string os, 
            string osVersion, 
            string activeUser)
        {
            return new DeviceDto
            {
                Name = "Test-PC",
                MacAddress = "00:11:22:33:44:55",
                IpAddress = "192.168.1.100",
                Model = "Test Model",
                Location = "Test Location",
                SoftwareInfo = new DeviceSoftwareInfoDto
                {
                    OperatingSystem = os,
                    OsVersion = osVersion,
                    OsArchitecture = "x64",
                    RegisteredUser = "Test User",
                    SerialNumber = "TEST123",
                    ActiveUser = activeUser,
                    InstalledApps = installedApps,
                    Updates = updates,
                    Users = users
                },
                HardwareInfo = new DeviceHardwareInfoDto
                {
                    Cpu = "Test CPU",
                    RamGB = 16,
                    Motherboard = "Test Motherboard",
                    MotherboardSerial = "MB123",
                    BiosManufacturer = "Test BIOS",
                    BiosVersion = "1.0",
                    BiosSerial = "BIOS123",
                    RamModules = new List<RamModuleDto>(),
                    Disks = new List<DiskInfoDto>(),
                    Gpus = new List<GpuInfoDto>(),
                    NetworkAdapters = new List<NetworkAdapterDto>()
                },
                ChangeLogs = new List<ChangeLogDto>()
            };
        }
    }
}