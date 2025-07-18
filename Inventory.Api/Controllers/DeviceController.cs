using Microsoft.AspNetCore.Mvc;
using Inventory.Domain.Entities;
using Inventory.Api.Helpers;
using Inventory.Api.DTOs;

namespace Inventory.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DeviceController : ControllerBase
    {
        // Ge�ici olarak bellekte cihaz listesi tutuyoruz
        private static readonly List<Device> Devices = new();

        [HttpGet]
        public ActionResult<IEnumerable<Device>> GetAll()
        {
            return Ok(Devices);
        }

        [HttpGet("{id}")]
        public ActionResult<Device> GetById(Guid id)
        {
            var device = Devices.FirstOrDefault(d => d.Id == id);
            if (device == null)
                return NotFound();
            return Ok(device);
        }

        [HttpPost]
        public ActionResult<Device> Create(Device device)
        {
            // Validate device
            var validationErrors = DeviceValidator.ValidateDevice(device);
            if (validationErrors.Any())
            {
                return BadRequest(new { errors = validationErrors });
            }

            device.Id = Guid.NewGuid();
            device.LastSeen = DateTime.UtcNow;
            
            // Set default values if not provided
            if (device.ManagementType == ManagementType.Unknown)
                device.ManagementType = ManagementType.Manual;
            if (device.DiscoveryMethod == DiscoveryMethod.Unknown)
                device.DiscoveryMethod = DiscoveryMethod.Manual;
            
            Devices.Add(device);
            return CreatedAtAction(nameof(GetById), new { id = device.Id }, device);
        }

        [HttpPost("network-discovered")]
        public ActionResult<Device> CreateNetworkDiscoveredDevice(NetworkDeviceRegistrationDto deviceDto)
        {
            // Create device from DTO
            var device = new Device
            {
                Id = Guid.NewGuid(),
                Name = deviceDto.Name,
                IpAddress = deviceDto.IpAddress,
                MacAddress = deviceDto.MacAddress,
                DeviceType = deviceDto.DeviceType,
                Model = deviceDto.Model,
                Location = deviceDto.Location ?? "Network Discovery",
                ManagementType = deviceDto.ManagementType,
                AgentInstalled = deviceDto.AgentInstalled,
                DiscoveryMethod = DiscoveryMethod.NetworkDiscovery,
                LastSeen = DateTime.UtcNow,
                Status = 1, // Active
                ChangeLogs = new List<ChangeLog>()
            };

            // Validate device
            var validationErrors = DeviceValidator.ValidateDevice(device);
            if (validationErrors.Any())
            {
                return BadRequest(new { errors = validationErrors });
            }

            // Check if device already exists (by IP or MAC)
            var existingDevice = FindDeviceByIpOrMac(device.IpAddress, device.MacAddress);
            if (existingDevice != null)
            {
                // Update existing device
                return UpdateExistingNetworkDevice(existingDevice, device);
            }

            // Add new device
            Devices.Add(device);
            
            Console.WriteLine($"Network-discovered device registered: {device.Name} ({device.IpAddress}) - Management: {device.ManagementType}");
            
            return CreatedAtAction(nameof(GetById), new { id = device.Id }, device);
        }

        [HttpPost("register-by-ip-mac")]
        public ActionResult<Device> RegisterOrUpdateByIpMac(NetworkDeviceRegistrationDto deviceDto)
        {
            // Find existing device by IP or MAC
            var existingDevice = FindDeviceByIpOrMac(deviceDto.IpAddress, deviceDto.MacAddress);
            
            if (existingDevice != null)
            {
                // Update existing device
                existingDevice.Name = deviceDto.Name ?? existingDevice.Name;
                existingDevice.DeviceType = deviceDto.DeviceType != DeviceType.Unknown ? deviceDto.DeviceType : existingDevice.DeviceType;
                existingDevice.Model = deviceDto.Model ?? existingDevice.Model;
                existingDevice.Location = deviceDto.Location ?? existingDevice.Location;
                existingDevice.ManagementType = deviceDto.ManagementType != ManagementType.Unknown ? deviceDto.ManagementType : existingDevice.ManagementType;
                existingDevice.AgentInstalled = deviceDto.AgentInstalled;
                existingDevice.LastSeen = DateTime.UtcNow;

                // Validate updated device
                var validationErrors = DeviceValidator.ValidateDevice(existingDevice);
                if (validationErrors.Any())
                {
                    return BadRequest(new { errors = validationErrors });
                }

                return Ok(existingDevice);
            }
            else
            {
                // Create new device
                return CreateNetworkDiscoveredDevice(deviceDto);
            }
        }

        [HttpPut("{id}")]
        public IActionResult Update(Guid id, Device updatedDevice)
        {
            var device = Devices.FirstOrDefault(d => d.Id == id);
            if (device == null)
                return NotFound();

            // Basit g�ncelleme
            device.Name = updatedDevice.Name;
            device.MacAddress = updatedDevice.MacAddress;
            device.IpAddress = updatedDevice.IpAddress;
            device.DeviceType = updatedDevice.DeviceType;
            device.Model = updatedDevice.Model;
            device.Location = updatedDevice.Location;
            device.Status = updatedDevice.Status;
            device.HardwareInfo = updatedDevice.HardwareInfo;
            device.SoftwareInfo = updatedDevice.SoftwareInfo;
            device.AgentInstalled = updatedDevice.AgentInstalled;
            device.ManagementType = updatedDevice.ManagementType;
            device.DiscoveryMethod = updatedDevice.DiscoveryMethod;
            device.LastSeen = DateTime.UtcNow;

            return NoContent();
        }

        [HttpDelete("{id}")]
        public IActionResult Delete(Guid id)
        {
            var device = Devices.FirstOrDefault(d => d.Id == id);
            if (device == null)
                return NotFound();

            Devices.Remove(device);
            return NoContent();
        }

        // Helper methods
        private Device? FindDeviceByIpOrMac(string? ipAddress, string? macAddress)
        {
            return Devices.FirstOrDefault(d => 
                (!string.IsNullOrWhiteSpace(ipAddress) && d.IpAddress == ipAddress) ||
                (!string.IsNullOrWhiteSpace(macAddress) && d.MacAddress == macAddress));
        }

        private ActionResult<Device> UpdateExistingNetworkDevice(Device existingDevice, Device newDevice)
        {
            // Update existing device with new information
            existingDevice.Name = newDevice.Name ?? existingDevice.Name;
            existingDevice.DeviceType = newDevice.DeviceType != DeviceType.Unknown ? newDevice.DeviceType : existingDevice.DeviceType;
            existingDevice.Model = newDevice.Model ?? existingDevice.Model;
            existingDevice.Location = newDevice.Location ?? existingDevice.Location;
            existingDevice.ManagementType = newDevice.ManagementType != ManagementType.Unknown ? newDevice.ManagementType : existingDevice.ManagementType;
            existingDevice.AgentInstalled = newDevice.AgentInstalled;
            existingDevice.LastSeen = DateTime.UtcNow;

            // Validate updated device
            var validationErrors = DeviceValidator.ValidateDevice(existingDevice);
            if (validationErrors.Any())
            {
                return BadRequest(new { errors = validationErrors });
            }

            Console.WriteLine($"Network-discovered device updated: {existingDevice.Name} ({existingDevice.IpAddress}) - Management: {existingDevice.ManagementType}");
            
            return Ok(existingDevice);
        }
    }
}