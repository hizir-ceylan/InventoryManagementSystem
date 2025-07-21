using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace Inventory.Api.Helpers
{
    public static class NetworkRangeDetector
    {
        /// <summary>
        /// Automatically detects the local network ranges based on the device's network interfaces
        /// </summary>
        /// <returns>List of network ranges in CIDR notation (e.g., "192.168.1.0/24")</returns>
        public static List<string> GetLocalNetworkRanges()
        {
            var networkRanges = new List<string>();

            try
            {
                var networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();

                foreach (var networkInterface in networkInterfaces)
                {
                    // Skip loopback and non-operational interfaces
                    if (networkInterface.NetworkInterfaceType == NetworkInterfaceType.Loopback ||
                        networkInterface.OperationalStatus != OperationalStatus.Up)
                    {
                        continue;
                    }

                    var ipProperties = networkInterface.GetIPProperties();

                    foreach (var unicastAddress in ipProperties.UnicastAddresses)
                    {
                        // Only process IPv4 addresses
                        if (unicastAddress.Address.AddressFamily == AddressFamily.InterNetwork)
                        {
                            var networkRange = CalculateNetworkRange(unicastAddress.Address, unicastAddress.IPv4Mask);
                            if (!string.IsNullOrEmpty(networkRange) && !networkRanges.Contains(networkRange))
                            {
                                networkRanges.Add(networkRange);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Log error but return empty list
                Console.WriteLine($"Error detecting network ranges: {ex.Message}");
            }

            // If no ranges detected, add common private ranges as fallback
            if (!networkRanges.Any())
            {
                networkRanges.AddRange(new[]
                {
                    "192.168.1.0/24",
                    "192.168.0.0/24",
                    "10.0.0.0/24"
                });
            }

            return networkRanges;
        }

        /// <summary>
        /// Gets the primary local network range (the first non-loopback interface)
        /// </summary>
        /// <returns>Primary network range in CIDR notation</returns>
        public static string GetPrimaryNetworkRange()
        {
            var ranges = GetLocalNetworkRanges();
            return ranges.FirstOrDefault() ?? "192.168.1.0/24";
        }

        /// <summary>
        /// Calculates the network range in CIDR notation from IP address and subnet mask
        /// </summary>
        /// <param name="ipAddress">The IP address</param>
        /// <param name="subnetMask">The subnet mask</param>
        /// <returns>Network range in CIDR notation (e.g., "192.168.1.0/24")</returns>
        private static string CalculateNetworkRange(IPAddress ipAddress, IPAddress subnetMask)
        {
            try
            {
                var ipBytes = ipAddress.GetAddressBytes();
                var maskBytes = subnetMask.GetAddressBytes();

                // Calculate network address
                var networkBytes = new byte[4];
                for (int i = 0; i < 4; i++)
                {
                    networkBytes[i] = (byte)(ipBytes[i] & maskBytes[i]);
                }

                var networkAddress = new IPAddress(networkBytes);

                // Calculate CIDR prefix length
                var prefixLength = CalculatePrefixLength(subnetMask);

                return $"{networkAddress}/{prefixLength}";
            }
            catch
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Calculates the CIDR prefix length from subnet mask
        /// </summary>
        /// <param name="subnetMask">The subnet mask</param>
        /// <returns>CIDR prefix length</returns>
        private static int CalculatePrefixLength(IPAddress subnetMask)
        {
            var maskBytes = subnetMask.GetAddressBytes();
            var prefixLength = 0;

            foreach (var maskByte in maskBytes)
            {
                for (int i = 7; i >= 0; i--)
                {
                    if ((maskByte & (1 << i)) != 0)
                    {
                        prefixLength++;
                    }
                    else
                    {
                        return prefixLength;
                    }
                }
            }

            return prefixLength;
        }

        /// <summary>
        /// Gets the local IP address of the primary network interface
        /// </summary>
        /// <returns>Local IP address</returns>
        public static string GetLocalIPAddress()
        {
            try
            {
                var networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();

                foreach (var networkInterface in networkInterfaces)
                {
                    if (networkInterface.NetworkInterfaceType == NetworkInterfaceType.Loopback ||
                        networkInterface.OperationalStatus != OperationalStatus.Up)
                    {
                        continue;
                    }

                    var ipProperties = networkInterface.GetIPProperties();
                    var unicastAddress = ipProperties.UnicastAddresses
                        .FirstOrDefault(ua => ua.Address.AddressFamily == AddressFamily.InterNetwork);

                    if (unicastAddress != null)
                    {
                        return unicastAddress.Address.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting local IP address: {ex.Message}");
            }

            return "127.0.0.1";
        }
    }
}