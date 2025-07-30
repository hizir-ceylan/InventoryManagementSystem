using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Inventory.Agent.Windows.Models;

namespace Inventory.Agent.Windows.Services
{
    public class DeviceStateService
    {
        private readonly string _stateDirectory;
        private readonly string _stateFile;
        private readonly string _changesDirectory;

        public DeviceStateService(string storageDirectory)
        {
            _stateDirectory = storageDirectory;
            _stateFile = Path.Combine(storageDirectory, "device_state.json");
            _changesDirectory = Path.Combine(storageDirectory, "Changes");
            
            Directory.CreateDirectory(_stateDirectory);
            Directory.CreateDirectory(_changesDirectory);
        }

        public async Task<DeviceDto?> GetLastKnownStateAsync()
        {
            try
            {
                if (!File.Exists(_stateFile))
                {
                    return null;
                }

                var json = await File.ReadAllTextAsync(_stateFile);
                if (string.IsNullOrEmpty(json))
                {
                    return null;
                }

                return JsonSerializer.Deserialize<DeviceDto>(json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading last known device state: {ex.Message}");
                return null;
            }
        }

        public async Task SaveCurrentStateAsync(DeviceDto device)
        {
            try
            {
                var json = JsonSerializer.Serialize(device, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
                await File.WriteAllTextAsync(_stateFile, json);
                Console.WriteLine("Device state saved successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving device state: {ex.Message}");
            }
        }

        public async Task<List<ChangeLogDto>> DetectChangesAsync(DeviceDto currentDevice, DeviceDto? previousDevice)
        {
            var changes = new List<ChangeLogDto>();

            if (previousDevice == null)
            {
                // First time running - no changes to detect
                Console.WriteLine("No previous device state found - this is the first scan");
                return changes;
            }

            Console.WriteLine("Detecting changes between current and previous device state...");

            // Compare basic device information
            CompareBasicDeviceInfo(currentDevice, previousDevice, changes);
            
            // Compare hardware information
            CompareHardwareInfo(currentDevice.HardwareInfo, previousDevice.HardwareInfo, changes);
            
            // Compare software information
            CompareSoftwareInfo(currentDevice.SoftwareInfo, previousDevice.SoftwareInfo, changes);

            if (changes.Count > 0)
            {
                Console.WriteLine($"Detected {changes.Count} changes");
                
                // Save detailed change information to file
                await SaveChangesToFileAsync(changes);
            }
            else
            {
                Console.WriteLine("No changes detected");
            }

            return changes;
        }

        private void CompareBasicDeviceInfo(DeviceDto current, DeviceDto previous, List<ChangeLogDto> changes)
        {
            if (current.Name != previous.Name)
            {
                changes.Add(new ChangeLogDto
                {
                    Id = Guid.NewGuid(),
                    ChangeDate = DateTime.UtcNow,
                    ChangeType = "Device Name",
                    OldValue = previous.Name ?? "",
                    NewValue = current.Name ?? "",
                    ChangedBy = "Agent"
                });
            }

            if (current.IpAddress != previous.IpAddress)
            {
                changes.Add(new ChangeLogDto
                {
                    Id = Guid.NewGuid(),
                    ChangeDate = DateTime.UtcNow,
                    ChangeType = "IP Address",
                    OldValue = previous.IpAddress ?? "",
                    NewValue = current.IpAddress ?? "",
                    ChangedBy = "Agent"
                });
            }

            if (current.MacAddress != previous.MacAddress)
            {
                changes.Add(new ChangeLogDto
                {
                    Id = Guid.NewGuid(),
                    ChangeDate = DateTime.UtcNow,
                    ChangeType = "MAC Address",
                    OldValue = previous.MacAddress ?? "",
                    NewValue = current.MacAddress ?? "",
                    ChangedBy = "Agent"
                });
            }

            if (current.Model != previous.Model)
            {
                changes.Add(new ChangeLogDto
                {
                    Id = Guid.NewGuid(),
                    ChangeDate = DateTime.UtcNow,
                    ChangeType = "Model",
                    OldValue = previous.Model ?? "",
                    NewValue = current.Model ?? "",
                    ChangedBy = "Agent"
                });
            }

            if (current.Location != previous.Location)
            {
                changes.Add(new ChangeLogDto
                {
                    Id = Guid.NewGuid(),
                    ChangeDate = DateTime.UtcNow,
                    ChangeType = "Location",
                    OldValue = previous.Location ?? "",
                    NewValue = current.Location ?? "",
                    ChangedBy = "Agent"
                });
            }
        }

        private void CompareHardwareInfo(DeviceHardwareInfoDto? current, DeviceHardwareInfoDto? previous, List<ChangeLogDto> changes)
        {
            if (current == null && previous == null) return;
            if (current == null || previous == null)
            {
                changes.Add(new ChangeLogDto
                {
                    Id = Guid.NewGuid(),
                    ChangeDate = DateTime.UtcNow,
                    ChangeType = "Hardware Info",
                    OldValue = previous == null ? "None" : "Present",
                    NewValue = current == null ? "None" : "Present",
                    ChangedBy = "Agent"
                });
                return;
            }

            // Compare CPU
            if (current.Cpu != previous.Cpu)
            {
                changes.Add(new ChangeLogDto
                {
                    Id = Guid.NewGuid(),
                    ChangeDate = DateTime.UtcNow,
                    ChangeType = "CPU",
                    OldValue = previous.Cpu ?? "",
                    NewValue = current.Cpu ?? "",
                    ChangedBy = "Agent"
                });
            }

            // Compare RAM
            if (current.RamGB != previous.RamGB)
            {
                changes.Add(new ChangeLogDto
                {
                    Id = Guid.NewGuid(),
                    ChangeDate = DateTime.UtcNow,
                    ChangeType = "RAM (GB)",
                    OldValue = previous.RamGB.ToString(),
                    NewValue = current.RamGB.ToString(),
                    ChangedBy = "Agent"
                });
            }

            // Compare Disk
            if (current.DiskGB != previous.DiskGB)
            {
                changes.Add(new ChangeLogDto
                {
                    Id = Guid.NewGuid(),
                    ChangeDate = DateTime.UtcNow,
                    ChangeType = "Disk (GB)",
                    OldValue = previous.DiskGB.ToString(),
                    NewValue = current.DiskGB.ToString(),
                    ChangedBy = "Agent"
                });
            }

            // Compare Motherboard
            if (current.Motherboard != previous.Motherboard)
            {
                changes.Add(new ChangeLogDto
                {
                    Id = Guid.NewGuid(),
                    ChangeDate = DateTime.UtcNow,
                    ChangeType = "Motherboard",
                    OldValue = previous.Motherboard ?? "",
                    NewValue = current.Motherboard ?? "",
                    ChangedBy = "Agent"
                });
            }

            // Compare BIOS Version
            if (current.BiosVersion != previous.BiosVersion)
            {
                changes.Add(new ChangeLogDto
                {
                    Id = Guid.NewGuid(),
                    ChangeDate = DateTime.UtcNow,
                    ChangeType = "BIOS Version",
                    OldValue = previous.BiosVersion ?? "",
                    NewValue = current.BiosVersion ?? "",
                    ChangedBy = "Agent"
                });
            }
        }

        private void CompareSoftwareInfo(DeviceSoftwareInfoDto? current, DeviceSoftwareInfoDto? previous, List<ChangeLogDto> changes)
        {
            if (current == null && previous == null) return;
            if (current == null || previous == null)
            {
                changes.Add(new ChangeLogDto
                {
                    Id = Guid.NewGuid(),
                    ChangeDate = DateTime.UtcNow,
                    ChangeType = "Software Info",
                    OldValue = previous == null ? "None" : "Present",
                    NewValue = current == null ? "None" : "Present",
                    ChangedBy = "Agent"
                });
                return;
            }

            // Compare Operating System
            if (current.OperatingSystem != previous.OperatingSystem)
            {
                changes.Add(new ChangeLogDto
                {
                    Id = Guid.NewGuid(),
                    ChangeDate = DateTime.UtcNow,
                    ChangeType = "Operating System",
                    OldValue = previous.OperatingSystem ?? "",
                    NewValue = current.OperatingSystem ?? "",
                    ChangedBy = "Agent"
                });
            }

            // Compare OS Version
            if (current.OsVersion != previous.OsVersion)
            {
                changes.Add(new ChangeLogDto
                {
                    Id = Guid.NewGuid(),
                    ChangeDate = DateTime.UtcNow,
                    ChangeType = "OS Version",
                    OldValue = previous.OsVersion ?? "",
                    NewValue = current.OsVersion ?? "",
                    ChangedBy = "Agent"
                });
            }

            // Compare Active User
            if (current.ActiveUser != previous.ActiveUser)
            {
                changes.Add(new ChangeLogDto
                {
                    Id = Guid.NewGuid(),
                    ChangeDate = DateTime.UtcNow,
                    ChangeType = "Active User",
                    OldValue = previous.ActiveUser ?? "",
                    NewValue = current.ActiveUser ?? "",
                    ChangedBy = "Agent"
                });
            }

            // Compare installed applications count (simplified comparison)
            var currentAppCount = current.InstalledApps?.Count ?? 0;
            var previousAppCount = previous.InstalledApps?.Count ?? 0;
            
            if (currentAppCount != previousAppCount)
            {
                changes.Add(new ChangeLogDto
                {
                    Id = Guid.NewGuid(),
                    ChangeDate = DateTime.UtcNow,
                    ChangeType = "Installed Applications Count",
                    OldValue = previousAppCount.ToString(),
                    NewValue = currentAppCount.ToString(),
                    ChangedBy = "Agent"
                });
            }
        }

        private async Task SaveChangesToFileAsync(List<ChangeLogDto> changes)
        {
            try
            {
                var timestamp = DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss");
                var changesFile = Path.Combine(_changesDirectory, $"device-changes-{timestamp}.json");
                
                var json = JsonSerializer.Serialize(changes, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
                
                await File.WriteAllTextAsync(changesFile, json);
                Console.WriteLine($"Changes saved to: {changesFile}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving changes to file: {ex.Message}");
            }
        }

        public async Task CleanupOldChangesAsync(int retentionHours = 48)
        {
            try
            {
                if (!Directory.Exists(_changesDirectory))
                    return;

                var cutoffTime = DateTime.Now.AddHours(-retentionHours);
                var files = Directory.GetFiles(_changesDirectory, "device-changes-*.json");

                foreach (var file in files)
                {
                    var fileInfo = new FileInfo(file);
                    if (fileInfo.CreationTime < cutoffTime)
                    {
                        File.Delete(file);
                        Console.WriteLine($"Deleted old change file: {Path.GetFileName(file)}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error cleaning up old change files: {ex.Message}");
            }
        }
    }
}