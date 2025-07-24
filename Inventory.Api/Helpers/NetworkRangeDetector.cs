using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace Inventory.Api.Helpers
{
    public static class NetworkRangeDetector
    {
        /// <summary>
        /// Cihazın ağ arayüzlerine göre yerel ağ aralıklarını otomatik olarak algılar
        /// </summary>
        /// <returns>CIDR notasyonunda ağ aralıkları listesi (örn., "192.168.1.0/24")</returns>
        public static List<string> GetLocalNetworkRanges()
        {
            var networkRanges = new List<string>();

            try
            {
                var networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();

                foreach (var networkInterface in networkInterfaces)
                {
                    // Loopback ve çalışmayan arayüzleri atla
                    if (networkInterface.NetworkInterfaceType == NetworkInterfaceType.Loopback ||
                        networkInterface.OperationalStatus != OperationalStatus.Up)
                    {
                        continue;
                    }

                    var ipProperties = networkInterface.GetIPProperties();

                    foreach (var unicastAddress in ipProperties.UnicastAddresses)
                    {
                        // Sadece IPv4 adreslerini işle
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
                // Hatayı günlükle ancak boş liste döndür
                Console.WriteLine($"Error detecting network ranges: {ex.Message}");
            }

            // Hiçbir aralık algılanmadıysa yedek olarak yaygın özel aralıkları ekle
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
        /// Birincil yerel ağ aralığını alır (ilk loopback olmayan arayüz)
        /// </summary>
        /// <returns>CIDR notasyonunda birincil ağ aralığı</returns>
        public static string GetPrimaryNetworkRange()
        {
            var ranges = GetLocalNetworkRanges();
            return ranges.FirstOrDefault() ?? "192.168.1.0/24";
        }

        /// <summary>
        /// IP adresi ve alt ağ maskesinden CIDR notasyonunda ağ aralığını hesaplar
        /// </summary>
        /// <param name="ipAddress">IP adresi</param>
        /// <param name="subnetMask">Alt ağ maskesi</param>
        /// <returns>CIDR notasyonunda ağ aralığı (örn., "192.168.1.0/24")</returns>
        private static string CalculateNetworkRange(IPAddress ipAddress, IPAddress subnetMask)
        {
            try
            {
                var ipBytes = ipAddress.GetAddressBytes();
                var maskBytes = subnetMask.GetAddressBytes();

                // Ağ adresini hesapla
                var networkBytes = new byte[4];
                for (int i = 0; i < 4; i++)
                {
                    networkBytes[i] = (byte)(ipBytes[i] & maskBytes[i]);
                }

                var networkAddress = new IPAddress(networkBytes);

                // CIDR önek uzunluğunu hesapla
                var prefixLength = CalculatePrefixLength(subnetMask);

                return $"{networkAddress}/{prefixLength}";
            }
            catch
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Alt ağ maskesinden CIDR önek uzunluğunu hesaplar
        /// </summary>
        /// <param name="subnetMask">Alt ağ maskesi</param>
        /// <returns>CIDR önek uzunluğu</returns>
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
        /// Birincil ağ arayüzünün yerel IP adresini alır
        /// </summary>
        /// <returns>Yerel IP adresi</returns>
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