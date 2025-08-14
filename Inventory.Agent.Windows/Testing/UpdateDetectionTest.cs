using System;
using System.Threading.Tasks;
using Inventory.Agent.Windows.Services;

namespace Inventory.Agent.Windows.Testing
{
    /// <summary>
    /// Test utility for validating the update detection functionality
    /// </summary>
    public class UpdateDetectionTest
    {
        public static async Task RunTestAsync()
        {
            Console.WriteLine("Update Detection Test");
            Console.WriteLine("===================");
            Console.WriteLine();
            
            try
            {
                // Initialize the update detection service
                var updateDetectionService = new UpdateDetectionService();
                Console.WriteLine("✓ Update Detection Service initialized");
                
                // Generate a test device ID
                var deviceId = Guid.NewGuid();
                Console.WriteLine($"✓ Test device ID: {deviceId}");
                Console.WriteLine();
                
                // Test Windows Update detection
                Console.WriteLine("Testing Windows Update detection...");
                var updates = await updateDetectionService.DetectAllUpdatesAsync(deviceId);
                
                Console.WriteLine($"✓ Update detection completed: {updates.Count} updates found");
                Console.WriteLine();
                
                if (updates.Count > 0)
                {
                    Console.WriteLine("Detected Updates:");
                    foreach (var update in updates)
                    {
                        Console.WriteLine($"  - {update.UpdateType}: {update.Title}");
                        Console.WriteLine($"    Priority: {update.Priority}, Status: {update.Status}");
                        if (!string.IsNullOrEmpty(update.KBNumber))
                        {
                            Console.WriteLine($"    KB: {update.KBNumber}");
                        }
                        if (update.SizeInMB.HasValue)
                        {
                            Console.WriteLine($"    Size: {update.SizeInMB.Value:F2} MB");
                        }
                        Console.WriteLine();
                    }
                }
                else
                {
                    Console.WriteLine("No updates were detected. This could mean:");
                    Console.WriteLine("  1. All updates are already installed");
                    Console.WriteLine("  2. Windows Update service is not accessible");
                    Console.WriteLine("  3. The system is running on a non-Windows platform");
                    Console.WriteLine();
                }
                
                Console.WriteLine("✓ Update detection test completed successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Update detection test failed: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }
    }
}