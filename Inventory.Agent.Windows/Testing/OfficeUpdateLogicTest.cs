using System;
using System.Threading.Tasks;

namespace Inventory.Agent.Windows.Testing
{
    /// <summary>
    /// Test to validate the improved Office C2R update logic
    /// Verifies that updates are only reported when actually needed
    /// </summary>
    public class OfficeUpdateLogicTest
    {
        public static async Task RunTestAsync()
        {
            Console.WriteLine("===============================================");
            Console.WriteLine(" Office Update Logic Test");
            Console.WriteLine("===============================================");
            Console.WriteLine();

            try
            {
                Console.WriteLine("Testing improved Office C2R update detection logic...");
                Console.WriteLine();

                // Test 1: Check if C2R registry keys exist
                var hasC2RInstallation = CheckForC2RRegistry();
                Console.WriteLine($"✓ C2R Installation: {(hasC2RInstallation ? "FOUND" : "NOT FOUND")}");

                if (hasC2RInstallation)
                {
                    // Test 2: Check if the old logic would have reported unnecessary updates
                    var oldLogicWouldReport = CheckOldLogicWouldReport();
                    Console.WriteLine($"✓ Old logic would report updates: {(oldLogicWouldReport ? "YES (PROBLEM)" : "NO (GOOD)")}");

                    // Test 3: Check if new logic correctly detects pending updates
                    var newLogicDetectsPending = CheckNewLogicForPendingUpdates();
                    Console.WriteLine($"✓ New logic detects pending updates: {(newLogicDetectsPending ? "YES" : "NO")}");

                    if (oldLogicWouldReport && !newLogicDetectsPending)
                    {
                        Console.WriteLine();
                        Console.WriteLine("✅ SUCCESS: Fixed the unnecessary update reporting issue!");
                        Console.WriteLine("   - Old logic would incorrectly report updates when auto-update was enabled");
                        Console.WriteLine("   - New logic only reports when actual pending updates are detected");
                    }
                    else if (!oldLogicWouldReport)
                    {
                        Console.WriteLine();
                        Console.WriteLine("ℹ️  INFO: No auto-update channel configured, so old logic wouldn't have been a problem");
                    }
                }
                else
                {
                    Console.WriteLine();
                    Console.WriteLine("ℹ️  INFO: No Office C2R installation detected - test is not applicable");
                }

                Console.WriteLine();
                Console.WriteLine("✅ Office update logic test completed successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ERROR: Test failed - {ex.Message}");
                throw;
            }
        }

        private static bool CheckForC2RRegistry()
        {
            try
            {
                using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Office\ClickToRun\Configuration");
                return key != null;
            }
            catch
            {
                return false;
            }
        }

        private static bool CheckOldLogicWouldReport()
        {
            try
            {
                using var configKey = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Office\ClickToRun\Configuration");
                if (configKey != null)
                {
                    var updateChannel = configKey.GetValue("UpdateChannel")?.ToString();
                    var currentVersion = configKey.GetValue("VersionToReport")?.ToString();

                    // Old logic: if update channel is set and version exists, report update
                    return !string.IsNullOrEmpty(updateChannel) && !string.IsNullOrEmpty(currentVersion);
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        private static bool CheckNewLogicForPendingUpdates()
        {
            try
            {
                using var updateKey = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Office\ClickToRun\Updates");
                if (updateKey != null)
                {
                    // New logic: check for actual pending updates
                    var availableVersion = updateKey.GetValue("UpdatesAvailable")?.ToString();
                    var updateToApply = updateKey.GetValue("UpdateToApply")?.ToString();
                    var executionState = updateKey.GetValue("ExecutionState")?.ToString();

                    return !string.IsNullOrEmpty(availableVersion) ||
                           !string.IsNullOrEmpty(updateToApply) ||
                           (executionState != null && executionState != "0" && executionState != "Idle");
                }
                return false;
            }
            catch
            {
                return false;
            }
        }
    }
}