using Inventory.Domain.Entities;
using System.Text.RegularExpressions;

namespace Inventory.Api.Helpers
{
    public static class DeviceValidator
    {
        public static List<string> ValidateDevice(Device device)
        {
            var errors = new List<string>();

            // Temel doğrulama - Network Discovery cihazları için daha esnek
            if (device.DiscoveryMethod == DiscoveryMethod.NetworkDiscovery)
            {
                // Network discovery cihazları için minimal doğrulama
                // En az IP veya MAC adresinden birinin olması yeterli
                if (string.IsNullOrWhiteSpace(device.IpAddress) && string.IsNullOrWhiteSpace(device.MacAddress))
                    errors.Add("Network discovered devices must have at least IP or MAC address");
                
                // İsim yoksa IP veya MAC'den varsayılan isim oluştur
                if (string.IsNullOrWhiteSpace(device.Name))
                {
                    device.Name = device.IpAddress ?? device.MacAddress ?? "Unknown Device";
                }
            }
            else
            {
                // Agent ve manuel cihazlar için normal doğrulama
                if (string.IsNullOrWhiteSpace(device.Name))
                    errors.Add("Device name is required");
            }

            // IP adresi doğrulaması
            if (!string.IsNullOrWhiteSpace(device.IpAddress) && !IsValidIpAddress(device.IpAddress))
                errors.Add("Invalid IP address format");

            // MAC adresi doğrulaması
            if (!string.IsNullOrWhiteSpace(device.MacAddress) && !IsValidMacAddress(device.MacAddress))
                errors.Add("Invalid MAC address format");

            // Cihaz tipine özel doğrulama - sadece agent cihazları için
            if (device.DiscoveryMethod != DiscoveryMethod.NetworkDiscovery)
            {
                errors.AddRange(ValidateByDeviceType(device));
            }

            return errors;
        }

        private static List<string> ValidateByDeviceType(Device device)
        {
            var errors = new List<string>();

            switch (device.DeviceType)
            {
                case DeviceType.Router:
                case DeviceType.Switch:
                case DeviceType.AccessPoint:
                case DeviceType.NetworkDevice:
                    // Ağ cihazları IP adresine sahip olmalıdır
                    if (string.IsNullOrWhiteSpace(device.IpAddress))
                        errors.Add("Network devices must have an IP address");
                    break;

                case DeviceType.Laptop:
                case DeviceType.Desktop:
                case DeviceType.Server:
                    // Agent yüklü bilgisayarlar daha detaylı donanım bilgisine sahip olmalıdır
                    if (device.AgentInstalled && device.HardwareInfo == null)
                        errors.Add("Agent-managed devices should have hardware information");
                    break;

                case DeviceType.Printer:
                case DeviceType.Scanner:
                    // Yazıcılar/tarayıcılar bir modele sahip olmalıdır
                    if (string.IsNullOrWhiteSpace(device.Model))
                        errors.Add("Printers and scanners should have a model specified");
                    break;

                case DeviceType.IPPhone:
                    // IP telefonlar IP adresine sahip olmalıdır
                    if (string.IsNullOrWhiteSpace(device.IpAddress))
                        errors.Add("IP phones must have an IP address");
                    break;
            }

            return errors;
        }

        private static bool IsValidIpAddress(string ipAddress)
        {
            var regex = new Regex(@"^(?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)$");
            return regex.IsMatch(ipAddress);
        }

        private static bool IsValidMacAddress(string macAddress)
        {
            var regex = new Regex(@"^[0-9A-Fa-f]{2}[:-]?[0-9A-Fa-f]{2}[:-]?[0-9A-Fa-f]{2}[:-]?[0-9A-Fa-f]{2}[:-]?[0-9A-Fa-f]{2}[:-]?[0-9A-Fa-f]{2}$");
            return regex.IsMatch(macAddress);
        }
    }
}