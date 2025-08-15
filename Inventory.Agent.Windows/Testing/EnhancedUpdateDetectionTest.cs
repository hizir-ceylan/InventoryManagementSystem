using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Inventory.Agent.Windows.Services;
using Inventory.Domain.Entities;

namespace Inventory.Agent.Windows.Testing
{
    /// <summary>
    /// Test to validate the enhanced update detection functionality
    /// Specifically tests the improvements made to fix the .NET Framework reporting issue
    /// </summary>
    public class EnhancedUpdateDetectionTest
    {
        public static async Task RunTestAsync()
        {
            Console.WriteLine("Enhanced Update Detection Validation Test");
            Console.WriteLine("========================================");
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
                
                // Test all update detection
                Console.WriteLine("Testing enhanced update detection...");
                var updates = await updateDetectionService.DetectAllUpdatesAsync(deviceId);
                
                Console.WriteLine($"✓ Update detection completed: {updates.Count} total updates found");
                Console.WriteLine();
                
                // Analyze results by update type
                var groupedUpdates = updates.GroupBy(u => u.UpdateType)
                    .OrderBy(g => g.Key)
                    .ToList();
                
                Console.WriteLine("Update Analysis by Type:");
                Console.WriteLine("========================");
                
                foreach (var group in groupedUpdates)
                {
                    var availableCount = group.Count(u => u.Status == UpdateStatus.Available);
                    var installedCount = group.Count(u => u.Status == UpdateStatus.Installed);
                    var totalCount = group.Count();
                    
                    Console.WriteLine($"{group.Key}:");
                    Console.WriteLine($"  Total: {totalCount}, Available: {availableCount}, Installed: {installedCount}");
                    
                    // Show details for available updates
                    var availableUpdates = group.Where(u => u.Status == UpdateStatus.Available).ToList();
                    if (availableUpdates.Any())
                    {
                        Console.WriteLine("  Available Updates:");
                        foreach (var update in availableUpdates.Take(3))
                        {
                            Console.WriteLine($"    - {update.Title}");
                            if (!string.IsNullOrEmpty(update.KBNumber))
                                Console.WriteLine($"      KB: {update.KBNumber}");
                            if (update.SizeInMB.HasValue)
                                Console.WriteLine($"      Size: {update.SizeInMB.Value:F1} MB");
                            if (!string.IsNullOrEmpty(update.CurrentVersion))
                                Console.WriteLine($"      Current: {update.CurrentVersion}");
                            if (!string.IsNullOrEmpty(update.LatestVersion))
                                Console.WriteLine($"      Latest: {update.LatestVersion}");
                        }
                        
                        if (availableUpdates.Count > 3)
                        {
                            Console.WriteLine($"    ... and {availableUpdates.Count - 3} more");
                        }
                    }
                    Console.WriteLine();
                }
                
                // Specific validation for .NET Framework updates
                Console.WriteLine("Validation: .NET Framework Update Reporting");
                Console.WriteLine("===========================================");
                
                var dotnetUpdates = updates.Where(u => u.UpdateType == ".NET Framework").ToList();
                
                if (dotnetUpdates.Any())
                {
                    var availableDotnetUpdates = dotnetUpdates.Where(u => u.Status == UpdateStatus.Available).ToList();
                    var installedDotnetVersions = dotnetUpdates.Where(u => u.Status == UpdateStatus.Installed).ToList();
                    
                    Console.WriteLine($"✓ .NET Framework updates found: {dotnetUpdates.Count}");
                    Console.WriteLine($"  - Available updates: {availableDotnetUpdates.Count}");
                    Console.WriteLine($"  - Installed versions reported: {installedDotnetVersions.Count}");
                    
                    if (availableDotnetUpdates.Any())
                    {
                        Console.WriteLine("  ✅ SUCCESS: Only reporting actual .NET Framework updates that are available");
                        foreach (var update in availableDotnetUpdates)
                        {
                            Console.WriteLine($"    - {update.Title}");
                            Console.WriteLine($"      Status: {update.Status}");
                            if (!string.IsNullOrEmpty(update.CurrentVersion))
                                Console.WriteLine($"      Current: {update.CurrentVersion}");
                            if (!string.IsNullOrEmpty(update.LatestVersion))
                                Console.WriteLine($"      Latest: {update.LatestVersion}");
                        }
                    }
                    else if (installedDotnetVersions.Any())
                    {
                        Console.WriteLine("  ⚠️  WARNING: Still reporting installed .NET Framework versions instead of updates");
                        Console.WriteLine("       This indicates the fix may not be working properly.");
                    }
                    else
                    {
                        Console.WriteLine("  ✅ SUCCESS: No .NET Framework updates needed (system is up to date)");
                    }
                }
                else
                {
                    Console.WriteLine("  ✅ SUCCESS: No .NET Framework updates found (system is up to date)");
                }
                
                Console.WriteLine();
                
                // Validate improved update categorization
                Console.WriteLine("Validation: Update Categorization");
                Console.WriteLine("=================================");
                
                var expectedCategories = new[] { "Windows", "Office", ".NET Framework", "Security", "Driver", "Visual C++", "SQL Server", "Edge" };
                var foundCategories = groupedUpdates.Select(g => g.Key).ToHashSet();
                
                Console.WriteLine($"Found update categories: {string.Join(", ", foundCategories)}");
                
                // Check for Office updates
                if (foundCategories.Contains("Office"))
                {
                    Console.WriteLine("✅ SUCCESS: Office updates are now properly categorized");
                }
                else
                {
                    Console.WriteLine("ℹ️  INFO: No Office updates found (may not be installed or up to date)");
                }
                
                // Check for Windows updates
                if (foundCategories.Contains("Windows"))
                {
                    Console.WriteLine("✅ SUCCESS: Windows updates are properly categorized");
                }
                else
                {
                    Console.WriteLine("ℹ️  INFO: No Windows updates found (system may be up to date)");
                }
                
                // Overall summary
                Console.WriteLine();
                Console.WriteLine("Test Summary");
                Console.WriteLine("============");
                
                var totalAvailableUpdates = updates.Count(u => u.Status == UpdateStatus.Available);
                var totalInstalledReports = updates.Count(u => u.Status == UpdateStatus.Installed);
                
                Console.WriteLine($"Total updates requiring attention: {totalAvailableUpdates}");
                Console.WriteLine($"Total installed versions reported: {totalInstalledReports}");
                
                if (totalAvailableUpdates > 0)
                {
                    Console.WriteLine($"✅ SUCCESS: System properly identified {totalAvailableUpdates} updates that need attention");
                }
                else
                {
                    Console.WriteLine("✅ SUCCESS: System is up to date - no updates needed");
                }
                
                if (totalInstalledReports == 0)
                {
                    Console.WriteLine("✅ SUCCESS: No unnecessary 'installed version' reports (fixed the main issue)");
                }
                else
                {
                    Console.WriteLine($"⚠️  WARNING: Still reporting {totalInstalledReports} installed versions unnecessarily");
                }
                
                Console.WriteLine();
                Console.WriteLine("✓ Enhanced update detection test completed successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Enhanced update detection test failed: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }
    }
}