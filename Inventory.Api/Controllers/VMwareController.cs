using Microsoft.AspNetCore.Mvc;
using Inventory.Domain.Entities;
using Inventory.Api.Services;
using Swashbuckle.AspNetCore.Annotations;

namespace Inventory.Api.Controllers
{
    /// <summary>
    /// VMware Yönetimi Controller'ı
    /// VMware vSphere entegrasyonu ve sanal makine yönetimi
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [SwaggerTag("VMware management operations - vSphere integration and virtual machine management")]
    public class VMwareController : ControllerBase
    {
        #region Fields ve Constructor
        
        private readonly IVMwareService _vmwareService;
        private readonly ILogger<VMwareController> _logger;

        public VMwareController(IVMwareService vmwareService, ILogger<VMwareController> logger)
        {
            _vmwareService = vmwareService;
            _logger = logger;
        }
        
        #endregion

        #region VMware Server Configuration

        /// <summary>
        /// VMware sunucu durumunu getirir
        /// </summary>
        [HttpGet("status")]
        [SwaggerOperation(Summary = "VMware sunucu durumunu getir", Description = "VMware sunucu bağlantı durumu ve metrikleri")]
        [SwaggerResponse(200, "VMware sunucu durumu", typeof(VMwareStatusDto))]
        public async Task<ActionResult<VMwareStatusDto>> GetStatus()
        {
            try
            {
                var status = await _vmwareService.GetServerStatusAsync();
                return Ok(status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting VMware server status");
                return StatusCode(500, new { error = "VMware sunucu durumu alınamadı", details = ex.Message });
            }
        }

        /// <summary>
        /// VMware sunucu konfigürasyonunu günceller
        /// </summary>
        [HttpPut("configuration")]
        [SwaggerOperation(Summary = "VMware konfigürasyonunu güncelle", Description = "VMware sunucu bağlantı ayarlarını günceller")]
        [SwaggerResponse(200, "Konfigürasyon güncellendi")]
        [SwaggerResponse(400, "Geçersiz konfigürasyon")]
        public async Task<ActionResult> UpdateConfiguration([FromBody] VMwareConfigurationDto config)
        {
            try
            {
                if (string.IsNullOrEmpty(config.ServerAddress))
                {
                    return BadRequest(new { error = "Sunucu adresi gereklidir" });
                }

                await _vmwareService.UpdateConfigurationAsync(config);
                return Ok(new { message = "VMware konfigürasyonu güncellendi" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating VMware configuration");
                return StatusCode(500, new { error = "Konfigürasyon güncellenemedi", details = ex.Message });
            }
        }

        /// <summary>
        /// VMware sunucusuna bağlantıyı test eder
        /// </summary>
        [HttpPost("test-connection")]
        [SwaggerOperation(Summary = "Bağlantıyı test et", Description = "VMware sunucusuna bağlantıyı test eder")]
        [SwaggerResponse(200, "Bağlantı başarılı")]
        [SwaggerResponse(400, "Bağlantı başarısız")]
        public async Task<ActionResult> TestConnection()
        {
            try
            {
                var result = await _vmwareService.TestConnectionAsync();
                
                if (result.Success)
                {
                    return Ok(new { 
                        success = true, 
                        message = "VMware sunucusuna başarıyla bağlanıldı",
                        serverInfo = result.ServerInfo
                    });
                }
                else
                {
                    return BadRequest(new { 
                        success = false, 
                        error = "Bağlantı başarısız", 
                        details = result.Error 
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing VMware connection");
                return StatusCode(500, new { 
                    success = false, 
                    error = "Bağlantı testi sırasında hata oluştu", 
                    details = ex.Message 
                });
            }
        }

        #endregion

        #region Virtual Machines

        /// <summary>
        /// Tüm sanal makineleri getirir
        /// </summary>
        [HttpGet("virtual-machines")]
        [SwaggerOperation(Summary = "Sanal makineleri getir", Description = "VMware ortamındaki tüm sanal makineleri döndürür")]
        [SwaggerResponse(200, "Sanal makine listesi", typeof(IEnumerable<VirtualMachineDto>))]
        public async Task<ActionResult<IEnumerable<VirtualMachineDto>>> GetVirtualMachines()
        {
            try
            {
                var vms = await _vmwareService.GetVirtualMachinesAsync();
                return Ok(vms);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting virtual machines");
                return StatusCode(500, new { error = "Sanal makineler getirilemedi", details = ex.Message });
            }
        }

        /// <summary>
        /// Belirli bir sanal makineyi getirir
        /// </summary>
        [HttpGet("virtual-machines/{id}")]
        [SwaggerOperation(Summary = "Sanal makine detayını getir", Description = "Belirli bir sanal makinenin detaylarını döndürür")]
        [SwaggerResponse(200, "Sanal makine detayları", typeof(VirtualMachineDto))]
        [SwaggerResponse(404, "Sanal makine bulunamadı")]
        public async Task<ActionResult<VirtualMachineDto>> GetVirtualMachine(Guid id)
        {
            try
            {
                var vm = await _vmwareService.GetVirtualMachineAsync(id);
                
                if (vm == null)
                {
                    return NotFound(new { error = "Sanal makine bulunamadı" });
                }

                return Ok(vm);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting virtual machine {Id}", id);
                return StatusCode(500, new { error = "Sanal makine getirilemedi", details = ex.Message });
            }
        }

        /// <summary>
        /// Sanal makineleri VMware'den senkronize eder
        /// </summary>
        [HttpPost("sync")]
        [SwaggerOperation(Summary = "Sanal makineleri senkronize et", Description = "VMware ortamından sanal makineleri senkronize eder")]
        [SwaggerResponse(200, "Senkronizasyon tamamlandı")]
        [SwaggerResponse(400, "Senkronizasyon başarısız")]
        public async Task<ActionResult> SyncVirtualMachines()
        {
            try
            {
                var result = await _vmwareService.SyncVirtualMachinesAsync();
                
                return Ok(new {
                    success = true,
                    message = "Senkronizasyon tamamlandı",
                    syncedCount = result.SyncedCount,
                    createdCount = result.CreatedCount,
                    updatedCount = result.UpdatedCount,
                    errorCount = result.ErrorCount,
                    duration = result.Duration
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing virtual machines");
                return StatusCode(500, new { 
                    success = false, 
                    error = "Senkronizasyon sırasında hata oluştu", 
                    details = ex.Message 
                });
            }
        }

        /// <summary>
        /// Sanal makineyi envantere ekler
        /// </summary>
        [HttpPost("virtual-machines/{vmwareId}/add-to-inventory")]
        [SwaggerOperation(Summary = "Sanal makineyi envantere ekle", Description = "VMware sanal makinesini envanter sistemine ekler")]
        [SwaggerResponse(200, "Sanal makine envantere eklendi")]
        [SwaggerResponse(400, "Ekleme başarısız")]
        public async Task<ActionResult> AddVirtualMachineToInventory(string vmwareId)
        {
            try
            {
                var result = await _vmwareService.AddVirtualMachineToInventoryAsync(vmwareId);
                
                if (result.Success)
                {
                    return Ok(new {
                        success = true,
                        message = "Sanal makine envantere eklendi",
                        deviceId = result.DeviceId
                    });
                }
                else
                {
                    return BadRequest(new {
                        success = false,
                        error = result.Error
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding virtual machine to inventory");
                return StatusCode(500, new { 
                    success = false, 
                    error = "Sanal makine envantere eklenemedi", 
                    details = ex.Message 
                });
            }
        }

        #endregion

        #region Sync History and Logs

        /// <summary>
        /// VMware senkronizasyon geçmişini getirir
        /// </summary>
        [HttpGet("sync-history")]
        [SwaggerOperation(Summary = "Senkronizasyon geçmişini getir", Description = "VMware senkronizasyon loglarını döndürür")]
        [SwaggerResponse(200, "Senkronizasyon geçmişi", typeof(IEnumerable<VMwareSyncLogDto>))]
        public async Task<ActionResult<IEnumerable<VMwareSyncLogDto>>> GetSyncHistory(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            try
            {
                var history = await _vmwareService.GetSyncHistoryAsync(startDate, endDate, page, pageSize);
                return Ok(history);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting sync history");
                return StatusCode(500, new { error = "Senkronizasyon geçmişi getirilemedi", details = ex.Message });
            }
        }

        /// <summary>
        /// Belirli bir senkronizasyon log detayını getirir
        /// </summary>
        [HttpGet("sync-history/{id}")]
        [SwaggerOperation(Summary = "Senkronizasyon log detayını getir", Description = "Belirli bir senkronizasyon logunundetaylarını döndürür")]
        [SwaggerResponse(200, "Senkronizasyon log detayları", typeof(VMwareSyncLogDto))]
        [SwaggerResponse(404, "Log bulunamadı")]
        public async Task<ActionResult<VMwareSyncLogDto>> GetSyncLogDetails(Guid id)
        {
            try
            {
                var log = await _vmwareService.GetSyncLogDetailsAsync(id);
                
                if (log == null)
                {
                    return NotFound(new { error = "Senkronizasyon logu bulunamadı" });
                }

                return Ok(log);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting sync log details {Id}", id);
                return StatusCode(500, new { error = "Senkronizasyon log detayları getirilemedi", details = ex.Message });
            }
        }

        #endregion
    }

    #region DTOs

    public class VMwareStatusDto
    {
        public bool Connected { get; set; }
        public string? ServerAddress { get; set; }
        public DateTime? LastConnection { get; set; }
        public string? Error { get; set; }
        public VMwareMetricsDto? Metrics { get; set; }
        public DateTime? LastSync { get; set; }
    }

    public class VMwareMetricsDto
    {
        public long? TotalStorageGB { get; set; }
        public long? UsedStorageGB { get; set; }
        public long? FreeStorageGB { get; set; }
        public double? CpuUsagePercent { get; set; }
        public double? MemoryUsagePercent { get; set; }
        public int? ActiveVMs { get; set; }
        public int? TotalVMs { get; set; }
    }

    public class VMwareConfigurationDto
    {
        public string ServerAddress { get; set; } = "10.0.0.10";
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
        public int SyncInterval { get; set; } = 30;
    }

    public class VirtualMachineDto
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public string? IpAddress { get; set; }
        public string? PowerState { get; set; }
        public string? GuestOS { get; set; }
        public int? CpuCount { get; set; }
        public long? MemoryGB { get; set; }
        public long? DiskGB { get; set; }
        public string? HostName { get; set; }
        public bool InInventory { get; set; }
        public Guid? DeviceId { get; set; }
    }

    public class VMwareSyncLogDto
    {
        public Guid Id { get; set; }
        public DateTime SyncStartTime { get; set; }
        public DateTime? SyncEndTime { get; set; }
        public string Status { get; set; } = "";
        public string? ErrorMessage { get; set; }
        public int VirtualMachinesFound { get; set; }
        public int VirtualMachinesCreated { get; set; }
        public int VirtualMachinesUpdated { get; set; }
        public TimeSpan? Duration { get; set; }
    }

    #endregion
}