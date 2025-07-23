using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Inventory.Agent.Windows.Models;
using System;
using Inventory.Agent.Windows.Services;

namespace Inventory.Agent.Windows
{
    public static class ApiClient
    {
        public static async Task<bool> PostDeviceAsync(DeviceDto device, string apiUrl, OfflineStorageService? offlineStorage = null)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    // Set timeout to 30 seconds
                    client.Timeout = TimeSpan.FromSeconds(30);
                    
                    var json = JsonConvert.SerializeObject(device);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");
                    
                    Console.WriteLine($"Sending device data to: {apiUrl}");
                    
                    var response = await client.PostAsync(apiUrl, content);
                    var responseString = await response.Content.ReadAsStringAsync();
                    
                    Console.WriteLine($"Status: {response.StatusCode}");
                    if (!response.IsSuccessStatusCode)
                    {
                        Console.WriteLine($"Error Response: {responseString}");
                        
                        // Store offline if enabled and storage service is provided
                        if (offlineStorage != null)
                        {
                            await offlineStorage.StoreDeviceDataAsync(device);
                            Console.WriteLine("Device data stored offline for later upload.");
                        }
                        
                        return false;
                    }
                    else
                    {
                        Console.WriteLine("Device data sent successfully!");
                        return true;
                    }
                }
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"HTTP Error: {ex.Message}");
                
                // Store offline if enabled and storage service is provided
                if (offlineStorage != null)
                {
                    await offlineStorage.StoreDeviceDataAsync(device);
                    Console.WriteLine("Device data stored offline due to network error.");
                }
                
                return false;
            }
            catch (TaskCanceledException ex)
            {
                Console.WriteLine($"Request Timeout: {ex.Message}");
                
                // Store offline if enabled and storage service is provided
                if (offlineStorage != null)
                {
                    await offlineStorage.StoreDeviceDataAsync(device);
                    Console.WriteLine("Device data stored offline due to timeout.");
                }
                
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected Error: {ex.Message}");
                
                // Store offline if enabled and storage service is provided
                if (offlineStorage != null)
                {
                    await offlineStorage.StoreDeviceDataAsync(device);
                    Console.WriteLine("Device data stored offline due to unexpected error.");
                }
                
                return false;
            }
        }

        public static async Task<bool> PostBatchDevicesAsync(DeviceDto[] devices, string apiUrl)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    // Set timeout to 60 seconds for batch uploads
                    client.Timeout = TimeSpan.FromSeconds(60);
                    
                    var json = JsonConvert.SerializeObject(devices);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");
                    
                    Console.WriteLine($"Sending batch device data to: {apiUrl} (Count: {devices.Length})");
                    
                    var response = await client.PostAsync(apiUrl, content);
                    var responseString = await response.Content.ReadAsStringAsync();
                    
                    Console.WriteLine($"Batch Status: {response.StatusCode}");
                    if (!response.IsSuccessStatusCode)
                    {
                        Console.WriteLine($"Batch Error Response: {responseString}");
                        return false;
                    }
                    else
                    {
                        Console.WriteLine($"Batch device data sent successfully! ({devices.Length} devices)");
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Batch Upload Error: {ex.Message}");
                return false;
            }
        }
    }
}