using Microsoft.AspNetCore.Mvc;
using Inventory.Api.Services;
using Inventory.Domain.Entities;
using Swashbuckle.AspNetCore.Annotations;

namespace Inventory.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [SwaggerTag("Predefined network range management - save and manage frequently scanned networks")]
    public class PredefinedNetworkRangeController : ControllerBase
    {
        private readonly IPredefinedNetworkRangeService _predefinedNetworkRangeService;
        private readonly INetworkScanService _networkScanService;
        private readonly ILogger<PredefinedNetworkRangeController> _logger;

        public PredefinedNetworkRangeController(
            IPredefinedNetworkRangeService predefinedNetworkRangeService,
            INetworkScanService networkScanService,
            ILogger<PredefinedNetworkRangeController> logger)
        {
            _predefinedNetworkRangeService = predefinedNetworkRangeService;
            _networkScanService = networkScanService;
            _logger = logger;
        }

        [HttpGet]
        [SwaggerOperation(Summary = "Tüm öntanımlı ağ aralıklarını getir", Description = "Kayıtlı tüm öntanımlı ağ aralıklarını listeler")]
        [SwaggerResponse(200, "Öntanımlı ağ aralıkları başarıyla getirildi")]
        public async Task<IActionResult> GetAll()
        {
            var ranges = await _predefinedNetworkRangeService.GetAllAsync();
            return Ok(ranges);
        }

        [HttpGet("active")]
        [SwaggerOperation(Summary = "Aktif öntanımlı ağ aralıklarını getir", Description = "Sadece aktif öntanımlı ağ aralıklarını listeler")]
        [SwaggerResponse(200, "Aktif öntanımlı ağ aralıkları başarıyla getirildi")]
        public async Task<IActionResult> GetActive()
        {
            var ranges = await _predefinedNetworkRangeService.GetActiveAsync();
            return Ok(ranges);
        }

        [HttpGet("{id}")]
        [SwaggerOperation(Summary = "Öntanımlı ağ aralığını getir", Description = "Belirli bir öntanımlı ağ aralığının detaylarını getirir")]
        [SwaggerResponse(200, "Öntanımlı ağ aralığı başarıyla getirildi")]
        [SwaggerResponse(404, "Öntanımlı ağ aralığı bulunamadı")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var range = await _predefinedNetworkRangeService.GetByIdAsync(id);
            if (range == null)
                return NotFound(new { error = "Öntanımlı ağ aralığı bulunamadı" });

            return Ok(range);
        }

        [HttpPost]
        [SwaggerOperation(Summary = "Yeni öntanımlı ağ aralığı ekle", Description = "Sık kullanılan ağ aralığını kaydetmek için yeni öntanımlı ağ aralığı oluşturur")]
        [SwaggerResponse(201, "Öntanımlı ağ aralığı başarıyla oluşturuldu")]
        [SwaggerResponse(400, "Geçersiz veri")]
        [SwaggerResponse(409, "Bu ağ aralığı zaten kayıtlı")]
        public async Task<IActionResult> Create([FromBody] CreatePredefinedNetworkRangeDto dto)
        {
            try
            {
                // Check if network range already exists
                var existing = await _predefinedNetworkRangeService.GetByNetworkRangeAsync(dto.NetworkRange);
                if (existing != null)
                {
                    return Conflict(new { error = "Bu ağ aralığı zaten kayıtlı" });
                }

                var predefinedRange = new PredefinedNetworkRange
                {
                    Name = dto.Name,
                    NetworkRange = dto.NetworkRange,
                    Description = dto.Description,
                    TimeoutSeconds = dto.TimeoutSeconds,
                    PortScanType = dto.PortScanType,
                    IsActive = true
                };

                var createdRange = await _predefinedNetworkRangeService.CreateAsync(predefinedRange);
                return CreatedAtAction(nameof(GetById), new { id = createdRange.Id }, createdRange);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating predefined network range");
                return BadRequest(new { error = $"Öntanımlı ağ aralığı oluşturulamadı: {ex.Message}" });
            }
        }

        [HttpPut("{id}")]
        [SwaggerOperation(Summary = "Öntanımlı ağ aralığını güncelle", Description = "Mevcut öntanımlı ağ aralığının bilgilerini günceller")]
        [SwaggerResponse(200, "Öntanımlı ağ aralığı başarıyla güncellendi")]
        [SwaggerResponse(404, "Öntanımlı ağ aralığı bulunamadı")]
        [SwaggerResponse(400, "Geçersiz veri")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePredefinedNetworkRangeDto dto)
        {
            try
            {
                var existingRange = await _predefinedNetworkRangeService.GetByIdAsync(id);
                if (existingRange == null)
                    return NotFound(new { error = "Öntanımlı ağ aralığı bulunamadı" });

                existingRange.Name = dto.Name;
                existingRange.Description = dto.Description;
                existingRange.TimeoutSeconds = dto.TimeoutSeconds;
                existingRange.PortScanType = dto.PortScanType;
                existingRange.IsActive = dto.IsActive;

                var updatedRange = await _predefinedNetworkRangeService.UpdateAsync(existingRange);
                return Ok(updatedRange);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating predefined network range");
                return BadRequest(new { error = $"Öntanımlı ağ aralığı güncellenemedi: {ex.Message}" });
            }
        }

        [HttpDelete("{id}")]
        [SwaggerOperation(Summary = "Öntanımlı ağ aralığını sil", Description = "Mevcut öntanımlı ağ aralığını siler")]
        [SwaggerResponse(200, "Öntanımlı ağ aralığı başarıyla silindi")]
        [SwaggerResponse(404, "Öntanımlı ağ aralığı bulunamadı")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var deleted = await _predefinedNetworkRangeService.DeleteAsync(id);
            if (!deleted)
                return NotFound(new { error = "Öntanımlı ağ aralığı bulunamadı" });

            return Ok(new { message = "Öntanımlı ağ aralığı başarıyla silindi" });
        }

        [HttpPost("{id}/scan")]
        [SwaggerOperation(Summary = "Öntanımlı ağ aralığını tara", Description = "Belirli bir öntanımlı ağ aralığı için tarama başlatır")]
        [SwaggerResponse(200, "Ağ taraması başarıyla başlatıldı")]
        [SwaggerResponse(404, "Öntanımlı ağ aralığı bulunamadı")]
        [SwaggerResponse(400, "Ağ taraması başlatılamadı")]
        public async Task<IActionResult> ScanPredefinedRange(Guid id)
        {
            try
            {
                var predefinedRange = await _predefinedNetworkRangeService.GetByIdAsync(id);
                if (predefinedRange == null)
                    return NotFound(new { error = "Öntanımlı ağ aralığı bulunamadı" });

                await _networkScanService.TriggerScanForPredefinedRangeAsync(id);
                return Ok(new { message = $"Ağ taraması '{predefinedRange.Name}' ({predefinedRange.NetworkRange}) için başarıyla başlatıldı" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error scanning predefined network range");
                return BadRequest(new { error = $"Ağ taraması başlatılamadı: {ex.Message}" });
            }
        }
    }

    public class CreatePredefinedNetworkRangeDto
    {
        public string Name { get; set; } = string.Empty;
        public string NetworkRange { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int TimeoutSeconds { get; set; } = 5;
        public string PortScanType { get; set; } = "common";
    }

    public class UpdatePredefinedNetworkRangeDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int TimeoutSeconds { get; set; } = 5;
        public string PortScanType { get; set; } = "common";
        public bool IsActive { get; set; } = true;
    }
}