using Inventory.Domain.Entities;
using System.Text.RegularExpressions;

namespace Inventory.Api.Helpers
{
    public static class DeviceValidator
    {
        public static List<string> ValidateDevice(Device device)
        {
            var errors = new List<string>();

            // Basic validation
            if (string.IsNullOrWhiteSpace(device.Name))
                errors.Add("Device name is required");

            // IP address validation
            if (!string.IsNullOrWhiteSpace(device.IpAddress) && !IsValidIpAddress(device.IpAddress))
                errors.Add("Invalid IP address format");

            // MAC address validation
            if (!string.IsNullOrWhiteSpace(device.MacAddress) && !IsValidMacAddress(device.MacAddress))
                errors.Add("Invalid MAC address format");

            // Device type specific validation
            errors.AddRange(ValidateByDeviceType(device));

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
                    // Network devices should have IP address
                    if (string.IsNullOrWhiteSpace(device.IpAddress))
                        errors.Add("Network devices must have an IP address");
                    break;

                case DeviceType.Laptop:
                case DeviceType.Desktop:
                case DeviceType.Server:
                    // Computers should have more detailed hardware info if agent is installed
                    if (device.AgentInstalled && device.HardwareInfo == null)
                        errors.Add("Agent-managed devices should have hardware information");
                    break;

                case DeviceType.Printer:
                case DeviceType.Scanner:
                    // Printers/scanners should have a model
                    if (string.IsNullOrWhiteSpace(device.Model))
                        errors.Add("Printers and scanners should have a model specified");
                    break;

                case DeviceType.IPPhone:
                    // IP phones must have IP address
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