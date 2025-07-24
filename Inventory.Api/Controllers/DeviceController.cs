using Microsoft.AspNetCore.Mvc;
using Inventory.Domain.Entities;
using Inventory.Api.Helpers;
using Inventory.Api.DTOs;
using Swashbuckle.AspNetCore.Annotations;

namespace Inventory.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [SwaggerTag("Device management operations - supports both agent-installed and network-discovered devices")]
    public class DeviceController : ControllerBase
    {
        // Geçici olarak bellekte cihaz listesi tutuyoruz
        private static readonly List<Device> Devices = new();

        [HttpGet]
        [SwaggerOperation(Summary = "Get all devices", Description = "Returns all devices in the inventory")]
        [SwaggerResponse(200, "Returns the list of devices", typeof(IEnumerable<Device>))]
        public ActionResult<IEnumerable<Device>> GetAll()
        {
            return Ok(Devices);
        }

        [HttpGet("agent-installed")]
        [SwaggerOperation(Summary = "Get agent-installed devices", Description = "Returns only devices that have the agent installed")]
        [SwaggerResponse(200, "Returns the list of agent-installed devices", typeof(IEnumerable<Device>))]
        public ActionResult<IEnumerable<Device>> GetAgentInstalledDevices()
        {
            var agentDevices = Devices.Where(d => d.AgentInstalled || d.ManagementType == ManagementType.Agent).ToList();
            return Ok(agentDevices);
        }

        [HttpGet("network-discovered")]
        [SwaggerOperation(Summary = "Get network-discovered devices", Description = "Returns only devices discovered through network scanning")]
        [SwaggerResponse(200, "Returns the list of network-discovered devices", typeof(IEnumerable<Device>))]
        public ActionResult<IEnumerable<Device>> GetNetworkDiscoveredDevices()
        {
            var networkDevices = Devices.Where(d => !d.AgentInstalled && 
                (d.ManagementType == ManagementType.NetworkDiscovery || d.DiscoveryMethod == DiscoveryMethod.NetworkDiscovery)).ToList();
            return Ok(networkDevices);
        }

        [HttpGet("{id}/available-fields")]
        [SwaggerOperation(Summary = "Get available fields for a device", Description = "Returns information about which fields are available for a specific device")]
        [SwaggerResponse(200, "Returns the available fields information")]
        [SwaggerResponse(404, "Device not found")]
        public ActionResult<object> GetAvailableFields(Guid id)
        {
            var device = Devices.FirstOrDefault(d => d.Id == id);
            if (device == null)
                return NotFound();

            var availableFields = new
            {
                Device = new
                {
                    HasBasicInfo = !string.IsNullOrEmpty(device.Name),
                    HasIpAddress = !string.IsNullOrEmpty(device.IpAddress),
                    HasMacAddress = !string.IsNullOrEmpty(device.MacAddress),
                    HasDeviceType = device.DeviceType != DeviceType.Unknown,
                    HasModel = !string.IsNullOrEmpty(device.Model),
                    HasLocation = !string.IsNullOrEmpty(device.Location),
                    HasManagementType = device.ManagementType != ManagementType.Unknown,
                    HasDiscoveryMethod = device.DiscoveryMethod != DiscoveryMethod.Unknown
                },
                HardwareInfo = new
                {
                    Available = device.HardwareInfo != null,
                    HasCpuInfo = device.HardwareInfo?.Cpu != null,
                    HasMemoryInfo = device.HardwareInfo?.RamGB > 0,
                    HasDiskInfo = device.HardwareInfo?.DiskGB > 0,
                    HasDetailedSpecs = device.HardwareInfo?.RamModules?.Any() == true || device.HardwareInfo?.Disks?.Any() == true
                },
                SoftwareInfo = new
                {
                    Available = device.SoftwareInfo != null,
                    HasOperatingSystem = !string.IsNullOrEmpty(device.SoftwareInfo?.OperatingSystem),
                    HasInstalledApps = device.SoftwareInfo?.InstalledApps?.Any() == true,
                    HasUsers = device.SoftwareInfo?.Users?.Any() == true
                },
                CanEdit = !device.AgentInstalled, // Network discovered devices can be edited
                CanAssignType = device.DeviceType == DeviceType.Unknown
            };

            return Ok(availableFields);
        }

        [HttpGet("{id}")]
        [SwaggerOperation(Summary = "Get device by ID", Description = "Returns a specific device by its ID")]
        [SwaggerResponse(200, "Returns the device", typeof(Device))]
        [SwaggerResponse(404, "Device not found")]
        public ActionResult<Device> GetById(Guid id)
        {
            var device = Devices.FirstOrDefault(d => d.Id == id);
            if (device == null)
                return NotFound();
            return Ok(device);
        }

        [HttpPost]
        [SwaggerOperation(Summary = "Create a new device", Description = "Creates a new device in the inventory")]
        [SwaggerResponse(201, "Device created successfully", typeof(Device))]
        [SwaggerResponse(400, "Invalid device data")]
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
        [SwaggerOperation(Summary = "Create a network-discovered device", Description = "Creates a new device discovered through network scanning")]
        [SwaggerResponse(201, "Device created successfully", typeof(Device))]
        [SwaggerResponse(400, "Invalid device data")]
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
        [SwaggerOperation(Summary = "Register or update device by IP/MAC", Description = "Registers a new device or updates an existing one based on IP or MAC address")]
        [SwaggerResponse(200, "Device updated successfully", typeof(Device))]
        [SwaggerResponse(201, "Device created successfully", typeof(Device))]
        [SwaggerResponse(400, "Invalid device data")]
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
        [SwaggerOperation(Summary = "Update a device", Description = "Updates an existing device")]
        [SwaggerResponse(204, "Device updated successfully")]
        [SwaggerResponse(404, "Device not found")]
        [SwaggerResponse(400, "Invalid device data")]
        public IActionResult Update(Guid id, Device updatedDevice)
        {
            var device = Devices.FirstOrDefault(d => d.Id == id);
            if (device == null)
                return NotFound();

        // Basit güncelleme
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

        [HttpPut("{id}/device-type")]
        [SwaggerOperation(Summary = "Assign device type", Description = "Assigns a device type to a network-discovered device")]
        [SwaggerResponse(204, "Device type assigned successfully")]
        [SwaggerResponse(404, "Device not found")]
        [SwaggerResponse(400, "Invalid request")]
        public IActionResult AssignDeviceType(Guid id, [FromBody] DeviceType deviceType)
        {
            var device = Devices.FirstOrDefault(d => d.Id == id);
            if (device == null)
                return NotFound();

            // Only allow type assignment for network-discovered devices
            if (device.AgentInstalled)
                return BadRequest(new { error = "Device type cannot be changed for agent-installed devices." });

            device.DeviceType = deviceType;
            device.LastSeen = DateTime.UtcNow;

            return NoContent();
        }

        [HttpPut("{id}/network-discovered")]
        [SwaggerOperation(Summary = "Update network-discovered device", Description = "Updates a network-discovered device")]
        [SwaggerResponse(204, "Device updated successfully")]
        [SwaggerResponse(404, "Device not found")]
        [SwaggerResponse(400, "Invalid request")]
        public IActionResult UpdateNetworkDiscoveredDevice(Guid id, [FromBody] NetworkDeviceRegistrationDto updateDto)
        {
            var device = Devices.FirstOrDefault(d => d.Id == id);
            if (device == null)
                return NotFound();

            // Only allow updates for network-discovered devices
            if (device.AgentInstalled)
                return BadRequest(new { error = "Agent-installed devices cannot be updated through this endpoint." });

            // Update only provided fields
            if (!string.IsNullOrEmpty(updateDto.Name))
                device.Name = updateDto.Name;
            if (!string.IsNullOrEmpty(updateDto.IpAddress))
                device.IpAddress = updateDto.IpAddress;
            if (!string.IsNullOrEmpty(updateDto.MacAddress))
                device.MacAddress = updateDto.MacAddress;
            if (updateDto.DeviceType != DeviceType.Unknown)
                device.DeviceType = updateDto.DeviceType;
            if (!string.IsNullOrEmpty(updateDto.Model))
                device.Model = updateDto.Model;
            if (!string.IsNullOrEmpty(updateDto.Location))
                device.Location = updateDto.Location;
            if (updateDto.ManagementType != ManagementType.Unknown)
                device.ManagementType = updateDto.ManagementType;

            device.LastSeen = DateTime.UtcNow;

            // Validate updated device
            var validationErrors = DeviceValidator.ValidateDevice(device);
            if (validationErrors.Any())
            {
                return BadRequest(new { errors = validationErrors });
            }

            return NoContent();
        }

        [HttpPost("batch")]
        [SwaggerOperation(Summary = "Batch upload devices", Description = "Uploads multiple devices in a single request")]
        [SwaggerResponse(200, "Batch upload completed", typeof(BatchUploadResultDto))]
        [SwaggerResponse(400, "Invalid batch data")]
        public ActionResult<BatchUploadResultDto> BatchUpload([FromBody] DeviceBatchDto[] deviceDtos)
        {
            var result = new BatchUploadResultDto
            {
                TotalDevices = deviceDtos.Length,
                SuccessfulUploads = 0,
                FailedUploads = 0,
                Errors = new List<string>()
            };

            foreach (var deviceDto in deviceDtos)
            {
                try
                {
                    // Convert DTO to Device entity
                    var device = new Device
                    {
                        Id = Guid.NewGuid(),
                        Name = deviceDto.Name,
                        MacAddress = deviceDto.MacAddress,
                        IpAddress = deviceDto.IpAddress,
                        DeviceType = deviceDto.DeviceType,
                        Model = deviceDto.Model,
                        Location = deviceDto.Location,
                        LastSeen = DateTime.UtcNow,
                        ManagementType = ManagementType.Agent,
                        DiscoveryMethod = DiscoveryMethod.Agent,
                        Status = 1, // Active
                        ChangeLogs = new List<ChangeLog>()
                    };

                    // Set default values for required fields if they're null
                    if (deviceDto.HardwareInfo == null)
                    {
                        device.HardwareInfo = new DeviceHardwareInfo
                        {
                            Cpu = "",
                            Motherboard = "",
                            MotherboardSerial = "",
                            BiosManufacturer = "",
                            BiosVersion = "",
                            BiosSerial = "",
                            RamModules = new List<RamModule>(),
                            Disks = new List<DiskInfo>(),
                            Gpus = new List<GpuInfo>(),
                            NetworkAdapters = new List<NetworkAdapter>()
                        };
                    }
                    else
                    {
                        device.HardwareInfo = deviceDto.HardwareInfo;
                    }

                    if (deviceDto.SoftwareInfo == null)
                    {
                        device.SoftwareInfo = new DeviceSoftwareInfo
                        {
                            OperatingSystem = "",
                            OsVersion = "",
                            OsArchitecture = "",
                            RegisteredUser = "",
                            SerialNumber = "",
                            ActiveUser = "",
                            InstalledApps = new List<string>(),
                            Updates = new List<string>(),
                            Users = new List<string>()
                        };
                    }
                    else
                    {
                        device.SoftwareInfo = deviceDto.SoftwareInfo;
                    }
                    
                    // Validate device
                    var validationErrors = DeviceValidator.ValidateDevice(device);
                    if (validationErrors.Any())
                    {
                        result.FailedUploads++;
                        result.Errors.Add($"Device {device.Name}: {string.Join(", ", validationErrors)}");
                        continue;
                    }

                    // Check if device already exists
                    var existingDevice = FindDeviceByIpOrMac(device.IpAddress, device.MacAddress);
                    if (existingDevice != null)
                    {
                        // Update existing device
                        existingDevice.Name = device.Name ?? existingDevice.Name;
                        existingDevice.DeviceType = device.DeviceType != DeviceType.Unknown ? device.DeviceType : existingDevice.DeviceType;
                        existingDevice.Model = device.Model ?? existingDevice.Model;
                        existingDevice.Location = device.Location ?? existingDevice.Location;
                        existingDevice.LastSeen = DateTime.UtcNow;
                        existingDevice.HardwareInfo = device.HardwareInfo ?? existingDevice.HardwareInfo;
                        existingDevice.SoftwareInfo = device.SoftwareInfo ?? existingDevice.SoftwareInfo;
                    }
                    else
                    {
                        // Add new device
                        Devices.Add(device);
                    }

                    result.SuccessfulUploads++;
                }
                catch (Exception ex)
                {
                    result.FailedUploads++;
                    result.Errors.Add($"Device {deviceDto.Name}: {ex.Message}");
                }
            }

            Console.WriteLine($"Batch upload completed: {result.SuccessfulUploads} successful, {result.FailedUploads} failed");
            
            return Ok(result);
        }

        [HttpDelete("{id}")]
        [SwaggerOperation(Summary = "Delete a device", Description = "Deletes a device from the inventory")]
        [SwaggerResponse(204, "Device deleted successfully")]
        [SwaggerResponse(404, "Device not found")]
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