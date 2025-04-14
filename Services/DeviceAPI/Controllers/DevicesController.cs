using DeviceAPI.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using MongoDB.Driver.Linq;


namespace DeviceAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    //[Authorize]
    public class DevicesController : ControllerBase
    {
        private readonly ILogger<DevicesController> _logger;
        private readonly UserManager<Device> _userManager;

        public DevicesController(
            ILogger<DevicesController> logger,
            UserManager<Device> userManager)
        {
            _logger = logger;
            _userManager = userManager;
        }

        // get device info
        [HttpGet("GetDevice/{deviceId}")]
        public async Task<IActionResult> GetDevice(string deviceId)
        {
            try
            {
                var device = await _userManager.FindByNameAsync(deviceId);
                if (device == null)
                {
                    return NotFound();
                }


                return Ok(device);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting device {Id}", deviceId);
                return StatusCode(500, "Internal server error");
            }
        }

        // update device location and address
        [HttpPost("location")]
        //[Authorize]
        public async Task<IActionResult> UpdateLocation([FromBody] LocationUpdateDto locationUpdate)
        {
            try
            {
                var device = await _userManager.FindByNameAsync(locationUpdate.id);
                if (device.DeviceId == null)
                {
                    return NotFound();
                }

                device.Location.Address = locationUpdate.Address;
                device.Location.Latitude = locationUpdate.Latitude;
                device.Location.Longitude = locationUpdate.Longitude;
                device.Location.LastUpdated = DateTime.UtcNow;

                var result = await _userManager.UpdateAsync(device);

                if (!result.Succeeded)
                {
                    return NotFound(new { Message = "Device not found" });
                }

                return Ok((new { Message = "Device Location Updated" }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update device location");
                return StatusCode(500, new { Message = "Internal server error" });
            }
        }

        [HttpGet("firmware")]
        //[Authorize]
        public async Task<IActionResult> CheckFirmwareUpdate([FromQuery] string deviceId)
        {
            try
            {
                if (string.IsNullOrEmpty(deviceId))
                    return Unauthorized(new { Message = "User not authenticated." });

                var device = await _userManager.FindByNameAsync(deviceId);
                if (device == null)
                    return NotFound(new { Message = "User not found." });


                if(device.FirmwareVersion == device.FirmwareUpdateVersion)
                {
                    return BadRequest(new { Message = "Fireware alreay updated" });
                }

                // In a real implementation, you would check against a firmware repository
                return Ok(new
                {
                    CurrentVersion = device.FirmwareVersion,
                    LatestVersion = device.FirmwareUpdateVersion,
                    UpdateUrl = $"https://firmware.yourdomain.com/Firmware/{device.Name}/{device.FirmwareUpdateVersion}.bin"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to check firmware updates");
                return StatusCode(500, new { Message = "Internal server error" });
            }
        }

    }

    public class LocationUpdateDto
    {
        public string id { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string? Address { get; set; }
    }
}