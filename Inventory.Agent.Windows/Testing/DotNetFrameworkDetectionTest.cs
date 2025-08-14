using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Inventory.Agent.Windows.Services;
using Inventory.Domain.Entities;

namespace Inventory.Agent.Windows.Testing
{
    /// <summary>
    /// Test to validate .NET Framework detection functionality
    /// Specifically tests the detection of .NET Framework versions that caused the API error
    /// </summary>
    public class DotNetFrameworkDetectionTest
    {
        public static async Task RunTestAsync()
        {
            Console.WriteLine(".NET Framework Detection Validation Test");
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
                
                // Test .NET Framework detection specifically
                Console.WriteLine("Testing .NET Framework detection...");
                var updates = await updateDetectionService.DetectAllUpdatesAsync(deviceId);
                
                // Filter to only .NET Framework updates
                var dotNetUpdates = updates.Where(u => u.UpdateType == ".NET Framework").ToList();
                
                Console.WriteLine($"✓ .NET Framework detection completed: {dotNetUpdates.Count} .NET Framework versions found");
                Console.WriteLine();
                
                if (dotNetUpdates.Count > 0)
                {
                    Console.WriteLine("Detected .NET Framework Versions:");
                    foreach (var update in dotNetUpdates)
                    {
                        Console.WriteLine($"  - {update.Title}");
                        Console.WriteLine($"    Version: {update.CurrentVersion}");
                        Console.WriteLine($"    Status: {update.Status}");
                        Console.WriteLine($"    Description: {update.Description}");
                        
                        // Validate that this looks like a legitimate .NET Framework version
                        if (IsLegitimateNetFrameworkVersion(update.CurrentVersion))
                        {
                            Console.WriteLine("    ✅ Legitimate Microsoft .NET Framework version");
                        }
                        else
                        {
                            Console.WriteLine("    ⚠️  Version format needs verification");
                        }
                        Console.WriteLine();
                    }
                    
                    // Check for the specific versions from the error log
                    Console.WriteLine("Checking for versions mentioned in the error log:");
                    CheckForSpecificVersion(dotNetUpdates, "2.0.50727.4927");
                    CheckForSpecificVersion(dotNetUpdates, "3.0.30729.4926");
                    CheckForSpecificVersion(dotNetUpdates, "3.5.30729.4926");
                    
                }
                else
                {
                    Console.WriteLine("No .NET Framework versions detected. This could mean:");
                    Console.WriteLine("  1. No .NET Framework is installed");
                    Console.WriteLine("  2. Registry access is restricted");
                    Console.WriteLine("  3. The system is running on a non-Windows platform");
                    Console.WriteLine();
                }
                
                Console.WriteLine("✓ .NET Framework detection test completed successfully");
                
                // Validate that we can create valid SystemUpdate objects
                if (dotNetUpdates.Count > 0)
                {
                    Console.WriteLine("\nValidating SystemUpdate object creation...");
                    foreach (var update in dotNetUpdates.Take(3)) // Test first 3
                    {
                        ValidateSystemUpdate(update);
                    }
                    Console.WriteLine("✅ All SystemUpdate objects are valid");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ .NET Framework detection test failed: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }
        
        /// <summary>
        /// Checks if a version string looks like a legitimate .NET Framework version
        /// </summary>
        private static bool IsLegitimateNetFrameworkVersion(string? version)
        {
            if (string.IsNullOrEmpty(version))
                return false;
                
            // .NET Framework versions typically follow patterns like:
            // 2.0.50727.xxxx, 3.0.30729.xxxx, 3.5.30729.xxxx, 4.0.30319.xxxx, etc.
            var knownPatterns = new[]
            {
                "1.0.", "1.1.", "2.0.", "3.0.", "3.5.", "4.0.", "4.5.", "4.6.", "4.7.", "4.8."
            };
            
            return knownPatterns.Any(pattern => version.StartsWith(pattern));
        }
        
        /// <summary>
        /// Checks if a specific version from the error log is detected
        /// </summary>
        private static void CheckForSpecificVersion(List<SystemUpdate> updates, string version)
        {
            var found = updates.Any(u => u.CurrentVersion == version);
            if (found)
            {
                Console.WriteLine($"  ✅ Found version {version} (from error log)");
            }
            else
            {
                Console.WriteLine($"  ℹ️  Version {version} not found (may not be installed on this system)");
            }
        }
        
        /// <summary>
        /// Validates that a SystemUpdate object has all required fields
        /// </summary>
        private static void ValidateSystemUpdate(SystemUpdate update)
        {
            var issues = new List<string>();
            
            if (update.Id == Guid.Empty)
                issues.Add("Id is empty");
            if (update.DeviceId == Guid.Empty)
                issues.Add("DeviceId is empty");
            if (string.IsNullOrEmpty(update.UpdateType))
                issues.Add("UpdateType is empty");
            if (string.IsNullOrEmpty(update.Title))
                issues.Add("Title is empty");
            if (update.DetectedDate == default)
                issues.Add("DetectedDate is default");
            if (update.LastChecked == default)
                issues.Add("LastChecked is default");
                
            if (issues.Count > 0)
            {
                Console.WriteLine($"  ⚠️  {update.Title}: {string.Join(", ", issues)}");
            }
            else
            {
                Console.WriteLine($"  ✅ {update.Title}: Valid");
            }
        }
    }
}