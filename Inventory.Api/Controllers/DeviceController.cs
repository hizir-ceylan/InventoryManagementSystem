using Microsoft.AspNetCore.Mvc;
using Inventory.Domain.Entities;
using Inventory.Api.Helpers;
using Inventory.Api.DTOs;
using Inventory.Api.Services;
using Inventory.Shared.Utils;
using Swashbuckle.AspNetCore.Annotations;

namespace Inventory.Api.Controllers
{
    /// <summary>
    /// Cihaz Yönetimi Controller'ı
    /// Agent kurulu ve ağ keşfi ile bulunan cihazları yönetir
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [SwaggerTag("Device management operations - supports both agent-installed and network-discovered devices")]
    public class DeviceController : ControllerBase
    {
        #region Fields ve Constructor
        
        private readonly IDeviceService _deviceService;
        private readonly ILogger<DeviceController> _logger;

        public DeviceController(IDeviceService deviceService, ILogger<DeviceController> logger)
        {
            _deviceService = deviceService;
            _logger = logger;
        }
        
        #endregion

        #region GET - Cihaz Listeleme ve Sorgulama İşlemleri

        /// <summary>
        /// Tüm cihazları getirir
        /// </summary>
        [HttpGet]
        [SwaggerOperation(Summary = "Tüm cihazları getir", Description = "Envanterdeki tüm cihazları döndürür")]
        [SwaggerResponse(200, "Cihaz listesini döndürür", typeof(IEnumerable<Device>))]
        public async Task<ActionResult<IEnumerable<Device>>> GetAll()
        {
            var devices = await _deviceService.GetAllDevicesAsync();
            return Ok(devices);
        }

        /// <summary>
        /// Agent kurulu cihazları getirir
        /// </summary>
        [HttpGet("agent-installed")]
        [SwaggerOperation(Summary = "Agent kurulu cihazları getir", Description = "Sadece agent kurulu cihazları döndürür")]
        [SwaggerResponse(200, "Agent kurulu cihaz listesini döndürür", typeof(IEnumerable<Device>))]
        public async Task<ActionResult<IEnumerable<Device>>> GetAgentInstalledDevices()
        {
            var devices = await _deviceService.GetAgentInstalledDevicesAsync();
            return Ok(devices);
        }

        /// <summary>
        /// Ağ keşfi ile bulunan cihazları getirir
        /// </summary>
        [HttpGet("network-discovered")]
        [SwaggerOperation(Summary = "Ağ keşfi ile bulunan cihazları getir", Description = "Sadece ağ taraması ile keşfedilen cihazları döndürür")]
        [SwaggerResponse(200, "Ağ keşfi ile bulunan cihaz listesini döndürür", typeof(IEnumerable<Device>))]
        public async Task<ActionResult<IEnumerable<Device>>> GetNetworkDiscoveredDevices()
        {
            var devices = await _deviceService.GetNetworkDiscoveredDevicesAsync();
            return Ok(devices);
        }

        /// <summary>
        /// Belirli bir cihazın mevcut alanlarını getirir
        /// </summary>
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

        /// <summary>
        /// ID ile belirli bir cihazı getirir
        /// </summary>
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
        
        #endregion

        #region POST - Cihaz Oluşturma İşlemleri

