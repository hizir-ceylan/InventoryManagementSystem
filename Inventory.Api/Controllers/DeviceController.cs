using Microsoft.AspNetCore.Mvc;
using Inventory.Domain.Entities;
using Inventory.Api.Helpers;
using Inventory.Api.DTOs;
using Inventory.Api.Services;
using Swashbuckle.AspNetCore.Annotations;

namespace Inventory.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [SwaggerTag("Device management operations - supports both agent-installed and network-discovered devices")]
    public class DeviceController : ControllerBase
    {
        private readonly IDeviceService _deviceService;
        private readonly ILogger<DeviceController> _logger;

        public DeviceController(IDeviceService deviceService, ILogger<DeviceController> logger)
        {
            _deviceService = deviceService;
            _logger = logger;
        }

        [HttpGet]
        [SwaggerOperation(Summary = "Tüm cihazları getir", Description = "Envanterdeki tüm cihazları döndürür")]
        [SwaggerResponse(200, "Cihaz listesini döndürür", typeof(IEnumerable<Device>))]
        public async Task<ActionResult<IEnumerable<Device>>> GetAll()
        {
            var devices = await _deviceService.GetAllDevicesAsync();
            return Ok(devices);
        }

        [HttpGet("agent-installed")]
        [SwaggerOperation(Summary = "Agent kurulu cihazları getir", Description = "Sadece agent kurulu cihazları döndürür")]
        [SwaggerResponse(200, "Agent kurulu cihaz listesini döndürür", typeof(IEnumerable<Device>))]
        public async Task<ActionResult<IEnumerable<Device>>> GetAgentInstalledDevices()
        {
            var devices = await _deviceService.GetAgentInstalledDevicesAsync();
            return Ok(devices);
        }

        [HttpGet("network-discovered")]
        [SwaggerOperation(Summary = "Ağ keşfi ile bulunan cihazları getir", Description = "Sadece ağ taraması ile keşfedilen cihazları döndürür")]
        [SwaggerResponse(200, "Ağ keşfi ile bulunan cihaz listesini döndürür", typeof(IEnumerable<Device>))]
        public async Task<ActionResult<IEnumerable<Device>>> GetNetworkDiscoveredDevices()
        {
            var devices = await _deviceService.GetNetworkDiscoveredDevicesAsync();
            return Ok(devices);
        }

        [HttpGet("{id}/available-fields")]
        [SwaggerOperation(Summary = "Cihaz için mevcut alanları getir", Description = "Belirli bir cihaz için hangi alanların mevcut olduğu bilgisini döndürür")]
        [SwaggerResponse(200, "Mevcut alan bilgilerini döndürür")]
        [SwaggerResponse(404, "Cihaz bulunamadı")]
        public async Task<ActionResult<object>> GetAvailableFields(Guid id)
        {
            var device = await _deviceService.GetDeviceByIdAsync(id);
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
                CanEdit = !device.AgentInstalled, // Ağ keşfi ile bulunan cihazlar düzenlenebilir
                CanAssignType = device.DeviceType == DeviceType.Unknown
            };

            return Ok(availableFields);
        }

        [HttpGet("{id}")]
        [SwaggerOperation(Summary = "ID ile cihaz getir", Description = "ID'si ile belirli bir cihazı döndürür")]
        [SwaggerResponse(200, "Cihazı döndürür", typeof(Device))]
        [SwaggerResponse(404, "Cihaz bulunamadı")]
        public async Task<ActionResult<Device>> GetById(Guid id)
        {
            var device = await _deviceService.GetDeviceByIdAsync(id);
            if (device == null)
                return NotFound();
            return Ok(device);
        }

        [HttpPost]
        [SwaggerOperation(Summary = "Yeni cihaz oluştur", Description = "Envanterde yeni bir cihaz oluşturur")]
        [SwaggerResponse(201, "Cihaz başarıyla oluşturuldu", typeof(Device))]
        [SwaggerResponse(400, "Geçersiz cihaz verisi")]
        public async Task<ActionResult<Device>> Create(Device device)
        {
            // Cihazı doğrula
            var validationErrors = DeviceValidator.ValidateDevice(device);
            if (validationErrors.Any())
            {
                return BadRequest(new { errors = validationErrors });
            }

            device.Id = Guid.NewGuid();
            
            var createdDevice = await _deviceService.CreateDeviceAsync(device);
            return CreatedAtAction(nameof(GetById), new { id = createdDevice.Id }, createdDevice);
        }

        [HttpPost("network-discovered")]
        [SwaggerOperation(Summary = "Create a network-discovered device", Description = "Creates a new device discovered through network scanning")]
        [SwaggerResponse(201, "Device created successfully", typeof(Device))]
        [SwaggerResponse(400, "Invalid device data")]
        public async Task<ActionResult<Device>> CreateNetworkDiscoveredDevice(NetworkDeviceRegistrationDto deviceDto)
        {
            // DTO'dan cihaz oluştur
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

            // Cihazı doğrula
            var validationErrors = DeviceValidator.ValidateDevice(device);
            if (validationErrors.Any())
            {
                return BadRequest(new { errors = validationErrors });
            }

            // Cihazın zaten var olup olmadığını kontrol et (IP veya MAC ile)
            var existingDevice = await _deviceService.FindDeviceByIpOrMacAsync(device.IpAddress, device.MacAddress);
            if (existingDevice != null)
            {
                // Var olan cihazı güncelle
                return await UpdateExistingNetworkDevice(existingDevice, device);
            }

            // Yeni cihaz ekle
            var createdDevice = await _deviceService.CreateDeviceAsync(device);
            
            _logger.LogInformation("Network-discovered device registered: {DeviceName} ({IpAddress}) - Management: {ManagementType}", 
                device.Name, device.IpAddress, device.ManagementType);
            
            return CreatedAtAction(nameof(GetById), new { id = createdDevice.Id }, createdDevice);
        }

        [HttpPost("register-by-ip-mac")]
        [SwaggerOperation(Summary = "Register or update device by IP/MAC", Description = "Registers a new device or updates an existing one based on IP or MAC address")]
        [SwaggerResponse(200, "Device updated successfully", typeof(Device))]
        [SwaggerResponse(201, "Device created successfully", typeof(Device))]
        [SwaggerResponse(400, "Invalid device data")]
        public async Task<ActionResult<Device>> RegisterOrUpdateByIpMac(NetworkDeviceRegistrationDto deviceDto)
        {
            // Find existing device by IP or MAC
            var existingDevice = await _deviceService.FindDeviceByIpOrMacAsync(deviceDto.IpAddress, deviceDto.MacAddress);
            
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

                var updatedDevice = await _deviceService.UpdateDeviceAsync(existingDevice);
                return Ok(updatedDevice);
            }
            else
            {
                // Create new device
                return await CreateNetworkDiscoveredDevice(deviceDto);
            }
        }

        [HttpPut("{id}")]
        [SwaggerOperation(Summary = "Update a device", Description = "Updates an existing device")]
        [SwaggerResponse(204, "Device updated successfully")]
        [SwaggerResponse(404, "Device not found")]
        [SwaggerResponse(400, "Invalid device data")]
        public async Task<IActionResult> Update(Guid id, Device updatedDevice)
        {
            var device = await _deviceService.GetDeviceByIdAsync(id);
            if (device == null)
                return NotFound();

            // Update device properties
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

            await _deviceService.UpdateDeviceAsync(device);
            return NoContent();
        }

        [HttpPut("{id}/device-type")]
        [SwaggerOperation(Summary = "Assign device type", Description = "Assigns a device type to a network-discovered device")]
        [SwaggerResponse(204, "Device type assigned successfully")]
        [SwaggerResponse(404, "Device not found")]
        [SwaggerResponse(400, "Invalid request")]
        public async Task<IActionResult> AssignDeviceType(Guid id, [FromBody] DeviceType deviceType)
        {
            var device = await _deviceService.GetDeviceByIdAsync(id);
            if (device == null)
                return NotFound();

            // Only allow type assignment for network-discovered devices
            if (device.AgentInstalled)
                return BadRequest(new { error = "Device type cannot be changed for agent-installed devices." });

            device.DeviceType = deviceType;
            await _deviceService.UpdateDeviceAsync(device);

            return NoContent();
        }

        [HttpPut("{id}/network-discovered")]
        [SwaggerOperation(Summary = "Update network-discovered device", Description = "Updates a network-discovered device")]
        [SwaggerResponse(204, "Device updated successfully")]
        [SwaggerResponse(404, "Device not found")]
        [SwaggerResponse(400, "Invalid request")]
        public async Task<IActionResult> UpdateNetworkDiscoveredDevice(Guid id, [FromBody] NetworkDeviceRegistrationDto updateDto)
        {
            var device = await _deviceService.GetDeviceByIdAsync(id);
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

            // Validate updated device
            var validationErrors = DeviceValidator.ValidateDevice(device);
            if (validationErrors.Any())
            {
                return BadRequest(new { errors = validationErrors });
            }

            await _deviceService.UpdateDeviceAsync(device);
            return NoContent();
        }

        [HttpPost("batch")]
        [SwaggerOperation(Summary = "Batch upload devices", Description = "Uploads multiple devices in a single request")]
        [SwaggerResponse(200, "Batch upload completed", typeof(BatchUploadResultDto))]
        [SwaggerResponse(400, "Invalid batch data")]
        public async Task<ActionResult<BatchUploadResultDto>> BatchUpload([FromBody] DeviceBatchDto[] deviceDtos)
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

                    // Create or update device using service
                    await _deviceService.CreateOrUpdateDeviceAsync(device);
                    result.SuccessfulUploads++;
                }
                catch (Exception ex)
                {
                    result.FailedUploads++;
                    result.Errors.Add($"Device {deviceDto.Name}: {ex.Message}");
                }
            }

            _logger.LogInformation("Batch upload completed: {SuccessfulUploads} successful, {FailedUploads} failed", 
                result.SuccessfulUploads, result.FailedUploads);
            
            return Ok(result);
        }

        [HttpDelete("{id}")]
        [SwaggerOperation(Summary = "Delete a device", Description = "Deletes a device from the inventory")]
        [SwaggerResponse(204, "Device deleted successfully")]
        [SwaggerResponse(404, "Device not found")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var success = await _deviceService.DeleteDeviceAsync(id);
            if (!success)
                return NotFound();

            return NoContent();
        }

        // Helper methods
        private async Task<ActionResult<Device>> UpdateExistingNetworkDevice(Device existingDevice, Device newDevice)
        {
            // Update existing device with new information
            existingDevice.Name = newDevice.Name ?? existingDevice.Name;
            existingDevice.DeviceType = newDevice.DeviceType != DeviceType.Unknown ? newDevice.DeviceType : existingDevice.DeviceType;
            existingDevice.Model = newDevice.Model ?? existingDevice.Model;
            existingDevice.Location = newDevice.Location ?? existingDevice.Location;
            existingDevice.ManagementType = newDevice.ManagementType != ManagementType.Unknown ? newDevice.ManagementType : existingDevice.ManagementType;
            existingDevice.AgentInstalled = newDevice.AgentInstalled;

            // Validate updated device
            var validationErrors = DeviceValidator.ValidateDevice(existingDevice);
            if (validationErrors.Any())
            {
                return BadRequest(new { errors = validationErrors });
            }

            var updatedDevice = await _deviceService.UpdateDeviceAsync(existingDevice);
            
            _logger.LogInformation("Network-discovered device updated: {DeviceName} ({IpAddress}) - Management: {ManagementType}", 
                existingDevice.Name, existingDevice.IpAddress, existingDevice.ManagementType);
            
            return Ok(updatedDevice);
        }
    }
}