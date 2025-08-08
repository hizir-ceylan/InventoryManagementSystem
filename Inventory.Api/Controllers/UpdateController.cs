using Microsoft.AspNetCore.Mvc;
using Inventory.Domain.Entities;
using Inventory.Api.Services;
using Swashbuckle.AspNetCore.Annotations;

namespace Inventory.Api.Controllers
{
    /// <summary>
    /// Sistem Güncellemeleri Controller'ı
    /// Windows ve Office güncellemelerini yönetir (sadece raporlama, otomatik yükleme yapmaz)
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [SwaggerTag("System updates management - Windows and Office update detection and reporting")]
    public class UpdateController : ControllerBase
    {
        #region Fields ve Constructor
        
        private readonly IUpdateService _updateService;
        private readonly ILogger<UpdateController> _logger;

        public UpdateController(IUpdateService updateService, ILogger<UpdateController> logger)
        {
            _updateService = updateService;
            _logger = logger;
        }
        
        #endregion

        #region GET - Güncelleme Sorgulama İşlemleri

        /// <summary>
        /// Tüm sistem güncellemelerini getirir
        /// </summary>
        [HttpGet]
        [SwaggerOperation(Summary = "Tüm güncellemeleri getir", Description = "Sistemdeki tüm cihazlar için tespit edilen güncellemeleri döndürür")]
        [SwaggerResponse(200, "Güncelleme listesini döndürür", typeof(IEnumerable<SystemUpdate>))]
        public async Task<ActionResult<IEnumerable<SystemUpdate>>> GetAllUpdates(
            [FromQuery] UpdateStatus? status = null,
            [FromQuery] UpdatePriority? priority = null,
            [FromQuery] string? updateType = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            try
            {
                var updates = await _updateService.GetAllUpdatesAsync(status, priority, updateType, page, pageSize);
                return Ok(updates);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Güncellemeler getirilemedi");
                return StatusCode(500, new { error = "Güncellemeler getirilemedi", details = ex.Message });
            }
        }

        /// <summary>
        /// Belirli cihaza ait güncellemeleri getirir
        /// </summary>
        [HttpGet("device/{deviceId}")]
        [SwaggerOperation(Summary = "Cihaza ait güncellemeleri getir", Description = "Belirli bir cihaz için tespit edilen güncellemeleri döndürür")]
        [SwaggerResponse(200, "Cihaza ait güncelleme listesini döndürür", typeof(IEnumerable<SystemUpdate>))]
        [SwaggerResponse(404, "Cihaz bulunamadı")]
        public async Task<ActionResult<IEnumerable<SystemUpdate>>> GetUpdatesByDevice(Guid deviceId)
        {
            try
            {
                var updates = await _updateService.GetUpdatesByDeviceAsync(deviceId);
                return Ok(updates);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Cihaz güncellemeleri getirilemedi: {DeviceId}", deviceId);
                return StatusCode(500, new { error = "Cihaz güncellemeleri getirilemedi", details = ex.Message });
            }
        }

        /// <summary>
        /// Mevcut güncellemeleri getirir (henüz yüklenmemiş)
        /// </summary>
        [HttpGet("available")]
        [SwaggerOperation(Summary = "Mevcut güncellemeleri getir", Description = "Henüz yüklenmemiş güncellemeleri döndürür")]
        [SwaggerResponse(200, "Mevcut güncelleme listesini döndürür")]
        public async Task<ActionResult<IEnumerable<SystemUpdate>>> GetAvailableUpdates()
        {
            try
            {
                var updates = await _updateService.GetAvailableUpdatesAsync();
                return Ok(updates);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Mevcut güncellemeler getirilemedi");
                return StatusCode(500, new { error = "Mevcut güncellemeler getirilemedi", details = ex.Message });
            }
        }

        /// <summary>
        /// Kritik ve güvenlik güncellemelerini getirir
        /// </summary>
        [HttpGet("critical")]
        [SwaggerOperation(Summary = "Kritik güncellemeleri getir", Description = "Kritik ve güvenlik güncellemelerini döndürür")]
        [SwaggerResponse(200, "Kritik güncelleme listesini döndürür")]
        public async Task<ActionResult<IEnumerable<SystemUpdate>>> GetCriticalUpdates()
        {
            try
            {
                var updates = await _updateService.GetCriticalUpdatesAsync();
                return Ok(updates);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kritik güncellemeler getirilemedi");
                return StatusCode(500, new { error = "Kritik güncellemeler getirilemedi", details = ex.Message });
            }
        }

