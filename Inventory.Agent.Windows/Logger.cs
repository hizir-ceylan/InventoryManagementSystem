using System;
using System.IO;
using Newtonsoft.Json;

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
        string diff = "No change detected";
        if (File.Exists(yesterdayPath))
        {
            var yesterdayJson = File.ReadAllText(yesterdayPath);
            dynamic yesterdayObj = JsonConvert.DeserializeObject(yesterdayJson);
            string yesterdayDeviceJson = JsonConvert.SerializeObject(yesterdayObj.Device);
            string todayDeviceJson = JsonConvert.SerializeObject(deviceSnapshot);

            if (yesterdayDeviceJson != todayDeviceJson)
            {
                diff = GetSimpleDiff(yesterdayDeviceJson, todayDeviceJson);
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

    // Basit diff fonksiyonu (daha gelişmişi için bir diff kütüphanesi kullanılabilir)
    private static string GetSimpleDiff(string oldJson, string newJson)
    {
        if (oldJson == newJson)
            return "No change detected";
        // Fark varsa kısa bilgi dön (isteğe göre daha gelişmiş yazılabilir)
        return "Change detected. Device snapshot has changed.";
    }
}