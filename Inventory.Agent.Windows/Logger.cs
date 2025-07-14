using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Inventory.Agent.Windows.Models;

public static class DeviceLogger
{
    private static string LogFolder =>
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "LocalLogs");

    public static void LogDevice(object deviceSnapshot)
    {
        Directory.CreateDirectory(LogFolder);

        // 1. Dosya adları
        string today = DateTime.Now.ToString("yyyy-MM-dd");
        string yesterday = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");
        string todayPath = Path.Combine(LogFolder, $"device-log-{today}.json");
        string yesterdayPath = Path.Combine(LogFolder, $"device-log-{yesterday}.json");

        // 2. Eski log dosyalarını sil (yalnızca son 2 günün dosyasını tut)
        foreach (var file in Directory.GetFiles(LogFolder, "device-log-*.json"))
        {
            var fileName = Path.GetFileNameWithoutExtension(file);
            if (!fileName.EndsWith(today) && !fileName.EndsWith(yesterday))
            {
                try { File.Delete(file); } catch { }
            }
        }

        // 3. Dünkü logu oku ve karşılaştır
        object diff = "No change detected";
        if (File.Exists(yesterdayPath))
        {
            var yesterdayJson = File.ReadAllText(yesterdayPath);
            dynamic yesterdayObj = JsonConvert.DeserializeObject(yesterdayJson);
            string yesterdayDeviceJson = JsonConvert.SerializeObject(yesterdayObj.Device);
            string todayDeviceJson = JsonConvert.SerializeObject(deviceSnapshot);

            if (yesterdayDeviceJson != todayDeviceJson)
            {
                diff = GetDetailedDiff(yesterdayDeviceJson, todayDeviceJson);
            }
        }

        // 4. Log verisi oluştur
        var logObject = new
        {
            Date = DateTime.Now,
            Device = deviceSnapshot,
            Diff = diff
        };

        // 5. Dosyaya yaz (üzerine yazar, her gün tek dosya)
        File.WriteAllText(todayPath, JsonConvert.SerializeObject(logObject, Formatting.Indented));
    }

    // Detaylı diff fonksiyonu - tüm önemli alanları karşılaştırır
    private static object GetDetailedDiff(string oldJson, string newJson)
    {
        if (oldJson == newJson)
            return "No change detected";

        try
        {
            var oldDevice = JsonConvert.DeserializeObject<DeviceDto>(oldJson);
            var newDevice = JsonConvert.DeserializeObject<DeviceDto>(newJson);

            var detailedDiff = new DetailedDiff();

            // Hardware bilgileri karşılaştırması
            CompareHardwareInfo(oldDevice?.HardwareInfo, newDevice?.HardwareInfo, detailedDiff);
            
            // Software bilgileri karşılaştırması  
            CompareSoftwareInfo(oldDevice?.SoftwareInfo, newDevice?.SoftwareInfo, detailedDiff);
            
            // Temel cihaz bilgileri karşılaştırması
            CompareBasicDeviceInfo(oldDevice, newDevice, detailedDiff);

            // Eğer hiç değişiklik yoksa basit mesaj dön
            if (detailedDiff.Diff.Count == 0)
                return "No change detected";

            return detailedDiff;
        }
        catch (Exception ex)
        {
            // Hata durumunda basit diff'e geri dön
            return $"Change detected. Device snapshot has changed. (Error parsing diff: {ex.Message})";
        }
    }

    private static void CompareHardwareInfo(DeviceHardwareInfoDto oldHw, DeviceHardwareInfoDto newHw, DetailedDiff diff)
    {
        if (oldHw == null && newHw == null) return;
        if (oldHw == null || newHw == null)
        {
            diff.Diff["HardwareInfo"] = new FieldDiff();
            diff.Diff["HardwareInfo"].ChangedValues.Add(new ChangedValue 
            { 
                Field = "HardwareInfo", 
                OldValue = oldHw == null ? "null" : "present", 
                NewValue = newHw == null ? "null" : "present" 
            });
            return;
        }

        // GPU karşılaştırması
        CompareGpus(oldHw.Gpus, newHw.Gpus, diff);

        // RAM modülleri karşılaştırması
        CompareRamModules(oldHw.RamModules, newHw.RamModules, diff);

        // Disk karşılaştırması
        CompareDisks(oldHw.Disks, newHw.Disks, diff);

        // Network adaptörü karşılaştırması
        CompareNetworkAdapters(oldHw.NetworkAdapters, newHw.NetworkAdapters, diff);

        // Basit hardware alanları karşılaştırması
        CompareSimpleFields("HardwareInfo", new Dictionary<string, (object old, object @new)>
        {
            { "Cpu", (oldHw.Cpu, newHw.Cpu) },
            { "CpuCores", (oldHw.CpuCores, newHw.CpuCores) },
            { "CpuLogical", (oldHw.CpuLogical, newHw.CpuLogical) },
            { "CpuClockMHz", (oldHw.CpuClockMHz, newHw.CpuClockMHz) },
            { "Motherboard", (oldHw.Motherboard, newHw.Motherboard) },
            { "MotherboardSerial", (oldHw.MotherboardSerial, newHw.MotherboardSerial) },
            { "BiosManufacturer", (oldHw.BiosManufacturer, newHw.BiosManufacturer) },
            { "BiosVersion", (oldHw.BiosVersion, newHw.BiosVersion) },
            { "BiosSerial", (oldHw.BiosSerial, newHw.BiosSerial) },
            { "RamGB", (oldHw.RamGB, newHw.RamGB) },
            { "DiskGB", (oldHw.DiskGB, newHw.DiskGB) }
        }, diff);
    }

    private static void CompareSoftwareInfo(DeviceSoftwareInfoDto oldSw, DeviceSoftwareInfoDto newSw, DetailedDiff diff)
    {
        if (oldSw == null && newSw == null) return;
        if (oldSw == null || newSw == null)
        {
            diff.Diff["SoftwareInfo"] = new FieldDiff();
            diff.Diff["SoftwareInfo"].ChangedValues.Add(new ChangedValue 
            { 
                Field = "SoftwareInfo", 
                OldValue = oldSw == null ? "null" : "present", 
                NewValue = newSw == null ? "null" : "present" 
            });
            return;
        }

        // Yüklü uygulamalar karşılaştırması
        CompareStringLists("SoftwareInfo.InstalledApps", oldSw.InstalledApps, newSw.InstalledApps, diff);

        // Kullanıcılar karşılaştırması
        CompareStringLists("SoftwareInfo.Users", oldSw.Users, newSw.Users, diff);

        // Güncellemeler karşılaştırması
        CompareStringLists("SoftwareInfo.Updates", oldSw.Updates, newSw.Updates, diff);

        // Basit software alanları karşılaştırması
        CompareSimpleFields("SoftwareInfo", new Dictionary<string, (object old, object @new)>
        {
            { "OperatingSystem", (oldSw.OperatingSystem, newSw.OperatingSystem) },
            { "OsVersion", (oldSw.OsVersion, newSw.OsVersion) },
            { "OsArchitecture", (oldSw.OsArchitecture, newSw.OsArchitecture) },
            { "RegisteredUser", (oldSw.RegisteredUser, newSw.RegisteredUser) },
            { "SerialNumber", (oldSw.SerialNumber, newSw.SerialNumber) },
            { "ActiveUser", (oldSw.ActiveUser, newSw.ActiveUser) }
        }, diff);
    }

    private static void CompareBasicDeviceInfo(DeviceDto oldDevice, DeviceDto newDevice, DetailedDiff diff)
    {
        if (oldDevice == null || newDevice == null) return;

        CompareSimpleFields("Device", new Dictionary<string, (object old, object @new)>
        {
            { "Name", (oldDevice.Name, newDevice.Name) },
            { "MacAddress", (oldDevice.MacAddress, newDevice.MacAddress) },
            { "IpAddress", (oldDevice.IpAddress, newDevice.IpAddress) },
            { "DeviceType", (oldDevice.DeviceType, newDevice.DeviceType) },
            { "Model", (oldDevice.Model, newDevice.Model) },
            { "Location", (oldDevice.Location, newDevice.Location) },
            { "Status", (oldDevice.Status, newDevice.Status) }
        }, diff);
    }

    private static void CompareGpus(List<GpuInfoDto> oldGpus, List<GpuInfoDto> newGpus, DetailedDiff diff)
    {
        var fieldName = "HardwareInfo.Gpus";
        oldGpus = oldGpus ?? new List<GpuInfoDto>();
        newGpus = newGpus ?? new List<GpuInfoDto>();

        var oldGpuStrings = oldGpus.Select(g => $"{g.Name} ({g.MemoryGB:F1} GB)").ToList();
        var newGpuStrings = newGpus.Select(g => $"{g.Name} ({g.MemoryGB:F1} GB)").ToList();

        CompareStringLists(fieldName, oldGpuStrings, newGpuStrings, diff);
    }

    private static void CompareRamModules(List<RamModuleDto> oldRam, List<RamModuleDto> newRam, DetailedDiff diff)
    {
        var fieldName = "HardwareInfo.RamModules";
        oldRam = oldRam ?? new List<RamModuleDto>();
        newRam = newRam ?? new List<RamModuleDto>();

        // RAM modüllerini karşılaştır - slot bazında
        var fieldDiff = new FieldDiff();
        
        var oldSlots = oldRam.GroupBy(r => r.Slot).ToDictionary(g => g.Key, g => g.First());
        var newSlots = newRam.GroupBy(r => r.Slot).ToDictionary(g => g.Key, g => g.First());

        // Kaldırılan slotlar
        foreach (var slot in oldSlots.Keys.Except(newSlots.Keys))
        {
            var module = oldSlots[slot];
            fieldDiff.Removed.Add($"Slot {slot}: {module.CapacityGB}GB {module.Manufacturer} {module.PartNumber}");
        }

        // Eklenen slotlar
        foreach (var slot in newSlots.Keys.Except(oldSlots.Keys))
        {
            var module = newSlots[slot];
            fieldDiff.Added.Add($"Slot {slot}: {module.CapacityGB}GB {module.Manufacturer} {module.PartNumber}");
        }

        // Değişen slotlar
        foreach (var slot in oldSlots.Keys.Intersect(newSlots.Keys))
        {
            var oldModule = oldSlots[slot];
            var newModule = newSlots[slot];
            
            if (!AreRamModulesEqual(oldModule, newModule))
            {
                var changes = new ChangedObjectValue
                {
                    Identifier = $"Slot {slot}",
                    Changes = new List<ChangedValue>()
                };

                if (oldModule.CapacityGB != newModule.CapacityGB)
                    changes.Changes.Add(new ChangedValue { Field = "CapacityGB", OldValue = oldModule.CapacityGB, NewValue = newModule.CapacityGB });
                if (oldModule.SpeedMHz != newModule.SpeedMHz)
                    changes.Changes.Add(new ChangedValue { Field = "SpeedMHz", OldValue = oldModule.SpeedMHz, NewValue = newModule.SpeedMHz });
                if (oldModule.Manufacturer != newModule.Manufacturer)
                    changes.Changes.Add(new ChangedValue { Field = "Manufacturer", OldValue = oldModule.Manufacturer, NewValue = newModule.Manufacturer });
                if (oldModule.PartNumber != newModule.PartNumber)
                    changes.Changes.Add(new ChangedValue { Field = "PartNumber", OldValue = oldModule.PartNumber, NewValue = newModule.PartNumber });

                if (changes.Changes.Count > 0)
                    fieldDiff.ChangedValues.Add(changes);
            }
        }

        if (fieldDiff.Removed.Count > 0 || fieldDiff.Added.Count > 0 || fieldDiff.ChangedValues.Count > 0)
        {
            diff.Diff[fieldName] = fieldDiff;
        }
    }

    private static void CompareDisks(List<DiskInfoDto> oldDisks, List<DiskInfoDto> newDisks, DetailedDiff diff)
    {
        var fieldName = "HardwareInfo.Disks";
        oldDisks = oldDisks ?? new List<DiskInfoDto>();
        newDisks = newDisks ?? new List<DiskInfoDto>();

        var fieldDiff = new FieldDiff();
        
        var oldDiskMap = oldDisks.ToDictionary(d => d.DeviceId, d => d);
        var newDiskMap = newDisks.ToDictionary(d => d.DeviceId, d => d);

        // Kaldırılan diskler
        foreach (var deviceId in oldDiskMap.Keys.Except(newDiskMap.Keys))
        {
            var disk = oldDiskMap[deviceId];
            fieldDiff.Removed.Add($"{deviceId} ({disk.TotalGB:F2} GB Total)");
        }

        // Eklenen diskler
        foreach (var deviceId in newDiskMap.Keys.Except(oldDiskMap.Keys))
        {
            var disk = newDiskMap[deviceId];
            fieldDiff.Added.Add($"{deviceId} ({disk.TotalGB:F2} GB Total)");
        }

        // Değişen diskler
        foreach (var deviceId in oldDiskMap.Keys.Intersect(newDiskMap.Keys))
        {
            var oldDisk = oldDiskMap[deviceId];
            var newDisk = newDiskMap[deviceId];
            
            if (Math.Abs(oldDisk.FreeGB - newDisk.FreeGB) > 0.01 || Math.Abs(oldDisk.TotalGB - newDisk.TotalGB) > 0.01)
            {
                var changes = new ChangedObjectValue
                {
                    Identifier = deviceId,
                    Changes = new List<ChangedValue>()
                };

                if (Math.Abs(oldDisk.TotalGB - newDisk.TotalGB) > 0.01)
                    changes.Changes.Add(new ChangedValue { Field = "TotalGB", OldValue = Math.Round(oldDisk.TotalGB, 2), NewValue = Math.Round(newDisk.TotalGB, 2) });
                if (Math.Abs(oldDisk.FreeGB - newDisk.FreeGB) > 0.01)
                    changes.Changes.Add(new ChangedValue { Field = "FreeGB", OldValue = Math.Round(oldDisk.FreeGB, 2), NewValue = Math.Round(newDisk.FreeGB, 2) });

                fieldDiff.ChangedValues.Add(changes);
            }
        }

        if (fieldDiff.Removed.Count > 0 || fieldDiff.Added.Count > 0 || fieldDiff.ChangedValues.Count > 0)
        {
            diff.Diff[fieldName] = fieldDiff;
        }
    }

    private static void CompareNetworkAdapters(List<NetworkAdapterDto> oldAdapters, List<NetworkAdapterDto> newAdapters, DetailedDiff diff)
    {
        var fieldName = "HardwareInfo.NetworkAdapters";
        oldAdapters = oldAdapters ?? new List<NetworkAdapterDto>();
        newAdapters = newAdapters ?? new List<NetworkAdapterDto>();

        var fieldDiff = new FieldDiff();
        
        var oldAdapterMap = oldAdapters.ToDictionary(a => a.MacAddress ?? a.Description, a => a);
        var newAdapterMap = newAdapters.ToDictionary(a => a.MacAddress ?? a.Description, a => a);

        // Kaldırılan adaptörler
        foreach (var key in oldAdapterMap.Keys.Except(newAdapterMap.Keys))
        {
            var adapter = oldAdapterMap[key];
            fieldDiff.Removed.Add($"{adapter.Description} (MAC: {adapter.MacAddress})");
        }

        // Eklenen adaptörler
        foreach (var key in newAdapterMap.Keys.Except(oldAdapterMap.Keys))
        {
            var adapter = newAdapterMap[key];
            fieldDiff.Added.Add($"{adapter.Description} (MAC: {adapter.MacAddress})");
        }

        // Değişen adaptörler (IP değişikliği)
        foreach (var key in oldAdapterMap.Keys.Intersect(newAdapterMap.Keys))
        {
            var oldAdapter = oldAdapterMap[key];
            var newAdapter = newAdapterMap[key];
            
            if (oldAdapter.IpAddress != newAdapter.IpAddress)
            {
                var changes = new ChangedObjectValue
                {
                    Identifier = oldAdapter.Description,
                    Changes = new List<ChangedValue>
                    {
                        new ChangedValue { Field = "IpAddress", OldValue = oldAdapter.IpAddress, NewValue = newAdapter.IpAddress }
                    }
                };
                fieldDiff.ChangedValues.Add(changes);
            }
        }

        if (fieldDiff.Removed.Count > 0 || fieldDiff.Added.Count > 0 || fieldDiff.ChangedValues.Count > 0)
        {
            diff.Diff[fieldName] = fieldDiff;
        }
    }

    private static void CompareStringLists(string fieldName, List<string> oldList, List<string> newList, DetailedDiff diff)
    {
        oldList = oldList ?? new List<string>();
        newList = newList ?? new List<string>();

        var removed = oldList.Except(newList).ToList();
        var added = newList.Except(oldList).ToList();

        if (removed.Count > 0 || added.Count > 0)
        {
            diff.Diff[fieldName] = new FieldDiff
            {
                Removed = removed,
                Added = added
            };
        }
    }

    private static void CompareSimpleFields(string prefix, Dictionary<string, (object old, object @new)> fields, DetailedDiff diff)
    {
        foreach (var kvp in fields)
        {
            var fieldName = $"{prefix}.{kvp.Key}";
            var (oldValue, newValue) = kvp.Value;
            
            if (!Equals(oldValue, newValue))
            {
                if (!diff.Diff.ContainsKey(fieldName))
                    diff.Diff[fieldName] = new FieldDiff();
                    
                diff.Diff[fieldName].ChangedValues.Add(new ChangedValue
                {
                    Field = kvp.Key,
                    OldValue = oldValue,
                    NewValue = newValue
                });
            }
        }
    }

    private static bool AreRamModulesEqual(RamModuleDto module1, RamModuleDto module2)
    {
        return module1.CapacityGB == module2.CapacityGB &&
               module1.SpeedMHz == module2.SpeedMHz &&
               module1.Manufacturer == module2.Manufacturer &&
               module1.PartNumber == module2.PartNumber &&
               module1.SerialNumber == module2.SerialNumber;
    }
}