using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Inventory.Data;
using Inventory.Domain.Entities;
using Inventory.Api.Services;
using Inventory.Shared.Utils;

namespace Inventory.Api.Testing
{
    /// <summary>
    /// Test to validate device validation functionality in UpdateService
    /// Tests the scenario where updates are reported for non-existent devices
    /// </summary>
    public class DeviceValidationTest
    {
        /// <summary>
        /// Test the device validation functionality
        /// </summary>
        public static async Task<bool> TestDeviceValidation()
        {
            Console.WriteLine("Testing Device Validation in Update Service");
            Console.WriteLine("==========================================");
            
            try
            {
                // Create in-memory database for testing
                var options = new DbContextOptionsBuilder<InventoryDbContext>()
                    .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                    .Options;

                using var context = new InventoryDbContext(options);
                
                // Create a logger factory for testing
                using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
                var logger = loggerFactory.CreateLogger<UpdateService>();
                
                var updateService = new UpdateService(context, logger);

                // Test 1: Test DeviceExistsAsync with non-existent device
                Console.WriteLine("1. Testing DeviceExistsAsync with non-existent device...");
                var nonExistentDeviceId = Guid.Parse("9c17bdc2-8f23-f55b-945f-9ef3f7d442a2");
                var deviceExists = await updateService.DeviceExistsAsync(nonExistentDeviceId);
                
                if (!deviceExists)
                {
                    Console.WriteLine("✓ DeviceExistsAsync correctly returned false for non-existent device");
                }
                else
                {
                    Console.WriteLine("❌ DeviceExistsAsync incorrectly returned true for non-existent device");
                    return false;
                }

                // Test 2: Create a device and test with existing device
                Console.WriteLine("2. Creating test device and testing with existing device...");
                var testDeviceId = Guid.NewGuid();
                var testDevice = new Device
                {
                    Id = testDeviceId,
                    Name = "Test-Device",
                    DeviceType = DeviceType.Desktop,
                    MacAddress = "00:11:22:33:44:55",
                    IpAddress = "192.168.1.100",
                    Model = "Test Model",
                    Location = "Test Location",
                    Status = (int)DeviceStatus.Active,
                    CreatedAt = TimeZoneHelper.GetUtcNowForStorage(),
                    AgentInstalled = true,
                    ManagementType = ManagementType.Agent,
                    DiscoveryMethod = DiscoveryMethod.Agent
                };

                context.Set<Device>().Add(testDevice);
                await context.SaveChangesAsync();

                var existingDeviceExists = await updateService.DeviceExistsAsync(testDeviceId);
                if (existingDeviceExists)
                {
                    Console.WriteLine("✓ DeviceExistsAsync correctly returned true for existing device");
                }
                else
                {
                    Console.WriteLine("❌ DeviceExistsAsync incorrectly returned false for existing device");
                    return false;
                }

                // Test 3: Test SaveUpdatesAsync with non-existent device (should be skipped)
                Console.WriteLine("3. Testing SaveUpdatesAsync with updates for non-existent device...");
                var updatesForNonExistentDevice = new List<SystemUpdate>
                {
                    new SystemUpdate
                    {
                        Id = Guid.NewGuid(),
                        DeviceId = nonExistentDeviceId,
                        UpdateType = "Office (C2R)",
                        Title = "Test Update for Non-Existent Device",
                        Description = "This should be skipped",
                        CurrentVersion = "1.0.0",
                        LatestVersion = "1.1.0",
                        Status = UpdateStatus.Available,
                        Priority = UpdatePriority.Normal,
                        DetectedDate = TimeZoneHelper.GetUtcNowForStorage(),
                        LastChecked = TimeZoneHelper.GetUtcNowForStorage(),
                        CreatedAt = TimeZoneHelper.GetUtcNowForStorage(),
                        UpdatedAt = TimeZoneHelper.GetUtcNowForStorage()
                    }
                };

                var savedCount = await updateService.SaveUpdatesAsync(updatesForNonExistentDevice);
                if (savedCount == 0)
                {
                    Console.WriteLine("✓ SaveUpdatesAsync correctly skipped updates for non-existent device");
                }
                else
                {
                    Console.WriteLine($"❌ SaveUpdatesAsync incorrectly saved {savedCount} updates for non-existent device");
                    return false;
                }

                // Test 4: Test SaveUpdatesAsync with existing device (should be saved)
                Console.WriteLine("4. Testing SaveUpdatesAsync with updates for existing device...");
                var updatesForExistingDevice = new List<SystemUpdate>
                {
                    new SystemUpdate
                    {
                        Id = Guid.NewGuid(),
                        DeviceId = testDeviceId,
                        UpdateType = "Windows",
                        Title = "Test Update for Existing Device",
                        Description = "This should be saved",
                        CurrentVersion = "1.0.0",
                        LatestVersion = "1.1.0",
                        Status = UpdateStatus.Available,
                        Priority = UpdatePriority.Normal,
                        DetectedDate = TimeZoneHelper.GetUtcNowForStorage(),
                        LastChecked = TimeZoneHelper.GetUtcNowForStorage(),
                        CreatedAt = TimeZoneHelper.GetUtcNowForStorage(),
                        UpdatedAt = TimeZoneHelper.GetUtcNowForStorage()
                    }
                };

                var savedCountForExisting = await updateService.SaveUpdatesAsync(updatesForExistingDevice);
                if (savedCountForExisting == 1)
                {
                    Console.WriteLine("✓ SaveUpdatesAsync correctly saved updates for existing device");
                }
                else
                {
                    Console.WriteLine($"❌ SaveUpdatesAsync incorrectly saved {savedCountForExisting} updates (expected 1)");
                    return false;
                }

                // Test 5: Verify updates were saved correctly
                Console.WriteLine("5. Verifying saved updates...");
                var savedUpdates = await updateService.GetUpdatesByDeviceAsync(testDeviceId);
                var savedUpdatesList = savedUpdates.ToList();
                
                if (savedUpdatesList.Count == 1)
                {
                    Console.WriteLine("✓ GetUpdatesByDeviceAsync correctly returned saved updates");
                }
                else
                {
                    Console.WriteLine($"❌ GetUpdatesByDeviceAsync returned {savedUpdatesList.Count} updates (expected 1)");
                    return false;
                }

                Console.WriteLine();
                Console.WriteLine("✅ All device validation tests passed!");
                Console.WriteLine("   - Non-existent devices are correctly identified");
                Console.WriteLine("   - Updates for non-existent devices are skipped");
                Console.WriteLine("   - Updates for existing devices are saved properly");
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Test failed with error: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return false;
            }
        }
    }
}