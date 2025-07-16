using System;
using System.Management;
using System.Threading.Tasks;
using Inventory.Agent.Windows.Models;
using System.Collections.Generic;
using LibreHardwareMonitor.Hardware;
using System.Runtime.InteropServices;

namespace Inventory.Agent.Windows
{
    class Program
    {
        static async Task Main(string[] args)
        {
            try
            {
                // Çapraz platform sistem bilgilerini topla
                var device = CrossPlatformSystemInfo.GatherSystemInformation();

                // --- Cihazı logla ---
                DeviceLogger.LogDevice(device);

                // --- API'ye gönder ---
                string apiUrl = "https://localhost:7296/api/device";
                bool success = await ApiClient.PostDeviceAsync(device, apiUrl);
                Console.WriteLine(success ? "Cihaz başarıyla API'ye gönderildi!" : "Gönderim başarısız.");
            }
            catch (PlatformNotSupportedException ex)
            {
                Console.WriteLine($"Platform desteklenmiyor: {ex.Message}");
                Console.WriteLine("Şu anda Windows ve Linux desteklenmektedir.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Bir hata oluştu: {ex.Message}");
            }

            Console.WriteLine("\nİşlem tamamlandı. Çıkmak için bir tuşa basın.");
            Console.ReadKey();
        }
    }
}