        /// <summary>
        /// Yeni cihaz oluşturur veya mevcut cihazı günceller
        /// </summary>
        [HttpPost]
        [SwaggerOperation(Summary = "Yeni cihaz oluştur veya güncelle", Description = "MAC adresine göre mevcut cihazı bulur ve günceller veya yeni cihaz oluşturur")]
        [SwaggerResponse(201, "Cihaz başarıyla oluşturuldu", typeof(Device))]
        [SwaggerResponse(200, "Mevcut cihaz başarıyla güncellendi", typeof(Device))]
        [SwaggerResponse(400, "Geçersiz cihaz verisi")]
        public async Task<ActionResult<Device>> Create(Device device)
        {
            // Lokasyon atanmamışsa dinamik olarak belirle
            if (string.IsNullOrWhiteSpace(device.Location))
            {
                device.Location = LocationHelper.GetLocationByIpAddress(device.IpAddress, "Network Discovery");
            }

            // Cihaz verilerini doğrula
            var validationErrors = DeviceValidator.ValidateDevice(device);
            if (validationErrors.Any())
            {
                return BadRequest(new { errors = validationErrors });
            }

            // Mevcut cihaz kontrolü ve oluşturma/güncelleme işlemi
            var result = await _deviceService.CreateOrUpdateDeviceAsync(device);
            if (result == null)
            {
                return BadRequest(new { error = "Failed to create or update device" });
            }

            // Yeni cihaz mı güncelleme mi kontrol et
            var isNewDevice = device.Id == Guid.Empty;
            
            if (isNewDevice)
            {
                _logger.LogInformation("Yeni cihaz oluşturuldu: {DeviceName} ({MacAddress})", result.Name, result.MacAddress);
                return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
            }
            else
            {
                _logger.LogInformation("Mevcut cihaz güncellendi: {DeviceName} ({MacAddress})", result.Name, result.MacAddress);
                return Ok(result);
            }
        }

        /// <summary>
        /// Ağ keşfi ile bulunan cihaz kaydeder
        /// </summary>
        [HttpPost("network-discovered")]
        [SwaggerOperation(Summary = "Ağ keşfi ile bulunan cihaz oluştur", Description = "Ağ taraması ile keşfedilen yeni bir cihaz oluşturur")]
        [SwaggerResponse(201, "Cihaz başarıyla oluşturuldu", typeof(Device))]
        [SwaggerResponse(400, "Geçersiz cihaz verisi")]
        public async Task<ActionResult<Device>> CreateNetworkDiscoveredDevice(NetworkDeviceRegistrationDto deviceDto)
        {
            // DTO'dan cihaz modeli oluştur
            var device = new Device
            {
                Id = Guid.NewGuid(),
                Name = deviceDto.Name,
                IpAddress = deviceDto.IpAddress,
                MacAddress = deviceDto.MacAddress,
                DeviceType = deviceDto.DeviceType,
                Model = deviceDto.Model,
                Location = LocationHelper.GetLocationByIpAddress(deviceDto.IpAddress, deviceDto.Location ?? "Network Discovery"),
                ManagementType = deviceDto.ManagementType,
                AgentInstalled = deviceDto.AgentInstalled,
                DiscoveryMethod = DiscoveryMethod.NetworkDiscovery,
                LastSeen = TimeZoneHelper.GetUtcNowForStorage(),
                Status = 1, // Active
                ChangeLogs = new List<ChangeLog>()
            };

            // Cihaz verilerini doğrula
            var validationErrors = DeviceValidator.ValidateDevice(device);
            if (validationErrors.Any())
            {
                return BadRequest(new { errors = validationErrors });
            }

            // Mevcut cihaz kontrolü (iyileştirilmiş logic)
            var existingDevice = await _deviceService.FindDeviceByNameMacAndIpAsync(device.Name, device.MacAddress, device.IpAddress);
            if (existingDevice != null)
            {
                // Var olan cihazı güncelle
                return await UpdateExistingNetworkDevice(existingDevice, device);
            }

            // Yeni cihaz oluştur
            var createdDevice = await _deviceService.CreateDeviceAsync(device);
            
            _logger.LogInformation("Ağ keşfi ile cihaz kaydedildi: {DeviceName} ({IpAddress}) - Yönetim: {ManagementType}", 
                device.Name, device.IpAddress, device.ManagementType);
            
            return CreatedAtAction(nameof(GetById), new { id = createdDevice.Id }, createdDevice);
        }

