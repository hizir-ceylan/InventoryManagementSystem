using Microsoft.AspNetCore.Mvc;
using Inventory.Shared.Utils;
using Swashbuckle.AspNetCore.Annotations;

namespace Inventory.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [SwaggerTag("Network location management operations")]
    public class LocationController : ControllerBase
    {
        private readonly ILogger<LocationController> _logger;

        public LocationController(ILogger<LocationController> logger)
        {
            _logger = logger;
        }

        [HttpGet("network-mappings")]
        [SwaggerOperation(Summary = "Ağ konum eşlemelerini getir", Description = "Mevcut ağ-konum eşlemelerinin listesini döndürür")]
        [SwaggerResponse(200, "Ağ konum eşlemeleri", typeof(Dictionary<string, string>))]
        public ActionResult<Dictionary<string, string>> GetNetworkLocationMappings()
        {
            var mappings = LocationHelper.GetNetworkLocationMappings();
            return Ok(mappings);
        }

        [HttpPost("network-mappings")]
        [SwaggerOperation(Summary = "Yeni ağ konum eşlemesi ekle", Description = "Belirtilen ağ için konum eşlemesi ekler")]
        [SwaggerResponse(200, "Eşleme başarıyla eklendi")]
        [SwaggerResponse(400, "Geçersiz parametre veya eşleme zaten mevcut")]
        public ActionResult AddNetworkLocationMapping([FromBody] NetworkLocationMappingDto mapping)
        {
            if (string.IsNullOrWhiteSpace(mapping.NetworkPrefix) || string.IsNullOrWhiteSpace(mapping.Location))
            {
                return BadRequest(new { error = "Network prefix ve location alanları boş olamaz." });
            }

            bool success = LocationHelper.AddNetworkLocationMapping(mapping.NetworkPrefix, mapping.Location);
            if (!success)
            {
                return BadRequest(new { error = "Bu ağ için zaten bir konum eşlemesi mevcut." });
            }

            _logger.LogInformation("Yeni ağ konum eşlemesi eklendi: {NetworkPrefix} -> {Location}", 
                mapping.NetworkPrefix, mapping.Location);

            return Ok(new { message = "Ağ konum eşlemesi başarıyla eklendi.", 
                networkPrefix = mapping.NetworkPrefix, location = mapping.Location });
        }

        [HttpPut("network-mappings/{networkPrefix}")]
        [SwaggerOperation(Summary = "Ağ konum eşlemesini güncelle", Description = "Mevcut ağ konum eşlemesini günceller")]
        [SwaggerResponse(200, "Eşleme başarıyla güncellendi")]
        [SwaggerResponse(404, "Eşleme bulunamadı")]
        [SwaggerResponse(400, "Geçersiz parametre")]
        public ActionResult UpdateNetworkLocationMapping(string networkPrefix, [FromBody] LocationUpdateDto locationUpdate)
        {
            if (string.IsNullOrWhiteSpace(locationUpdate.Location))
            {
                return BadRequest(new { error = "Location alanı boş olamaz." });
            }

            bool success = LocationHelper.UpdateNetworkLocationMapping(networkPrefix, locationUpdate.Location);
            if (!success)
            {
                return NotFound(new { error = "Belirtilen ağ için konum eşlemesi bulunamadı." });
            }

            _logger.LogInformation("Ağ konum eşlemesi güncellendi: {NetworkPrefix} -> {Location}", 
                networkPrefix, locationUpdate.Location);

            return Ok(new { message = "Ağ konum eşlemesi başarıyla güncellendi.", 
                networkPrefix = networkPrefix, location = locationUpdate.Location });
        }

        [HttpDelete("network-mappings/{networkPrefix}")]
        [SwaggerOperation(Summary = "Ağ konum eşlemesini sil", Description = "Belirtilen ağ konum eşlemesini siler")]
        [SwaggerResponse(200, "Eşleme başarıyla silindi")]
        [SwaggerResponse(404, "Eşleme bulunamadı")]
        public ActionResult RemoveNetworkLocationMapping(string networkPrefix)
        {
            bool success = LocationHelper.RemoveNetworkLocationMapping(networkPrefix);
            if (!success)
            {
                return NotFound(new { error = "Belirtilen ağ için konum eşlemesi bulunamadı." });
            }

            _logger.LogInformation("Ağ konum eşlemesi silindi: {NetworkPrefix}", networkPrefix);

            return Ok(new { message = "Ağ konum eşlemesi başarıyla silindi.", networkPrefix = networkPrefix });
        }

        [HttpGet("example")]
        [SwaggerOperation(Summary = "Örnek kullanım", Description = "Ağ konum eşlemesi nasıl kullanılacağına dair örnekler")]
        [SwaggerResponse(200, "Örnek bilgiler")]
        public ActionResult GetExample()
        {
            var examples = new
            {
                description = "Ağ konum eşlemesi nasıl çalışır",
                currentMappings = LocationHelper.GetNetworkLocationMappings(),
                examples = new[]
                {
                    new { networkPrefix = "192.168.1", location = "Ofis Ana Katta", description = "192.168.1.x ağındaki tüm cihazlar" },
                    new { networkPrefix = "192.168.2", location = "Üretim Katı", description = "192.168.2.x ağındaki tüm cihazlar" },
                    new { networkPrefix = "192.168.112", location = "Stajyer", description = "192.168.112.x ağındaki tüm cihazlar (mevcut)" },
                    new { networkPrefix = "10.0.1", location = "IT Departmanı", description = "10.0.1.x ağındaki tüm cihazlar" }
                },
                usage = new
                {
                    addNew = "POST /api/location/network-mappings ile yeni eşleme ekleyin",
                    update = "PUT /api/location/network-mappings/{networkPrefix} ile mevcut eşlemeyi güncelleyin",
                    delete = "DELETE /api/location/network-mappings/{networkPrefix} ile eşlemeyi silin",
                    list = "GET /api/location/network-mappings ile tüm eşlemeleri listeleyin"
                }
            };

            return Ok(examples);
        }
    }

    public class NetworkLocationMappingDto
    {
        public string NetworkPrefix { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
    }

    public class LocationUpdateDto
    {
        public string Location { get; set; } = string.Empty;
    }
}