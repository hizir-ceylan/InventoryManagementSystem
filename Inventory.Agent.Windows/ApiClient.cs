using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Inventory.Agent.Windows.Models;

namespace Inventory.Agent.Windows
{
    public static class ApiClient
    {
        public static async Task<bool> PostDeviceAsync(DeviceDto device, string apiUrl)
        {
            using (var client = new HttpClient())
            {
                var json = JsonConvert.SerializeObject(device);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await client.PostAsync(apiUrl, content);
                var responseString = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Status: {response.StatusCode}, Response: {responseString}");
                return response.IsSuccessStatusCode;
            }
        }
    }
}