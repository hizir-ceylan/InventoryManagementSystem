using System;

namespace Inventory.Agent.Windows.Configuration
{
    public class ApiSettings
    {
        public string BaseUrl { get; set; } = "http://localhost:5000";
        public int Timeout { get; set; } = 30;
        public int RetryCount { get; set; } = 3;
        
        public static ApiSettings LoadFromEnvironment()
        {
            var settings = new ApiSettings();
            
            // Load from environment variables with fallback to defaults
            var baseUrl = Environment.GetEnvironmentVariable("ApiSettings__BaseUrl");
            if (!string.IsNullOrEmpty(baseUrl))
            {
                settings.BaseUrl = baseUrl;
            }
            
            var timeout = Environment.GetEnvironmentVariable("ApiSettings__Timeout");
            if (!string.IsNullOrEmpty(timeout) && int.TryParse(timeout, out int timeoutValue))
            {
                settings.Timeout = timeoutValue;
            }
            
            var retryCount = Environment.GetEnvironmentVariable("ApiSettings__RetryCount");
            if (!string.IsNullOrEmpty(retryCount) && int.TryParse(retryCount, out int retryValue))
            {
                settings.RetryCount = retryValue;
            }
            
            return settings;
        }
        
        public string GetDeviceEndpoint()
        {
            return $"{BaseUrl.TrimEnd('/')}/api/device";
        }
        
        public string GetNetworkDiscoveryEndpoint()
        {
            return $"{BaseUrl.TrimEnd('/')}/api/devices/network-discovered";
        }
    }
}