        /// <summary>
        /// IP/MAC adresine göre cihaz kaydeder veya günceller
        /// </summary>
        [SwaggerResponse(200, "Cihaz başarıyla güncellendi", typeof(Device))]
        [SwaggerResponse(201, "Cihaz başarıyla oluşturuldu", typeof(Device))]
        [SwaggerResponse(400, "Geçersiz cihaz verisi")]
        public async Task<ActionResult<Device>> RegisterOrUpdateByIpMac(NetworkDeviceRegistrationDto deviceDto)
        {
            // Mevcut cihazı IP veya MAC ile bul (improved logic)
            var existingDevice = await _deviceService.FindDeviceByNameMacAndIpAsync(deviceDto.Name, deviceDto.MacAddress, deviceDto.IpAddress);
            
            if (existingDevice != null)
            {
                // Mevcut cihazı güncelle
                existingDevice.Name = deviceDto.Name ?? existingDevice.Name;
                existingDevice.DeviceType = deviceDto.DeviceType != DeviceType.Unknown ? deviceDto.DeviceType : existingDevice.DeviceType;
                existingDevice.Model = deviceDto.Model ?? existingDevice.Model;
                existingDevice.Location = deviceDto.Location ?? existingDevice.Location;
                existingDevice.ManagementType = deviceDto.ManagementType != ManagementType.Unknown ? deviceDto.ManagementType : existingDevice.ManagementType;
                existingDevice.AgentInstalled = deviceDto.AgentInstalled;
                existingDevice.LastSeen = TimeZoneHelper.GetUtcNowForStorage();

                // Güncellenen cihazı doğrula
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
                // Yeni cihaz oluştur
                return await CreateNetworkDiscoveredDevice(deviceDto);
            }
        }

