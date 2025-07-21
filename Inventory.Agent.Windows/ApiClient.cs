using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Inventory.Agent.Windows.Models;
using System;

namespace Inventory.Agent.Windows
{
    public static class ApiClient
    {
        public static async Task<bool> PostDeviceAsync(DeviceDto device, string apiUrl)
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
                    }
                    else
                    {
                        Console.WriteLine("Device data sent successfully!");
                    }
                    
                    return response.IsSuccessStatusCode;
                }
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"HTTP Error: {ex.Message}");
                return false;
            }
            catch (TaskCanceledException ex)
            {
                Console.WriteLine($"Request Timeout: {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected Error: {ex.Message}");
                return false;
            }
        }
    }
}