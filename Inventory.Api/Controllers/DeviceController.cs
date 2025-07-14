using Microsoft.AspNetCore.Mvc;
using Inventory.Domain.Entities;

namespace Inventory.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DeviceController : ControllerBase
    {
        // Geçici olarak bellekte cihaz listesi tutuyoruz
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
            device.Id = Guid.NewGuid();
            Devices.Add(device);
            return CreatedAtAction(nameof(GetById), new { id = device.Id }, device);
        }

        [HttpPut("{id}")]
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
    }
}