        [HttpPut("{id}")]
        [SwaggerOperation(Summary = "Cihaz güncelle", Description = "Mevcut bir cihazı günceller")]
        [SwaggerResponse(204, "Cihaz başarıyla güncellendi")]
        [SwaggerResponse(404, "Cihaz bulunamadı")]
        [SwaggerResponse(400, "Geçersiz cihaz verisi")]
        public async Task<IActionResult> Update(Guid id, Device updatedDevice)
        {
            var device = await _deviceService.GetDeviceByIdAsync(id);
            if (device == null)
                return NotFound();

            // Cihaz özelliklerini güncelle
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
        [SwaggerOperation(Summary = "Cihaz tipini ata", Description = "Ağ keşfi ile bulunan cihaza cihaz tipi atar")]
        [SwaggerResponse(204, "Cihaz tipi başarıyla atandı")]
        [SwaggerResponse(404, "Cihaz bulunamadı")]
        [SwaggerResponse(400, "Geçersiz istek")]
        public async Task<IActionResult> AssignDeviceType(Guid id, [FromBody] DeviceType deviceType)
        {
            var device = await _deviceService.GetDeviceByIdAsync(id);
            if (device == null)
                return NotFound();

            // Sadece ağ keşfi ile bulunan cihazlarda tip atamasına izin ver
            if (device.AgentInstalled)
                return BadRequest(new { error = "Agent kurulu cihazlarda cihaz tipi değiştirilemez." });

            device.DeviceType = deviceType;
            await _deviceService.UpdateDeviceAsync(device);

            return NoContent();
        }

        [HttpPut("{id}/network-discovered")]
        [SwaggerOperation(Summary = "Ağ keşfi ile bulunan cihazı güncelle", Description = "Ağ keşfi ile bulunan bir cihazı günceller")]
        [SwaggerResponse(204, "Cihaz başarıyla güncellendi")]
        [SwaggerResponse(404, "Cihaz bulunamadı")]
        [SwaggerResponse(400, "Geçersiz istek")]
        public async Task<IActionResult> UpdateNetworkDiscoveredDevice(Guid id, [FromBody] NetworkDeviceRegistrationDto updateDto)
        {
            var device = await _deviceService.GetDeviceByIdAsync(id);
            if (device == null)
                return NotFound();

            // Sadece ağ keşfi ile bulunan cihazlarda güncellemeye izin ver
            if (device.AgentInstalled)
                return BadRequest(new { error = "Agent kurulu cihazlar bu endpoint üzerinden güncellenemez." });

            // Sadece sağlanan alanları güncelle
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

            // Güncellenen cihazı doğrula
            var validationErrors = DeviceValidator.ValidateDevice(device);
            if (validationErrors.Any())
            {
                return BadRequest(new { errors = validationErrors });
            }

            await _deviceService.UpdateDeviceAsync(device);
            return NoContent();
        }

        [HttpPost("batch")]
        [SwaggerOperation(Summary = "Toplu cihaz yükleme", Description = "Tek istekte birden fazla cihaz yükler")]
        [SwaggerResponse(200, "Toplu yükleme tamamlandı", typeof(BatchUploadResultDto))]
        [SwaggerResponse(400, "Geçersiz toplu veri")]
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
                    // DTO'yu Device varlığına dönüştür
                    var device = new Device
                    {
                        Id = Guid.NewGuid(),
                        Name = deviceDto.Name,
                        MacAddress = deviceDto.MacAddress,
                        IpAddress = deviceDto.IpAddress,
                        DeviceType = deviceDto.DeviceType,
                        Model = deviceDto.Model,
                        Location = deviceDto.Location,
                        LastSeen = TimeZoneHelper.GetUtcNowForStorage(),
                        ManagementType = ManagementType.Agent,
                        DiscoveryMethod = DiscoveryMethod.Agent,
                        Status = 1, // Aktif
                        ChangeLogs = new List<ChangeLog>()
                    };

                    // Gerekli alanlar için varsayılan değerler ayarla (null ise)
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
                    
                    // Cihazı doğrula
                    var validationErrors = DeviceValidator.ValidateDevice(device);
                    if (validationErrors.Any())
                    {
                        result.FailedUploads++;
                        result.Errors.Add($"Device {device.Name}: {string.Join(", ", validationErrors)}");
                        continue;
                    }

                    // Servis kullanarak cihaz oluştur veya güncelle
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
        [SwaggerOperation(Summary = "Cihaz sil", Description = "Envanterden bir cihazı siler")]
        [SwaggerResponse(204, "Cihaz başarıyla silindi")]
        [SwaggerResponse(404, "Cihaz bulunamadı")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var success = await _deviceService.DeleteDeviceAsync(id);
            if (!success)
                return NotFound();

            return NoContent();
        }

        // Yardımcı metotlar
        private async Task<Device> UpdateExistingDeviceWithChangeLog(Device existingDevice, Device newDevice)
        {
            var changes = new List<ChangeLog>();
            
            // Reload the device from database to ensure we have the latest version
            var deviceToUpdate = await _deviceService.GetDeviceByIdAsync(existingDevice.Id);
            if (deviceToUpdate == null)
            {
                throw new InvalidOperationException("Device not found for update");
            }
            
            // Track changes and update fields
            if (!string.IsNullOrEmpty(newDevice.Name) && deviceToUpdate.Name != newDevice.Name)
            {
                changes.Add(new ChangeLog
                {
                    Id = Guid.NewGuid(),
                    DeviceId = deviceToUpdate.Id,
                    ChangeDate = TimeZoneHelper.GetUtcNowForStorage(),
                    ChangeType = "Name",
                    OldValue = deviceToUpdate.Name ?? "",
                    NewValue = newDevice.Name,
                    ChangedBy = "Agent"
                });
                deviceToUpdate.Name = newDevice.Name;
            }

            if (!string.IsNullOrEmpty(newDevice.IpAddress) && deviceToUpdate.IpAddress != newDevice.IpAddress)
            {
                changes.Add(new ChangeLog
                {
                    Id = Guid.NewGuid(),
                    DeviceId = deviceToUpdate.Id,
                    ChangeDate = TimeZoneHelper.GetUtcNowForStorage(),
                    ChangeType = "IpAddress",
                    OldValue = deviceToUpdate.IpAddress ?? "",
                    NewValue = newDevice.IpAddress,
                    ChangedBy = "Agent"
                });
                deviceToUpdate.IpAddress = newDevice.IpAddress;
            }

            if (!string.IsNullOrEmpty(newDevice.Model) && deviceToUpdate.Model != newDevice.Model)
            {
                changes.Add(new ChangeLog
                {
                    Id = Guid.NewGuid(),
                    DeviceId = deviceToUpdate.Id,
                    ChangeDate = TimeZoneHelper.GetUtcNowForStorage(),
                    ChangeType = "Model",
                    OldValue = deviceToUpdate.Model ?? "",
                    NewValue = newDevice.Model,
                    ChangedBy = "Agent"
                });
                deviceToUpdate.Model = newDevice.Model;
            }

            if (!string.IsNullOrEmpty(newDevice.Location) && deviceToUpdate.Location != newDevice.Location)
            {
                changes.Add(new ChangeLog
                {
                    Id = Guid.NewGuid(),
                    DeviceId = deviceToUpdate.Id,
                    ChangeDate = TimeZoneHelper.GetUtcNowForStorage(),
                    ChangeType = "Location",
                    OldValue = deviceToUpdate.Location ?? "",
                    NewValue = newDevice.Location,
                    ChangedBy = "Agent"
                });
                deviceToUpdate.Location = newDevice.Location;
            }

            if (newDevice.DeviceType != DeviceType.Unknown && deviceToUpdate.DeviceType != newDevice.DeviceType)
            {
                changes.Add(new ChangeLog
                {
                    Id = Guid.NewGuid(),
                    DeviceId = deviceToUpdate.Id,
                    ChangeDate = TimeZoneHelper.GetUtcNowForStorage(),
                    ChangeType = "DeviceType",
                    OldValue = deviceToUpdate.DeviceType.ToString(),
                    NewValue = newDevice.DeviceType.ToString(),
                    ChangedBy = "Agent"
                });
                deviceToUpdate.DeviceType = newDevice.DeviceType;
            }

            if (newDevice.ManagementType != ManagementType.Unknown && deviceToUpdate.ManagementType != newDevice.ManagementType)
            {
                changes.Add(new ChangeLog
                {
                    Id = Guid.NewGuid(),
                    DeviceId = deviceToUpdate.Id,
                    ChangeDate = TimeZoneHelper.GetUtcNowForStorage(),
                    ChangeType = "ManagementType",
                    OldValue = deviceToUpdate.ManagementType.ToString(),
                    NewValue = newDevice.ManagementType.ToString(),
                    ChangedBy = "Agent"
                });
                deviceToUpdate.ManagementType = newDevice.ManagementType;
            }

            if (deviceToUpdate.Status != newDevice.Status)
            {
                changes.Add(new ChangeLog
                {
                    Id = Guid.NewGuid(),
                    DeviceId = deviceToUpdate.Id,
                    ChangeDate = TimeZoneHelper.GetUtcNowForStorage(),
                    ChangeType = "Status",
                    OldValue = deviceToUpdate.Status.ToString(),
                    NewValue = newDevice.Status.ToString(),
                    ChangedBy = "Agent"
                });
                deviceToUpdate.Status = newDevice.Status;
            }

            // HardwareInfo güncelleme - sadece önemli değişiklikleri logla
            if (newDevice.HardwareInfo != null)
            {
                if (deviceToUpdate.HardwareInfo == null)
                {
                    changes.Add(new ChangeLog
                    {
                        Id = Guid.NewGuid(),
                        DeviceId = deviceToUpdate.Id,
                        ChangeDate = TimeZoneHelper.GetUtcNowForStorage(),
                        ChangeType = "HardwareInfo",
                        OldValue = "None",
                        NewValue = "Hardware information added",
                        ChangedBy = "Agent"
                    });
                    deviceToUpdate.HardwareInfo = newDevice.HardwareInfo;
                }
                else
                {
                    // CPU değişikliği kontrolü
                    if (!string.IsNullOrEmpty(newDevice.HardwareInfo.Cpu) && 
                        deviceToUpdate.HardwareInfo.Cpu != newDevice.HardwareInfo.Cpu)
                    {
                        changes.Add(new ChangeLog
                        {
                            Id = Guid.NewGuid(),
                            DeviceId = deviceToUpdate.Id,
                            ChangeDate = TimeZoneHelper.GetUtcNowForStorage(),
                            ChangeType = "CPU",
                            OldValue = deviceToUpdate.HardwareInfo.Cpu ?? "",
                            NewValue = newDevice.HardwareInfo.Cpu,
                            ChangedBy = "Agent"
                        });
                    }

                    // RAM değişikliği kontrolü
                    if (newDevice.HardwareInfo.RamGB != deviceToUpdate.HardwareInfo.RamGB)
                    {
                        changes.Add(new ChangeLog
                        {
                            Id = Guid.NewGuid(),
                            DeviceId = deviceToUpdate.Id,
                            ChangeDate = TimeZoneHelper.GetUtcNowForStorage(),
                            ChangeType = "RAM",
                            OldValue = $"{deviceToUpdate.HardwareInfo.RamGB} GB",
                            NewValue = $"{newDevice.HardwareInfo.RamGB} GB",
                            ChangedBy = "Agent"
                        });
                    }

                    // Update hardware info without complex nested object updates for now
                    deviceToUpdate.HardwareInfo.Cpu = newDevice.HardwareInfo.Cpu ?? deviceToUpdate.HardwareInfo.Cpu;
                    deviceToUpdate.HardwareInfo.CpuCores = newDevice.HardwareInfo.CpuCores > 0 ? newDevice.HardwareInfo.CpuCores : deviceToUpdate.HardwareInfo.CpuCores;
                    deviceToUpdate.HardwareInfo.CpuLogical = newDevice.HardwareInfo.CpuLogical > 0 ? newDevice.HardwareInfo.CpuLogical : deviceToUpdate.HardwareInfo.CpuLogical;
                    deviceToUpdate.HardwareInfo.CpuClockMHz = newDevice.HardwareInfo.CpuClockMHz > 0 ? newDevice.HardwareInfo.CpuClockMHz : deviceToUpdate.HardwareInfo.CpuClockMHz;
                    deviceToUpdate.HardwareInfo.Motherboard = newDevice.HardwareInfo.Motherboard ?? deviceToUpdate.HardwareInfo.Motherboard;
                    deviceToUpdate.HardwareInfo.MotherboardSerial = newDevice.HardwareInfo.MotherboardSerial ?? deviceToUpdate.HardwareInfo.MotherboardSerial;
                    deviceToUpdate.HardwareInfo.BiosManufacturer = newDevice.HardwareInfo.BiosManufacturer ?? deviceToUpdate.HardwareInfo.BiosManufacturer;
                    deviceToUpdate.HardwareInfo.BiosVersion = newDevice.HardwareInfo.BiosVersion ?? deviceToUpdate.HardwareInfo.BiosVersion;
                    deviceToUpdate.HardwareInfo.BiosSerial = newDevice.HardwareInfo.BiosSerial ?? deviceToUpdate.HardwareInfo.BiosSerial;
                    deviceToUpdate.HardwareInfo.RamGB = newDevice.HardwareInfo.RamGB > 0 ? newDevice.HardwareInfo.RamGB : deviceToUpdate.HardwareInfo.RamGB;
                    deviceToUpdate.HardwareInfo.DiskGB = newDevice.HardwareInfo.DiskGB > 0 ? newDevice.HardwareInfo.DiskGB : deviceToUpdate.HardwareInfo.DiskGB;
                }
            }

            // SoftwareInfo güncelleme - sadece önemli değişiklikleri logla
            if (newDevice.SoftwareInfo != null)
            {
                if (deviceToUpdate.SoftwareInfo == null)
                {
                    changes.Add(new ChangeLog
                    {
                        Id = Guid.NewGuid(),
                        DeviceId = deviceToUpdate.Id,
                        ChangeDate = TimeZoneHelper.GetUtcNowForStorage(),
                        ChangeType = "SoftwareInfo",
                        OldValue = "None",
                        NewValue = "Software information added",
                        ChangedBy = "Agent"
                    });
                    deviceToUpdate.SoftwareInfo = newDevice.SoftwareInfo;
                }
                else
                {
                    // OS değişikliği kontrolü
                    if (!string.IsNullOrEmpty(newDevice.SoftwareInfo.OperatingSystem) && 
                        deviceToUpdate.SoftwareInfo.OperatingSystem != newDevice.SoftwareInfo.OperatingSystem)
                    {
                        changes.Add(new ChangeLog
                        {
                            Id = Guid.NewGuid(),
                            DeviceId = deviceToUpdate.Id,
                            ChangeDate = TimeZoneHelper.GetUtcNowForStorage(),
                            ChangeType = "OperatingSystem",
                            OldValue = deviceToUpdate.SoftwareInfo.OperatingSystem ?? "",
                            NewValue = newDevice.SoftwareInfo.OperatingSystem,
                            ChangedBy = "Agent"
                        });
                    }

                    // OS Version değişikliği kontrolü
                    if (!string.IsNullOrEmpty(newDevice.SoftwareInfo.OsVersion) && 
                        deviceToUpdate.SoftwareInfo.OsVersion != newDevice.SoftwareInfo.OsVersion)
                    {
                        changes.Add(new ChangeLog
                        {
                            Id = Guid.NewGuid(),
                            DeviceId = deviceToUpdate.Id,
                            ChangeDate = TimeZoneHelper.GetUtcNowForStorage(),
                            ChangeType = "OSVersion",
                            OldValue = deviceToUpdate.SoftwareInfo.OsVersion ?? "",
                            NewValue = newDevice.SoftwareInfo.OsVersion,
                            ChangedBy = "Agent"
                        });
                    }

                    // Update software info
                    deviceToUpdate.SoftwareInfo.OperatingSystem = newDevice.SoftwareInfo.OperatingSystem ?? deviceToUpdate.SoftwareInfo.OperatingSystem;
                    deviceToUpdate.SoftwareInfo.OsVersion = newDevice.SoftwareInfo.OsVersion ?? deviceToUpdate.SoftwareInfo.OsVersion;
                    deviceToUpdate.SoftwareInfo.OsArchitecture = newDevice.SoftwareInfo.OsArchitecture ?? deviceToUpdate.SoftwareInfo.OsArchitecture;
                    deviceToUpdate.SoftwareInfo.RegisteredUser = newDevice.SoftwareInfo.RegisteredUser ?? deviceToUpdate.SoftwareInfo.RegisteredUser;
                    deviceToUpdate.SoftwareInfo.SerialNumber = newDevice.SoftwareInfo.SerialNumber ?? deviceToUpdate.SoftwareInfo.SerialNumber;
                    deviceToUpdate.SoftwareInfo.ActiveUser = newDevice.SoftwareInfo.ActiveUser ?? deviceToUpdate.SoftwareInfo.ActiveUser;
                }
            }

            // Agent bilgileri güncelle
            deviceToUpdate.AgentInstalled = newDevice.AgentInstalled;
            if (newDevice.DiscoveryMethod != DiscoveryMethod.Unknown)
                deviceToUpdate.DiscoveryMethod = newDevice.DiscoveryMethod;
            
            // LastSeen güncelle
            deviceToUpdate.LastSeen = TimeZoneHelper.GetUtcNowForStorage();

            // Yeni change log'ları ekle
            foreach (var changeLog in changes)
            {
                deviceToUpdate.ChangeLogs.Add(changeLog);
            }

            // Cihazı güncelle - use the DeviceService's more robust update method
            try
            {
                var updatedDevice = await _deviceService.UpdateDeviceAsync(deviceToUpdate);
                
                if (changes.Any())
                {
                    _logger.LogInformation("Device updated with {ChangeCount} changes: {DeviceName} ({MacAddress})", 
                        changes.Count, deviceToUpdate.Name, deviceToUpdate.MacAddress);
                }
                else
                {
                    _logger.LogInformation("Device seen but no changes detected: {DeviceName} ({MacAddress})", 
                        deviceToUpdate.Name, deviceToUpdate.MacAddress);
                }
                
                return updatedDevice;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating device: {DeviceName} ({MacAddress})", 
                    deviceToUpdate.Name, deviceToUpdate.MacAddress);
                throw;
            }
        }

        private async Task<ActionResult<Device>> UpdateExistingNetworkDevice(Device existingDevice, Device newDevice)
        {
            // Mevcut cihazı yeni bilgilerle güncelle
            existingDevice.Name = newDevice.Name ?? existingDevice.Name;
            existingDevice.DeviceType = newDevice.DeviceType != DeviceType.Unknown ? newDevice.DeviceType : existingDevice.DeviceType;
            existingDevice.Model = newDevice.Model ?? existingDevice.Model;
            existingDevice.Location = newDevice.Location ?? existingDevice.Location;
            existingDevice.ManagementType = newDevice.ManagementType != ManagementType.Unknown ? newDevice.ManagementType : existingDevice.ManagementType;
            existingDevice.AgentInstalled = newDevice.AgentInstalled;

            // Güncellenen cihazı doğrula
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
        
        #endregion
    }
}