        /// <summary>
        /// Güncelleme istatistiklerini getirir
        /// </summary>
        [HttpGet("statistics")]
        [SwaggerOperation(Summary = "Güncelleme istatistikleri", Description = "Güncelleme durumu ve sayıları hakkında istatistik döndürür")]
        [SwaggerResponse(200, "Güncelleme istatistiklerini döndürür")]
        public async Task<ActionResult<object>> GetUpdateStatistics()
        {
            try
            {
                var statistics = await _updateService.GetUpdateStatisticsAsync();
                return Ok(statistics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Güncelleme istatistikleri getirilemedi");
                return StatusCode(500, new { error = "Güncelleme istatistikleri getirilemedi", details = ex.Message });
            }
        }

        /// <summary>
        /// Güncelleme türlerine göre gruplandırılmış veri getirir
        /// </summary>
        [HttpGet("by-type")]
        [SwaggerOperation(Summary = "Türe göre güncellemeler", Description = "Güncelleme türlerine göre gruplandırılmış verileri döndürür")]
        [SwaggerResponse(200, "Türe göre güncelleme bilgilerini döndürür")]
        public async Task<ActionResult<object>> GetUpdatesByType()
        {
            try
            {
                var updatesByType = await _updateService.GetUpdatesByTypeAsync();
                return Ok(updatesByType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Türe göre güncellemeler getirilemedi");
                return StatusCode(500, new { error = "Türe göre güncellemeler getirilemedi", details = ex.Message });
            }
        }

        #endregion

        #region POST - Güncelleme Kaydetme İşlemleri

        /// <summary>
        /// Agent'dan güncelleme verisi kaydeder
        /// </summary>
        [HttpPost("report")]
        [SwaggerOperation(Summary = "Güncelleme raporu kaydet", Description = "Agent'dan gelen güncelleme verilerini kaydeder")]
        [SwaggerResponse(201, "Güncelleme raporu başarıyla kaydedildi")]
        [SwaggerResponse(400, "Geçersiz güncelleme verisi")]
        public async Task<ActionResult> ReportUpdates([FromBody] List<SystemUpdate> updates)
        {
            try
            {
                if (updates == null || !updates.Any())
                {
                    return BadRequest(new { error = "Güncelleme verisi boş olamaz" });
                }

                var savedCount = await _updateService.SaveUpdatesAsync(updates);
                
                _logger.LogInformation("{SavedCount} güncelleme kaydedildi", savedCount);
                
                return CreatedAtAction(nameof(GetAllUpdates), new { }, new 
                { 
                    message = "Güncelleme raporu başarıyla kaydedildi",
                    savedCount = savedCount
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Güncelleme raporu kaydedilemedi");
                return StatusCode(500, new { error = "Güncelleme raporu kaydedilemedi", details = ex.Message });
            }
        }

        /// <summary>
        /// Belirli cihaz için güncelleme taraması başlatır
        /// </summary>
        [HttpPost("scan/{deviceId}")]
        [SwaggerOperation(Summary = "Güncelleme taraması başlat", Description = "Belirli bir cihaz için güncelleme taraması başlatır")]
        [SwaggerResponse(202, "Güncelleme taraması başlatıldı")]
        [SwaggerResponse(404, "Cihaz bulunamadı")]
        public async Task<ActionResult> StartUpdateScan(Guid deviceId)
        {
            try
            {
                var scanId = await _updateService.StartUpdateScanAsync(deviceId);
                
                _logger.LogInformation("Cihaz {DeviceId} için güncelleme taraması başlatıldı: {ScanId}", deviceId, scanId);
                
                return Accepted(new 
                { 
                    message = "Güncelleme taraması başlatıldı",
                    scanId = scanId,
                    deviceId = deviceId
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Güncelleme taraması başlatılamadı: {DeviceId}", deviceId);
                return StatusCode(500, new { error = "Güncelleme taraması başlatılamadı", details = ex.Message });
            }
        }

        #endregion

        #region PUT - Güncelleme Durumu Değiştirme

        /// <summary>
        /// Güncelleme durumunu değiştirir
        /// </summary>
        [HttpPut("{updateId}/status")]
        [SwaggerOperation(Summary = "Güncelleme durumu değiştir", Description = "Belirli bir güncellemenin durumunu değiştirir")]
        [SwaggerResponse(200, "Güncelleme durumu başarıyla değiştirildi")]
        [SwaggerResponse(404, "Güncelleme bulunamadı")]
        public async Task<ActionResult> UpdateStatus(Guid updateId, [FromBody] UpdateStatusChangeRequest request)
        {
            try
            {
                var success = await _updateService.UpdateStatusAsync(updateId, request.Status, request.Reason);
                
                if (!success)
                {
                    return NotFound(new { error = "Güncelleme bulunamadı" });
                }

                _logger.LogInformation("Güncelleme {UpdateId} durumu {Status} olarak değiştirildi", updateId, request.Status);
                
                return Ok(new { message = "Güncelleme durumu başarıyla değiştirildi" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Güncelleme durumu değiştirilemedi: {UpdateId}", updateId);
                return StatusCode(500, new { error = "Güncelleme durumu değiştirilemedi", details = ex.Message });
            }
        }

        /// <summary>
        /// Toplu güncelleme durumu değiştirme
        /// </summary>
        [HttpPut("bulk-status")]
        [SwaggerOperation(Summary = "Toplu durum değiştir", Description = "Birden fazla güncellemenin durumunu aynı anda değiştirir")]
        [SwaggerResponse(200, "Toplu durum değişikliği başarıyla tamamlandı")]
        public async Task<ActionResult> BulkUpdateStatus([FromBody] BulkStatusChangeRequest request)
        {
            try
            {
                var updatedCount = await _updateService.BulkUpdateStatusAsync(request.UpdateIds, request.Status, request.Reason);
                
                _logger.LogInformation("{UpdatedCount} güncelleme durumu {Status} olarak değiştirildi", updatedCount, request.Status);
                
                return Ok(new 
                { 
                    message = "Toplu durum değişikliği başarıyla tamamlandı",
                    updatedCount = updatedCount
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Toplu durum değişikliği başarısız");
                return StatusCode(500, new { error = "Toplu durum değişikliği başarısız", details = ex.Message });
            }
        }

        #endregion

        #region DELETE - Güncelleme Silme İşlemleri

        /// <summary>
        /// Eski güncelleme kayıtlarını temizler
        /// </summary>
        [HttpDelete("cleanup")]
        [SwaggerOperation(Summary = "Eski kayıtları temizle", Description = "Belirli tarihten eski güncelleme kayıtlarını siler")]
        [SwaggerResponse(200, "Eski kayıtlar başarıyla temizlendi")]
        public async Task<ActionResult> CleanupOldRecords([FromQuery] int daysOld = 90)
        {
            try
            {
                var deletedCount = await _updateService.CleanupOldRecordsAsync(daysOld);
                
                _logger.LogInformation("{DeletedCount} eski güncelleme kaydı temizlendi", deletedCount);
                
                return Ok(new 
                { 
                    message = "Eski kayıtlar başarıyla temizlendi",
                    deletedCount = deletedCount,
                    daysOld = daysOld
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Eski kayıtlar temizlenemedi");
                return StatusCode(500, new { error = "Eski kayıtlar temizlenemedi", details = ex.Message });
            }
        }

        #endregion

        #region Helper Classes

        /// <summary>
        /// Güncelleme durumu değiştirme isteği
        /// </summary>
        public class UpdateStatusChangeRequest
        {
            public UpdateStatus Status { get; set; }
            public string? Reason { get; set; }
        }

        /// <summary>
        /// Toplu durum değiştirme isteği
        /// </summary>
        public class BulkStatusChangeRequest
        {
            public List<Guid> UpdateIds { get; set; } = new();
            public UpdateStatus Status { get; set; }
            public string? Reason { get; set; }
        }

        #endregion
    }
}

/// <summary>
/// Güncelleme servisi interface'i
/// </summary>
public interface IUpdateService
{
    Task<IEnumerable<SystemUpdate>> GetAllUpdatesAsync(UpdateStatus? status, UpdatePriority? priority, string? updateType, int page, int pageSize);
    Task<IEnumerable<SystemUpdate>> GetUpdatesByDeviceAsync(Guid deviceId);
    Task<IEnumerable<SystemUpdate>> GetAvailableUpdatesAsync();
    Task<IEnumerable<SystemUpdate>> GetCriticalUpdatesAsync();
    Task<object> GetUpdateStatisticsAsync();
    Task<object> GetUpdatesByTypeAsync();
    Task<int> SaveUpdatesAsync(List<SystemUpdate> updates);
    Task<Guid> StartUpdateScanAsync(Guid deviceId);
    Task<bool> UpdateStatusAsync(Guid updateId, UpdateStatus status, string? reason);
    Task<int> BulkUpdateStatusAsync(List<Guid> updateIds, UpdateStatus status, string? reason);
    Task<int> CleanupOldRecordsAsync(int daysOld);
}