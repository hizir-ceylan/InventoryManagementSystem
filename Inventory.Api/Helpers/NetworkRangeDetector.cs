using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace Inventory.Api.Helpers
{
    public static class NetworkRangeDetector
    {
        /// <summary>
        /// Cihazın ağ arayüzlerine göre yerel ağ aralıklarını otomatik olarak algılar
        /// Kullanıcının özel IP aralıkları için geliştirilmiş destek
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
                                
                                // Özel IP aralıkları için ek tarama aralıkları öner
                                var additionalRanges = GetAdditionalScanRanges(unicastAddress.Address);
                                foreach (var additionalRange in additionalRanges)
                                {
                                    if (!networkRanges.Contains(additionalRange))
                                    {
                                        networkRanges.Add(additionalRange);
                                    }
                                }
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

            // Hiçbir aralık algılanmadıysa yedek olarak geniş özel aralıkları ekle
            if (!networkRanges.Any())
            {
                networkRanges.AddRange(new[]
                {
                    "192.168.1.0/24",  // Yaygın ev ağları
                    "192.168.0.0/24",  // Yaygın ev ağları
                    "10.0.0.0/24",     // Kurumsal ağlar
                    "172.16.0.0/24",   // Özel ağlar
                    "100.64.0.0/24",   // Carrier-grade NAT
                    "105.0.0.0/24",    // Kullanıcının belirttiği aralık
                    "112.0.0.0/24"     // Kullanıcının belirttiği aralık
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

        /// <summary>
        /// IP adresine göre ek tarama aralıkları önerir - özellikle özel/kurumsal ağlar için
        /// </summary>
        /// <param name="ipAddress">Referans IP adresi</param>
        /// <returns>Önerilen ek tarama aralıkları</returns>
        private static List<string> GetAdditionalScanRanges(IPAddress ipAddress)
        {
            var additionalRanges = new List<string>();
            var ipBytes = ipAddress.GetAddressBytes();
            var firstOctet = ipBytes[0];
            var secondOctet = ipBytes[1];

            // Özel IP aralıkları için ek tarama aralıkları öner
            switch (firstOctet)
            {
                case 10:
                    // 10.x.x.x ağındaysa, yaygın 10.x alt ağlarını öner
                    additionalRanges.AddRange(new[]
                    {
                        "10.0.0.0/24",
                        "10.0.1.0/24",
                        "10.1.0.0/24",
                        $"10.{secondOctet}.0.0/24"
                    });
                    break;

                case 172:
                    // 172.16.x.x - 172.31.x.x aralığındaysa
                    if (secondOctet >= 16 && secondOctet <= 31)
                    {
                        additionalRanges.AddRange(new[]
                        {
                            "172.16.0.0/24",
                            "172.17.0.0/24",
                            $"172.{secondOctet}.0.0/24"
                        });
                    }
                    break;

                case 192:
                    // 192.168.x.x aralığındaysa
                    if (secondOctet == 168)
                    {
                        additionalRanges.AddRange(new[]
                        {
                            "192.168.0.0/24",
                            "192.168.1.0/24",
                            "192.168.2.0/24",
                            $"192.168.{ipBytes[2]}.0/24"
                        });
                    }
                    break;

                case 100:
                    // 100.64.x.x - 100.127.x.x (Carrier-grade NAT)
                    if (secondOctet >= 64 && secondOctet <= 127)
                    {
                        additionalRanges.AddRange(new[]
                        {
                            "100.64.0.0/24",
                            $"100.{secondOctet}.0.0/24"
                        });
                    }
                    break;

                // Kullanıcının belirttiği özel aralıklar
                case 105:
                    additionalRanges.AddRange(new[]
                    {
                        "105.0.0.0/24",
                        $"105.{secondOctet}.0.0/24",
                        $"105.{secondOctet}.{ipBytes[2]}.0/24"
                    });
                    break;

                case 112:
                    additionalRanges.AddRange(new[]
                    {
                        "112.0.0.0/24",
                        $"112.{secondOctet}.0.0/24",
                        $"112.{secondOctet}.{ipBytes[2]}.0/24"
                    });
                    break;

                // Diğer kurumsal aralıklar (Class A özel aralıkları)
                default:
                    if (firstOctet >= 1 && firstOctet <= 126 && firstOctet != 127)
                    {
                        // Muhtemelen özel/kurumsal ağ
                        additionalRanges.Add($"{firstOctet}.{secondOctet}.{ipBytes[2]}.0/24");
                    }
                    break;
            }

            return additionalRanges.Distinct().ToList();
        }

        /// <summary>
        /// IP adresinin özel (private) aralıkta olup olmadığını kontrol eder
        /// </summary>
        /// <param name="ipAddress">Kontrol edilecek IP adresi</param>
        /// <returns>Özel aralıktaysa true</returns>
        public static bool IsPrivateIPAddress(IPAddress ipAddress)
        {
            var ipBytes = ipAddress.GetAddressBytes();
            var firstOctet = ipBytes[0];
            var secondOctet = ipBytes[1];

            return firstOctet switch
            {
                10 => true, // 10.0.0.0/8
                172 when secondOctet >= 16 && secondOctet <= 31 => true, // 172.16.0.0/12
                192 when secondOctet == 168 => true, // 192.168.0.0/16
                100 when secondOctet >= 64 && secondOctet <= 127 => true, // 100.64.0.0/10 (Carrier-grade NAT)
                _ => false
            };
        }
    }
}