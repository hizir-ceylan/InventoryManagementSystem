using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Inventory.Domain.Entities;
using Inventory.Agent.Windows.Configuration;
using Microsoft.Extensions.Logging;

namespace Inventory.Agent.Windows.Services
{
    /// <summary>
    /// API client for sending update detection results to the server
    /// </summary>
    public class UpdateApiClient
    {
        private readonly string _baseUrl;
        private readonly ILogger<UpdateApiClient>? _logger;

        public UpdateApiClient(string baseUrl, ILogger<UpdateApiClient>? logger = null)
        {
            _baseUrl = baseUrl.TrimEnd('/');
            _logger = logger;
        }

        public UpdateApiClient(ApiSettings apiSettings, ILogger<UpdateApiClient>? logger = null)
        {
            _baseUrl = apiSettings.BaseUrl.TrimEnd('/');
            _logger = logger;
        }

        /// <summary>
        /// Sends detected updates to the API
        /// </summary>
        /// <param name="updates">List of detected system updates</param>
        /// <returns>True if successfully sent, false otherwise</returns>
        public async Task<bool> SendUpdatesAsync(List<SystemUpdate> updates)
        {
            if (updates == null || updates.Count == 0)
            {
                _logger?.LogInformation("No updates to send to API");
                return true;
            }

            try
            {
                using var client = new HttpClient();
                client.Timeout = TimeSpan.FromSeconds(30);

                var json = JsonSerializer.Serialize(updates, new JsonSerializerOptions 
                { 
                    WriteIndented = false,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                });

                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var updateEndpoint = $"{_baseUrl}/api/update/report";

                _logger?.LogInformation($"Sending {updates.Count} updates to: {updateEndpoint}");

                var response = await client.PostAsync(updateEndpoint, content);
                var responseString = await response.Content.ReadAsStringAsync();

                _logger?.LogInformation($"Update report response status: {response.StatusCode}");
                
                if (!response.IsSuccessStatusCode)
                {
                    _logger?.LogWarning($"Update report error response: {responseString}");
                    return false;
                }
                else
                {
                    _logger?.LogInformation($"Successfully sent {updates.Count} updates to API");
                    _logger?.LogDebug($"Response: {responseString}");
                    return true;
                }
            }
            catch (HttpRequestException ex)
            {
                _logger?.LogWarning($"HTTP Error sending updates: {ex.Message}");
                return false;
            }
            catch (TaskCanceledException ex)
            {
                _logger?.LogWarning($"Request timeout sending updates: {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Unexpected error sending updates");
                return false;
            }
        }
    }
}