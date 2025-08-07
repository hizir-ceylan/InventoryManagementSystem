using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Inventory.Agent.Windows.Models;
using Inventory.Shared.Utils;

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
                    ChangeDate = TimeZoneHelper.GetTurkeyTime(),
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
                    ChangeDate = TimeZoneHelper.GetTurkeyTime(),
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
                    ChangeDate = TimeZoneHelper.GetTurkeyTime(),
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
                    ChangeDate = TimeZoneHelper.GetTurkeyTime(),
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
                    ChangeDate = TimeZoneHelper.GetTurkeyTime(),
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
                    ChangeDate = TimeZoneHelper.GetTurkeyTime(),
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
                    ChangeDate = TimeZoneHelper.GetTurkeyTime(),
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
                    ChangeDate = TimeZoneHelper.GetTurkeyTime(),
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
                    ChangeDate = TimeZoneHelper.GetTurkeyTime(),
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
                    ChangeDate = TimeZoneHelper.GetTurkeyTime(),
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
                    ChangeDate = TimeZoneHelper.GetTurkeyTime(),
                    ChangeType = "BIOS Version",
                    OldValue = previous.BiosVersion ?? "",
                    NewValue = current.BiosVersion ?? "",
                    ChangedBy = "Agent"
                });
            }

            // Compare GPUs - detect added/removed graphics cards
            CompareGpuChanges(current.Gpus, previous.Gpus, changes);

            // Compare Network Adapters
            CompareNetworkAdapters(current.NetworkAdapters, previous.NetworkAdapters, changes);

            // Compare RAM Modules
            CompareRamModules(current.RamModules, previous.RamModules, changes);

            // Compare Disks
            CompareDiskChanges(current.Disks, previous.Disks, changes);
        }

        private void CompareSoftwareInfo(DeviceSoftwareInfoDto? current, DeviceSoftwareInfoDto? previous, List<ChangeLogDto> changes)
        {
            if (current == null && previous == null) return;
            if (current == null || previous == null)
            {
                changes.Add(new ChangeLogDto
                {
                    Id = Guid.NewGuid(),
                    ChangeDate = TimeZoneHelper.GetTurkeyTime(),
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
                    ChangeDate = TimeZoneHelper.GetTurkeyTime(),
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
                    ChangeDate = TimeZoneHelper.GetTurkeyTime(),
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
                    ChangeDate = TimeZoneHelper.GetTurkeyTime(),
                    ChangeType = "Active User",
                    OldValue = previous.ActiveUser ?? "",
                    NewValue = current.ActiveUser ?? "",
                    ChangedBy = "Agent"
                });
            }

            // Compare installed applications (detailed comparison)
            CompareApplicationLists(current.InstalledApps, previous.InstalledApps, changes, "Application");
            
            // Compare software updates
            CompareApplicationLists(current.Updates, previous.Updates, changes, "Software Update");
            
            // Compare user lists
            CompareApplicationLists(current.Users, previous.Users, changes, "User");
        }

        private void CompareApplicationLists(List<string>? currentList, List<string>? previousList, List<ChangeLogDto> changes, string changeTypePrefix)
        {
            if (currentList == null) currentList = new List<string>();
            if (previousList == null) previousList = new List<string>();

            // Find newly installed/added items
            var newItems = currentList.Except(previousList).ToList();
            foreach (var newItem in newItems)
            {
                changes.Add(new ChangeLogDto
                {
                    Id = Guid.NewGuid(),
                    ChangeDate = TimeZoneHelper.GetTurkeyTime(),
                    ChangeType = $"{changeTypePrefix} Installed",
                    OldValue = "",
                    NewValue = newItem,
                    ChangedBy = "Agent"
                });
            }

            // Find removed/uninstalled items
            var removedItems = previousList.Except(currentList).ToList();
            foreach (var removedItem in removedItems)
            {
                changes.Add(new ChangeLogDto
                {
                    Id = Guid.NewGuid(),
                    ChangeDate = TimeZoneHelper.GetTurkeyTime(),
                    ChangeType = $"{changeTypePrefix} Uninstalled",
                    OldValue = removedItem,
                    NewValue = "",
                    ChangedBy = "Agent"
                });
            }

            // Also track count changes for summary
            var currentCount = currentList.Count;
            var previousCount = previousList.Count;
            
            if (currentCount != previousCount)
            {
                changes.Add(new ChangeLogDto
                {
                    Id = Guid.NewGuid(),
                    ChangeDate = TimeZoneHelper.GetTurkeyTime(),
                    ChangeType = $"{changeTypePrefix} Count",
                    OldValue = previousCount.ToString(),
                    NewValue = currentCount.ToString(),
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

        private void CompareGpuChanges(List<GpuInfoDto>? current, List<GpuInfoDto>? previous, List<ChangeLogDto> changes)
        {
            if (current == null) current = new List<GpuInfoDto>();
            if (previous == null) previous = new List<GpuInfoDto>();

            // Check for removed GPUs
            for (int i = 0; i < previous.Count; i++)
            {
                var oldGpu = previous[i];
                var found = current.Any(g => g.Name == oldGpu.Name);
                if (!found)
                {
                    changes.Add(new ChangeLogDto
                    {
                        Id = Guid.NewGuid(),
                        ChangeDate = TimeZoneHelper.GetTurkeyTime(),
                        ChangeType = "Hardware Change",
                        OldValue = $"GPU {i}: {oldGpu.Name}",
                        NewValue = "Removed",
                        ChangedBy = "Agent"
                    });
                    Console.WriteLine($"GPU removed: {oldGpu.Name}");
                }
            }

            // Check for added GPUs
            for (int i = 0; i < current.Count; i++)
            {
                var newGpu = current[i];
                var found = previous.Any(g => g.Name == newGpu.Name);
                if (!found)
                {
                    changes.Add(new ChangeLogDto
                    {
                        Id = Guid.NewGuid(),
                        ChangeDate = TimeZoneHelper.GetTurkeyTime(),
                        ChangeType = "Hardware Change",
                        OldValue = "None",
                        NewValue = $"GPU {i}: {newGpu.Name}",
                        ChangedBy = "Agent"
                    });
                    Console.WriteLine($"GPU added: {newGpu.Name}");
                }
            }

            // Check for GPU memory changes
            for (int i = 0; i < Math.Min(current.Count, previous.Count); i++)
            {
                var currentGpu = current[i];
                var previousGpu = previous.FirstOrDefault(g => g.Name == currentGpu.Name);
                
                if (previousGpu != null && 
                    Math.Abs((currentGpu.MemoryGB ?? 0) - (previousGpu.MemoryGB ?? 0)) > 0.1)
                {
                    changes.Add(new ChangeLogDto
                    {
                        Id = Guid.NewGuid(),
                        ChangeDate = TimeZoneHelper.GetTurkeyTime(),
                        ChangeType = "GPU Memory",
                        OldValue = $"{currentGpu.Name}: {previousGpu.MemoryGB?.ToString("F1") ?? "Unknown"} GB",
                        NewValue = $"{currentGpu.Name}: {currentGpu.MemoryGB?.ToString("F1") ?? "Unknown"} GB",
                        ChangedBy = "Agent"
                    });
                }
            }
        }

        private void CompareNetworkAdapters(List<NetworkAdapterDto>? current, List<NetworkAdapterDto>? previous, List<ChangeLogDto> changes)
        {
            if (current == null) current = new List<NetworkAdapterDto>();
            if (previous == null) previous = new List<NetworkAdapterDto>();

            // Check for removed network adapters
            foreach (var oldAdapter in previous)
            {
                var found = current.Any(a => a.MacAddress == oldAdapter.MacAddress);
                if (!found)
                {
                    changes.Add(new ChangeLogDto
                    {
                        Id = Guid.NewGuid(),
                        ChangeDate = TimeZoneHelper.GetTurkeyTime(),
                        ChangeType = "Network Adapter",
                        OldValue = $"{oldAdapter.Description} ({oldAdapter.MacAddress})",
                        NewValue = "Removed",
                        ChangedBy = "Agent"
                    });
                }
            }

            // Check for added network adapters
            foreach (var newAdapter in current)
            {
                var found = previous.Any(a => a.MacAddress == newAdapter.MacAddress);
                if (!found)
                {
                    changes.Add(new ChangeLogDto
                    {
                        Id = Guid.NewGuid(),
                        ChangeDate = TimeZoneHelper.GetTurkeyTime(),
                        ChangeType = "Network Adapter",
                        OldValue = "None",
                        NewValue = $"{newAdapter.Description} ({newAdapter.MacAddress})",
                        ChangedBy = "Agent"
                    });
                }
            }
        }

        private void CompareRamModules(List<RamModuleDto>? current, List<RamModuleDto>? previous, List<ChangeLogDto> changes)
        {
            if (current == null) current = new List<RamModuleDto>();
            if (previous == null) previous = new List<RamModuleDto>();

            // Check for removed RAM modules
            foreach (var oldRam in previous)
            {
                var found = current.Any(r => r.Slot == oldRam.Slot && r.SerialNumber == oldRam.SerialNumber);
                if (!found)
                {
                    changes.Add(new ChangeLogDto
                    {
                        Id = Guid.NewGuid(),
                        ChangeDate = TimeZoneHelper.GetTurkeyTime(),
                        ChangeType = "RAM Module",
                        OldValue = $"{oldRam.Slot}: {oldRam.CapacityGB}GB {oldRam.Manufacturer}",
                        NewValue = "Removed",
                        ChangedBy = "Agent"
                    });
                }
            }

            // Check for added RAM modules
            foreach (var newRam in current)
            {
                var found = previous.Any(r => r.Slot == newRam.Slot && r.SerialNumber == newRam.SerialNumber);
                if (!found)
                {
                    changes.Add(new ChangeLogDto
                    {
                        Id = Guid.NewGuid(),
                        ChangeDate = TimeZoneHelper.GetTurkeyTime(),
                        ChangeType = "RAM Module",
                        OldValue = "None",
                        NewValue = $"{newRam.Slot}: {newRam.CapacityGB}GB {newRam.Manufacturer}",
                        ChangedBy = "Agent"
                    });
                }
            }
        }

        private void CompareDiskChanges(List<DiskInfoDto>? current, List<DiskInfoDto>? previous, List<ChangeLogDto> changes)
        {
            if (current == null) current = new List<DiskInfoDto>();
            if (previous == null) previous = new List<DiskInfoDto>();

            // Check for removed disks
            foreach (var oldDisk in previous)
            {
                var found = current.Any(d => d.DeviceId == oldDisk.DeviceId);
                if (!found)
                {
                    changes.Add(new ChangeLogDto
                    {
                        Id = Guid.NewGuid(),
                        ChangeDate = TimeZoneHelper.GetTurkeyTime(),
                        ChangeType = "Storage Device",
                        OldValue = $"{oldDisk.DeviceId}: {oldDisk.TotalGB}GB",
                        NewValue = "Removed",
                        ChangedBy = "Agent"
                    });
                }
            }

            // Check for added disks
            foreach (var newDisk in current)
            {
                var found = previous.Any(d => d.DeviceId == newDisk.DeviceId);
                if (!found)
                {
                    changes.Add(new ChangeLogDto
                    {
                        Id = Guid.NewGuid(),
                        ChangeDate = TimeZoneHelper.GetTurkeyTime(),
                        ChangeType = "Storage Device",
                        OldValue = "None",
                        NewValue = $"{newDisk.DeviceId}: {newDisk.TotalGB}GB",
                        ChangedBy = "Agent"
                    });
                }
            }

            // Check for disk capacity changes (significant changes only)
            foreach (var currentDisk in current)
            {
                var previousDisk = previous.FirstOrDefault(d => d.DeviceId == currentDisk.DeviceId);
                if (previousDisk != null)
                {
                    var capacityDiff = Math.Abs(currentDisk.TotalGB - previousDisk.TotalGB);
                    if (capacityDiff > 1.0) // Only log significant capacity changes (>1GB)
                    {
                        changes.Add(new ChangeLogDto
                        {
                            Id = Guid.NewGuid(),
                            ChangeDate = TimeZoneHelper.GetTurkeyTime(),
                            ChangeType = "Storage Capacity",
                            OldValue = $"{currentDisk.DeviceId}: {previousDisk.TotalGB}GB",
                            NewValue = $"{currentDisk.DeviceId}: {currentDisk.TotalGB}GB",
                            ChangedBy = "Agent"
                        });
                    }
                }
            }
        }
    }
}