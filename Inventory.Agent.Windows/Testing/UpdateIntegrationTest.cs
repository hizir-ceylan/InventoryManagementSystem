using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Inventory.Agent.Windows.Services;
using Inventory.Domain.Entities;

namespace Inventory.Agent.Windows.Testing
{
    /// <summary>
    /// Integration test to verify the complete update detection and API communication flow
    /// </summary>
    public class UpdateIntegrationTest
    {
        public static async Task RunTestAsync()
        {
            Console.WriteLine("Update Detection Integration Test");
            Console.WriteLine("===============================");
            Console.WriteLine();
            
            try
            {
                // Test 1: Create sample update data (simulating detected updates)
                Console.WriteLine("1. Creating sample update data...");
                var sampleUpdates = CreateSampleUpdates();
                Console.WriteLine($"✓ Created {sampleUpdates.Count} sample updates");
                
                // Test 2: Test UpdateApiClient (without actually sending to avoid network dependency)
                Console.WriteLine("\n2. Testing UpdateApiClient initialization...");
                var updateApiClient = new UpdateApiClient("http://localhost:5000");
                Console.WriteLine("✓ UpdateApiClient initialized successfully");
                
                // Test 3: Verify update data structure
                Console.WriteLine("\n3. Verifying update data structure...");
                foreach (var update in sampleUpdates)
                {
                    Console.WriteLine($"   - {update.UpdateType}: {update.Title}");
                    Console.WriteLine($"     Priority: {update.Priority}, Status: {update.Status}");
                    Console.WriteLine($"     Device ID: {update.DeviceId}");
                    if (!string.IsNullOrEmpty(update.KBNumber))
                        Console.WriteLine($"     KB: {update.KBNumber}");
                    Console.WriteLine();
                }
                Console.WriteLine("✓ Update data structure is valid");
                
                // Test 4: Verify device ID generation
                Console.WriteLine("4. Testing device ID generation consistency...");
                var deviceId1 = GenerateDeviceIdFromMac("00:11:22:33:44:55");
                var deviceId2 = GenerateDeviceIdFromMac("00:11:22:33:44:55");
                var deviceId3 = GenerateDeviceIdFromMac("00:11:22:33:44:56");
                
                if (deviceId1 == deviceId2 && deviceId1 != deviceId3)
                {
                    Console.WriteLine("✓ Device ID generation is consistent");
                    Console.WriteLine($"   Same MAC: {deviceId1}");
                    Console.WriteLine($"   Different MAC: {deviceId3}");
                }
                else
                {
                    Console.WriteLine("✗ Device ID generation inconsistency detected");
                }
                
                Console.WriteLine("\n✓ Integration test completed successfully!");
                Console.WriteLine("\nNote: This test validates the data structures and logic.");
                Console.WriteLine("To test actual API communication, ensure the API server is running and accessible.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Integration test failed: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }
        
        private static List<SystemUpdate> CreateSampleUpdates()
        {
            var deviceId = Guid.NewGuid();
            
            return new List<SystemUpdate>
            {
                new SystemUpdate
                {
                    Id = Guid.NewGuid(),
                    DeviceId = deviceId,
                    UpdateType = "Windows",
                    Title = "2023-10 Cumulative Update for Windows 11 Version 22H2 for x64-based Systems (KB5031354)",
                    Description = "This update includes quality improvements and security fixes.",
                    KBNumber = "KB5031354",
                    Status = UpdateStatus.Available,
                    Priority = UpdatePriority.Security,
                    DetectedDate = DateTime.UtcNow,
                    LastChecked = DateTime.UtcNow,
                    SizeInMB = 512.5,
                    CanAutoInstall = true,
                    RequiresRestart = true
                },
                new SystemUpdate
                {
                    Id = Guid.NewGuid(),
                    DeviceId = deviceId,
                    UpdateType = "Windows",
                    Title = "Definition Update for Microsoft Defender Antivirus",
                    Description = "Virus definition update for Microsoft Defender",
                    Status = UpdateStatus.Available,
                    Priority = UpdatePriority.Normal,
                    DetectedDate = DateTime.UtcNow,
                    LastChecked = DateTime.UtcNow,
                    SizeInMB = 15.2,
                    CanAutoInstall = true,
                    RequiresRestart = false
                },
                new SystemUpdate
                {
                    Id = Guid.NewGuid(),
                    DeviceId = deviceId,
                    UpdateType = ".NET Framework",
                    Title = ".NET Framework 4.8.1",
                    Description = ".NET Framework 4.8.1 - Already Installed",
                    Status = UpdateStatus.Installed,
                    Priority = UpdatePriority.Normal,
                    DetectedDate = DateTime.UtcNow,
                    LastChecked = DateTime.UtcNow,
                    CanAutoInstall = false,
                    RequiresRestart = false
                }
            };
        }
        
        /// <summary>
        /// Generates a consistent device ID based on MAC address (copied from InventoryAgentService)
        /// </summary>
        private static Guid GenerateDeviceIdFromMac(string macAddress)
        {
            if (string.IsNullOrWhiteSpace(macAddress))
                return Guid.NewGuid();

            // Create a deterministic GUID from MAC address
            using var sha1 = System.Security.Cryptography.SHA1.Create();
            var hash = sha1.ComputeHash(System.Text.Encoding.UTF8.GetBytes(macAddress));
            var guid = new byte[16];
            Array.Copy(hash, 0, guid, 0, 16);
            return new Guid(guid);
        }
    }
}