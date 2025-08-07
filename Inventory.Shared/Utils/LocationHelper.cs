using System;
using System.Collections.Generic;
using System.Net;

namespace Inventory.Shared.Utils
{
    /// <summary>
    /// Helper class for assigning locations based on IP addresses
    /// </summary>
    public static class LocationHelper
    {
        /// <summary>
        /// Network to location mappings
        /// Key: Network prefix (e.g., "192.168.112")
        /// Value: Location name
        /// </summary>
        private static readonly Dictionary<string, string> NetworkLocationMappings = new Dictionary<string, string>
        {
            { "192.168.112", "Stajyer" }  // Initial mapping for intern network
        };

        /// <summary>
        /// Gets location based on IP address
        /// </summary>
        /// <param name="ipAddress">Device IP address</param>
        /// <param name="fallbackLocation">Fallback location if no mapping found</param>
        /// <returns>Location name</returns>
        public static string GetLocationByIpAddress(string? ipAddress, string fallbackLocation = "Network Discovery")
        {
            if (string.IsNullOrWhiteSpace(ipAddress))
            {
                return fallbackLocation;
            }

            try
            {
                // Try to parse IP address
                if (!IPAddress.TryParse(ipAddress, out IPAddress? ip) || ip == null)
                {
                    return fallbackLocation;
                }

                // Get network prefix (first 3 octets for /24 networks)
                string[] octets = ipAddress.Split('.');
                if (octets.Length >= 3)
                {
                    string networkPrefix = $"{octets[0]}.{octets[1]}.{octets[2]}";
                    
                    if (NetworkLocationMappings.TryGetValue(networkPrefix, out string? location))
                    {
                        return location;
                    }
                }

                return fallbackLocation;
            }
            catch (Exception)
            {
                // If any error occurs, return fallback location
                return fallbackLocation;
            }
        }

        /// <summary>
        /// Gets all configured network location mappings
        /// </summary>
        /// <returns>Dictionary of network to location mappings</returns>
        public static Dictionary<string, string> GetNetworkLocationMappings()
        {
            return new Dictionary<string, string>(NetworkLocationMappings);
        }

        /// <summary>
        /// Adds a new network location mapping
        /// </summary>
        /// <param name="networkPrefix">Network prefix (e.g., "192.168.1")</param>
        /// <param name="location">Location name</param>
        /// <returns>True if added successfully, false if already exists</returns>
        public static bool AddNetworkLocationMapping(string networkPrefix, string location)
        {
            if (string.IsNullOrWhiteSpace(networkPrefix) || string.IsNullOrWhiteSpace(location))
            {
                return false;
            }

            if (NetworkLocationMappings.ContainsKey(networkPrefix))
            {
                return false; // Already exists
            }

            NetworkLocationMappings[networkPrefix] = location;
            return true;
        }

        /// <summary>
        /// Updates an existing network location mapping
        /// </summary>
        /// <param name="networkPrefix">Network prefix</param>
        /// <param name="location">New location name</param>
        /// <returns>True if updated successfully, false if not found</returns>
        public static bool UpdateNetworkLocationMapping(string networkPrefix, string location)
        {
            if (string.IsNullOrWhiteSpace(networkPrefix) || string.IsNullOrWhiteSpace(location))
            {
                return false;
            }

            if (!NetworkLocationMappings.ContainsKey(networkPrefix))
            {
                return false; // Doesn't exist
            }

            NetworkLocationMappings[networkPrefix] = location;
            return true;
        }

        /// <summary>
        /// Removes a network location mapping
        /// </summary>
        /// <param name="networkPrefix">Network prefix to remove</param>
        /// <returns>True if removed successfully, false if not found</returns>
        public static bool RemoveNetworkLocationMapping(string networkPrefix)
        {
            return NetworkLocationMappings.Remove(networkPrefix);
        }
    }
}