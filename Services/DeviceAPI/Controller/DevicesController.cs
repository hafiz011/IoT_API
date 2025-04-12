using DeviceAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using System.Security.Claims;

namespace DeviceAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DevicesController : ControllerBase
    {
        private readonly IMongoCollection<Device> _devices;
        private readonly ILogger<DevicesController> _logger;

        public DevicesController(IMongoDatabase database, ILogger<DevicesController> logger)
        {
            _devices = database.GetCollection<Device>("devices");
            _logger = logger;
        }



        public class DeviceRegistrationDto
        {
            public string DeviceId { get; set; }
            public string Name { get; set; }
            public string FirmwareVersion { get; set; }
            public string HardwareVersion { get; set; }
            public string SerialNumber { get; set; }
            public string MacAddress { get; set; }
        }

        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> RegisterDevice([FromBody] DeviceRegistrationDto dto)
        {
            try
            {
                dto.DeviceId = Guid.NewGuid().ToString();
                var existingDevice = await _devices.Find(d => d.DeviceId == dto.DeviceId).FirstOrDefaultAsync();
                if (existingDevice != null)
                {
                    return Conflict($"Device with ID {dto.DeviceId} already exists");
                }

                var device = new Device
                {
                    DeviceId = dto.DeviceId,
                    Name = dto.Name,
                    FirmwareVersion = dto.FirmwareVersion,
                    HardwareVersion = dto.HardwareVersion,
                    SerialNumber = dto.SerialNumber,
                    MacAddress = dto.MacAddress,
                    Status = DeviceStatus.Offline,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                await _devices.InsertOneAsync(device);

                _logger.LogInformation("Device {DeviceId} registered successfully", dto.DeviceId);
                return CreatedAtAction(nameof(GetDevice), new { id = device.Id }, device);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering device");
                return StatusCode(500, "Internal server error");
            }
        }


        [HttpGet("{id}")]
        public async Task<IActionResult> GetDevice(string id)
        {
            try
            {
                var device = await _devices.Find(d => d.Id == id).FirstOrDefaultAsync();
                if (device == null)
                {
                    return NotFound();
                }

                // Sanitize sensitive data before returning
                device.Authentication.Credentials = null;
                device.Authentication.EncryptedPrivateKey = null;

                return Ok(device);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting device {Id}", id);
                return StatusCode(500, "Internal server error");
            }
        }
















        [HttpGet("me")]
        public async Task<IActionResult> GetCurrentDevice()
        {
            try
            {
                var deviceId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(deviceId))
                {
                    return Unauthorized(new { Message = "Invalid token claims" });
                }

                var device = await _devices.Find(d => d.DeviceId == deviceId)
                    .Project<Device>(Builders<Device>.Projection
                        .Exclude(d => d.Authentication.Credentials)
                        .Exclude(d => d.Authentication.EncryptedPrivateKey))
                    .FirstOrDefaultAsync();

                if (device == null)
                {
                    return NotFound(new { Message = "Device not found" });
                }

                return Ok(device);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get device information");
                return StatusCode(500, new { Message = "Internal server error" });
            }
        }

        [HttpPost("location")]
        [Authorize]
        public async Task<IActionResult> UpdateLocation([FromBody] LocationUpdateDto locationUpdate)
        {
            try
            {
                var deviceId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(deviceId))
                {
                    return Unauthorized(new { Message = "Invalid token claims" });
                }

                var update = Builders<Device>.Update
                    .Set(d => d.Location, new DeviceLocation
                    {
                        Latitude = locationUpdate.Latitude,
                        Longitude = locationUpdate.Longitude,
                        Address = locationUpdate.Address,
                        LastUpdated = DateTime.UtcNow
                    })
                    .Set(d => d.LastSeenAt, DateTime.UtcNow);

                var result = await _devices.UpdateOneAsync(
                    d => d.DeviceId == deviceId,
                    update);

                if (result.MatchedCount == 0)
                {
                    return NotFound(new { Message = "Device not found" });
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update device location");
                return StatusCode(500, new { Message = "Internal server error" });
            }
        }

        [HttpPost("status")]
        [Authorize]
        public async Task<IActionResult> UpdateStatus([FromBody] StatusUpdateDto statusUpdate)
        {
            try
            {
                var deviceId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(deviceId))
                {
                    return Unauthorized(new { Message = "Invalid token claims" });
                }

                var update = Builders<Device>.Update
                    .Set(d => d.Status, statusUpdate.Status)
                    .Set(d => d.LastSeenAt, DateTime.UtcNow);

                var result = await _devices.UpdateOneAsync(
                    d => d.DeviceId == deviceId,
                    update);

                if (result.MatchedCount == 0)
                {
                    return NotFound(new { Message = "Device not found" });
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update device status");
                return StatusCode(500, new { Message = "Internal server error" });
            }
        }

        [HttpGet("firmware")]
        [Authorize]
        public async Task<IActionResult> CheckFirmwareUpdate()
        {
            try
            {
                var deviceId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(deviceId))
                {
                    return Unauthorized(new { Message = "Invalid token claims" });
                }

                var device = await _devices.Find(d => d.DeviceId == deviceId)
                    .Project(d => new { d.FirmwareVersion, d.DeviceId })
                    .FirstOrDefaultAsync();

                if (device == null)
                {
                    return NotFound(new { Message = "Device not found" });
                }

                // In a real implementation, you would check against a firmware repository
                return Ok(new
                {
                    CurrentVersion = device.FirmwareVersion,
                    LatestVersion = "1.2.0", // Example
                    UpdateAvailable = device.FirmwareVersion != "1.2.0",
                    UpdateUrl = $"https://firmware.yourdomain.com/{device.DeviceId}/1.2.0"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to check firmware updates");
                return StatusCode(500, new { Message = "Internal server error" });
            }
        }




        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllDevices([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                var total = await _devices.CountDocumentsAsync(FilterDefinition<Device>.Empty);
                var devices = await _devices.Find(_ => true)
                    .Skip((page - 1) * pageSize)
                    .Limit(pageSize)
                    .Project(d => new
                    {
                        d.Id,
                        d.DeviceId,
                        d.Name,
                        d.Status,
                        d.LastSeenAt
                    })
                    .ToListAsync();

                return Ok(new
                {
                    TotalCount = total,
                    Page = page,
                    PageSize = pageSize,
                    Devices = devices
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all devices");
                return StatusCode(500, "Internal server error");
            }
        }


    }

    public class LocationUpdateDto
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string? Address { get; set; }
    }

    public class StatusUpdateDto
    {
        public DeviceStatus Status { get; set; }
    }
}