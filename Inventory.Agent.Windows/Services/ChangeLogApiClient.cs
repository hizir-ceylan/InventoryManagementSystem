using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Inventory.Agent.Windows.Models;
using Inventory.Agent.Windows.Configuration;

namespace Inventory.Agent.Windows.Services
{
    public class ChangeLogApiClient
    {
        private readonly string _baseUrl;
        private readonly string _deviceIpAddress;
        private readonly string _deviceMacAddress;

        public ChangeLogApiClient(string baseUrl, string deviceIpAddress, string deviceMacAddress)
        {
            _baseUrl = baseUrl;
            _deviceIpAddress = deviceIpAddress;
            _deviceMacAddress = deviceMacAddress;
        }

        public ChangeLogApiClient(ApiSettings apiSettings)
        {
            _baseUrl = apiSettings.BaseUrl;
            _deviceIpAddress = "";
            _deviceMacAddress = "";
        }

        public async Task<bool> SendChangeLogsAsync(List<ChangeLogDto> changeLogs)
        {
            return await SendChangeLogsAsync("", changeLogs);
        }

        public async Task<bool> SendChangeLogsAsync(string deviceMacAddress, List<ChangeLogDto> changeLogs)
        {
            if (changeLogs == null || changeLogs.Count == 0)
            {
                Console.WriteLine("No change logs to send");
                return true;
            }

            try
            {
                using var client = new HttpClient();
                client.Timeout = TimeSpan.FromSeconds(30);

                var batchDto = new
                {
                    DeviceIpAddress = _deviceIpAddress,
                    DeviceMacAddress = !string.IsNullOrEmpty(deviceMacAddress) ? deviceMacAddress : _deviceMacAddress,
                    ChangeLogs = changeLogs.Select(c => new
                    {
                        ChangeDate = c.ChangeDate,
                        ChangeType = c.ChangeType,
                        OldValue = c.OldValue,
                        NewValue = c.NewValue,
                        ChangedBy = c.ChangedBy
                    }).ToList()
                };

                var json = JsonConvert.SerializeObject(batchDto);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var changeLogEndpoint = $"{_baseUrl.TrimEnd('/')}/api/changelog/batch";
                Console.WriteLine($"Sending {changeLogs.Count} change logs to: {changeLogEndpoint}");

                var response = await client.PostAsync(changeLogEndpoint, content);
                var responseString = await response.Content.ReadAsStringAsync();

                Console.WriteLine($"Change log response status: {response.StatusCode}");
                
                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Change log error response: {responseString}");
                    return false;
                }
                else
                {
                    Console.WriteLine($"Successfully sent {changeLogs.Count} change logs to API");
                    Console.WriteLine($"Response: {responseString}");
                    return true;
                }
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"HTTP Error sending change logs: {ex.Message}");
                return false;
            }
            catch (TaskCanceledException ex)
            {
                Console.WriteLine($"Request timeout sending change logs: {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error sending change logs: {ex.Message}");
                return false;
            }
        }
    }
}