using System;
using System.Collections.Generic;
using System.Linq;
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
    /// Test to validate the SystemUpdate save fix with the exact scenario from the error log
    /// </summary>
    public class UpdateSaveTest
    {
        /// <summary>
        /// Test the SaveUpdatesAsync fix with real-world data from the error log
        /// </summary>
        public static async Task<bool> TestUpdateSaveFix()
        {
            Console.WriteLine("Testing SystemUpdate Save Fix");
            Console.WriteLine("============================");
            
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

                // Test 1: Create test device first
                Console.WriteLine("1. Creating test device...");
                var deviceId = Guid.Parse("9c17bdc2-8f23-f55b-945f-9ef3f7d442a2");
                var testDevice = new Device
                {
                    Id = deviceId,
                    Name = "Test-Windows-PC",
                    DeviceType = DeviceType.Desktop,
                    MacAddress = "00:11:22:33:44:55",
                    IpAddress = "192.168.1.100",
                    Status = 1, // Active status as int
                    ManagementType = ManagementType.Agent,
                    CreatedAt = DateTime.UtcNow
                };
                
                context.Devices.Add(testDevice);
                await context.SaveChangesAsync();
                Console.WriteLine("✓ Test device created successfully");

                // Test 2: Create the exact update data from the error log
                Console.WriteLine("\n2. Creating test update data from error log...");
                var testUpdates = CreateTestDataFromLog();
                Console.WriteLine($"✓ Created {testUpdates.Count} test updates");

                // Test 3: Try to save the updates (this should now work)
                Console.WriteLine("\n3. Testing SaveUpdatesAsync...");
                var savedCount = await updateService.SaveUpdatesAsync(testUpdates);
                Console.WriteLine($"✓ Successfully saved {savedCount} updates");

                // Test 4: Verify the updates were saved correctly
                Console.WriteLine("\n4. Verifying saved updates...");
                var savedUpdates = await context.SystemUpdates
                    .Where(u => u.DeviceId == deviceId)
                    .ToListAsync();
                
                Console.WriteLine($"✓ Found {savedUpdates.Count} saved updates in database");
                
                foreach (var update in savedUpdates)
                {
                    Console.WriteLine($"   - {update.UpdateType}: {update.Title} (Status: {update.Status})");
                }

                // Test 5: Test duplicate handling - try to save the same updates again
                Console.WriteLine("\n5. Testing duplicate handling...");
                var duplicateSavedCount = await updateService.SaveUpdatesAsync(testUpdates);
                Console.WriteLine($"✓ Duplicate save handled: {duplicateSavedCount} updates processed");
                
                // Verify no duplicates were created
                var finalCount = await context.SystemUpdates
                    .Where(u => u.DeviceId == deviceId)
                    .CountAsync();
                Console.WriteLine($"✓ Final update count: {finalCount} (should be {testUpdates.Count})");

                // Test 6: Test with non-existent device ID
                Console.WriteLine("\n6. Testing with non-existent device...");
                var invalidUpdates = CreateTestDataWithInvalidDevice();
                var invalidSavedCount = await updateService.SaveUpdatesAsync(invalidUpdates);
                Console.WriteLine($"✓ Invalid device test: {invalidSavedCount} updates saved (should be 0)");

                var isSuccess = savedCount == testUpdates.Count && 
                              finalCount == testUpdates.Count && 
                              invalidSavedCount == 0;

                Console.WriteLine($"\n✓ All tests completed successfully: {isSuccess}");
                return isSuccess;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n✗ Test failed: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return false;
            }
        }

        /// <summary>
        /// Creates the exact test data from the error log
        /// </summary>
        private static List<SystemUpdate> CreateTestDataFromLog()
        {
            var deviceId = Guid.Parse("9c17bdc2-8f23-f55b-945f-9ef3f7d442a2");
            
            return new List<SystemUpdate>
            {
                new SystemUpdate
                {
                    Id = Guid.Parse("5619a00e-216b-4f70-85ed-a3b29b601e40"),
                    DeviceId = deviceId,
                    UpdateType = ".NET Framework",
                    Title = ".NET Framework 2.0.50727.4927",
                    Description = ".NET Framework 2.0.50727.4927 - Yüklü",
                    KBNumber = null,
                    CurrentVersion = "2.0.50727.4927",
                    LatestVersion = null,
                    SizeInMB = null,
                    Status = UpdateStatus.Installed,
                    Priority = UpdatePriority.Normal,
                    DetectedDate = DateTime.Parse("2025-08-14T16:13:50.501618"),
                    LastChecked = DateTime.Parse("2025-08-14T16:13:50.5016685"),
                    ReleaseDate = null,
                    CanAutoInstall = false,
                    RequiresRestart = false,
                    SecurityBulletinId = null,
                    CreatedAt = DateTime.Parse("2025-08-14T13:13:50.5013214Z"),
                    UpdatedAt = DateTime.Parse("2025-08-14T13:13:50.5013218Z")
                },
                new SystemUpdate
                {
                    Id = Guid.Parse("42f07e41-93f6-41d5-83a2-319d214efc11"),
                    DeviceId = deviceId,
                    UpdateType = ".NET Framework",
                    Title = ".NET Framework 3.0.30729.4926",
                    Description = ".NET Framework 3.0.30729.4926 - Yüklü",
                    KBNumber = null,
                    CurrentVersion = "3.0.30729.4926",
                    LatestVersion = null,
                    SizeInMB = null,
                    Status = UpdateStatus.Installed,
                    Priority = UpdatePriority.Normal,
                    DetectedDate = DateTime.Parse("2025-08-14T16:13:50.5017678"),
                    LastChecked = DateTime.Parse("2025-08-14T16:13:50.5017689"),
                    ReleaseDate = null,
                    CanAutoInstall = false,
                    RequiresRestart = false,
                    SecurityBulletinId = null,
                    CreatedAt = DateTime.Parse("2025-08-14T13:13:50.501765Z"),
                    UpdatedAt = DateTime.Parse("2025-08-14T13:13:50.5017651Z")
                },
                new SystemUpdate
                {
                    Id = Guid.Parse("ee115179-d7aa-4892-b0ee-4359d91f49a0"),
                    DeviceId = deviceId,
                    UpdateType = ".NET Framework",
                    Title = ".NET Framework 3.5.30729.4926",
                    Description = ".NET Framework 3.5.30729.4926 - Yüklü",
                    KBNumber = null,
                    CurrentVersion = "3.5.30729.4926",
                    LatestVersion = null,
                    SizeInMB = null,
                    Status = UpdateStatus.Installed,
                    Priority = UpdatePriority.Normal,
                    DetectedDate = DateTime.Parse("2025-08-14T16:13:50.5017695"),
                    LastChecked = DateTime.Parse("2025-08-14T16:13:50.5017697"),
                    ReleaseDate = null,
                    CanAutoInstall = false,
                    RequiresRestart = false,
                    SecurityBulletinId = null,
                    CreatedAt = DateTime.Parse("2025-08-14T13:13:50.5017691Z"),
                    UpdatedAt = DateTime.Parse("2025-08-14T13:13:50.5017692Z")
                }
            };
        }

        /// <summary>
        /// Creates test data with a non-existent device ID to test error handling
        /// </summary>
        private static List<SystemUpdate> CreateTestDataWithInvalidDevice()
        {
            var invalidDeviceId = Guid.NewGuid(); // Random device ID that doesn't exist
            
            return new List<SystemUpdate>
            {
                new SystemUpdate
                {
                    Id = Guid.NewGuid(),
                    DeviceId = invalidDeviceId,
                    UpdateType = ".NET Framework",
                    Title = ".NET Framework 4.8",
                    Description = ".NET Framework 4.8 - Test",
                    Status = UpdateStatus.Installed,
                    Priority = UpdatePriority.Normal,
                    DetectedDate = DateTime.UtcNow,
                    LastChecked = DateTime.UtcNow,
                    CanAutoInstall = false,
                    RequiresRestart = false
                }
            };
        }
    }
}