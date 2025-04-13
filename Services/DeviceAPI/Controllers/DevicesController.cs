using DeviceAPI.Authentication;
using DeviceAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using System.Security.Claims;

namespace DeviceAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    //[Authorize]
    public class DevicesController : ControllerBase
    {
        private readonly IMongoCollection<Device> _devices;
        private readonly ILogger<DevicesController> _logger;
        private readonly ICertificateAuthService _certAuthService;

        public DevicesController(IMongoDatabase database,
            ILogger<DevicesController> logger,
            CertificateAuthService certAuthService)
        {
            _devices = database.GetCollection<Device>("devices");
            _logger = logger;
            _certAuthService = certAuthService;
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

        //register device 
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

        [HttpPost("certificate/{deviceId}")]
        //[Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateDeviceCertificate(string deviceId)
        {
            try
            {
                var device = await _devices.Find(d => d.DeviceId == deviceId).FirstOrDefaultAsync();
                if (device == null)
                {
                    return NotFound(new ProblemDetails
                    {
                        Title = "Device not found",
                        Detail = $"Device {deviceId} does not exist",
                        Status = 404
                    });
                }

                var result = await _certAuthService.CreateCertificateAsync(device);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new ProblemDetails
                {
                    Title = "Certificate exists",
                    Detail = ex.Message,
                    Status = 409
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create certificate for device {DeviceId}", deviceId);
                return StatusCode(500, new ProblemDetails
                {
                    Title = "Certificate generation failed",
                    Detail = "An error occurred while generating the certificate",
                    Status = 500
                });
            }
        }

        [HttpDelete("certificate/{deviceId}")]
        //[Authorize(Roles = "Admin")]
        public async Task<IActionResult> RevokeDeviceCertificate(string deviceId)
        {
            try
            {
                await _certAuthService.RevokeCertificateAsync(deviceId);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new ProblemDetails
                {
                    Title = "Device not found",
                    Detail = ex.Message,
                    Status = 404
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to revoke certificate for device {DeviceId}", deviceId);
                return StatusCode(500, new ProblemDetails
                {
                    Title = "Revocation failed",
                    Detail = "An error occurred while revoking the certificate",
                    Status = 500
                });
            }
        }


        // get device info
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
                if (device.Authentication != null)
                {
                    device.Authentication.Credentials = null;
                    device.Authentication.EncryptedPrivateKey = null;
                }

                return Ok(device);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting device {Id}", id);
                return StatusCode(500, "Internal server error");
            }
        }



        //[HttpGet("me")]
        //public async Task<IActionResult> GetCurrentDevice()
        //{
        //    try
        //    {
        //        var deviceId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        //        if (string.IsNullOrEmpty(deviceId))
        //        {
        //            return Unauthorized(new { Message = "Invalid token claims" });
        //        }

        //        var device = await _devices.Find(d => d.DeviceId == deviceId)
        //            .Project<Device>(Builders<Device>.Projection
        //                .Exclude(d => d.Authentication.Credentials)
        //                .Exclude(d => d.Authentication.EncryptedPrivateKey))
        //            .FirstOrDefaultAsync();

        //        if (device == null)
        //        {
        //            return NotFound(new { Message = "Device not found" });
        //        }

        //        return Ok(device);
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Failed to get device information");
        //        return StatusCode(500, new { Message = "Internal server error" });
        //    }
        //}


        // update device location and address
        [HttpPost("location")]
        //[Authorize]
        public async Task<IActionResult> UpdateLocation([FromBody] LocationUpdateDto locationUpdate)
        {
            try
            {
                var device = await _devices.Find(d => d.Id == locationUpdate.id).FirstOrDefaultAsync();
                if (device.DeviceId == null)
                {
                    return NotFound();
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

                var result = await _devices.UpdateOneAsync(d => d.DeviceId == locationUpdate.id, update);

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
        //[Authorize]
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
        //[Authorize]
        public async Task<IActionResult> CheckFirmwareUpdate()
        {
            try
            {
                var deviceId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(deviceId))
                {
                    return Unauthorized(new { Message = "Invalid token claims" });
                }

                var device = await _devices.Find(d => d.Id == deviceId).FirstOrDefaultAsync();
                if (device == null)
                {
                    return NotFound(new { Message = "Device not found" });
                }

                if(device.FirmwareVersion != device.FirmwareUpdateVersion)
                {
                    return BadRequest(new { Message = "Fireware alreay updated" });
                }

                // In a real implementation, you would check against a firmware repository
                return Ok(new
                {
                    CurrentVersion = device.FirmwareVersion,
                    LatestVersion = device.FirmwareUpdateVersion,
                    UpdateAvailable = device.FirmwareVersion != device.FirmwareUpdateVersion,
                    UpdateUrl = $"https://firmware.yourdomain.com/Firmware/{device.Name}/{device.FirmwareUpdateVersion}.bin"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to check firmware updates");
                return StatusCode(500, new { Message = "Internal server error" });
            }
        }




        [HttpGet]
        //[Authorize(Roles = "Admin")]
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
        public string id { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string? Address { get; set; }
    }

    public class StatusUpdateDto
    {
        public DeviceStatus Status { get; set; }
    }
}