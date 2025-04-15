using DeviceAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace DeviceAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AdminController : ControllerBase
    {

        private readonly ILogger<AuthController> _logger;
        private readonly UserManager<Device> _userManager;
        private readonly RoleManager<DeviceRole> _roleManager;

        public AdminController(
            ILogger<AuthController> logger,
            UserManager<Device> userManager,
            RoleManager<DeviceRole> roleManager)
        {
            _logger = logger;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        // DTO for device registration
        public class DeviceRegistrationDto
        {
            public string DeviceId { get; set; }
            public string password { get; set; }
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

                var existingUser = await _userManager.FindByNameAsync(dto.DeviceId);
                if (existingUser != null)
                    return BadRequest(new { Message = "Device already exists" });

                var data = new Device
                {
                    DeviceId = dto.DeviceId,
                    UserName = dto.DeviceId, // Use DeviceId as username
                    Name = dto.Name,
                    FirmwareVersion = dto.FirmwareVersion,
                    HardwareVersion = dto.HardwareVersion,
                    SerialNumber = dto.SerialNumber,
                    MacAddress = dto.MacAddress,
                    Status = DeviceStatus.Offline,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                // Use a simple default password (example: "1234" or anything else)
                var result = await _userManager.CreateAsync(data, dto.password);
                if (!result.Succeeded)
                    return BadRequest(new { Message = "Device registration failed", Errors = result.Errors });

                // Assign default role
                const string defaultRole = "Device";
                if (!await _roleManager.RoleExistsAsync(defaultRole))
                {
                    var roleResult = await _roleManager.CreateAsync(new DeviceRole { Name = defaultRole });
                    if (!roleResult.Succeeded)
                        return BadRequest(new { Message = "Role creation failed", Errors = roleResult.Errors });
                }

                await _userManager.AddToRoleAsync(data, defaultRole);

                _logger.LogInformation("Device {DeviceId} registered", dto.DeviceId);
                return Ok(new { Message = "Device registered successfully!", DeviceId = dto.DeviceId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Device registration error");
                return StatusCode(500, "Internal server error");
            }
        }


        // get device info
        [HttpGet("GetDevice")]
        public async Task<IActionResult> GetDevice([FromQuery] string deviceId)
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


        [HttpGet]
        // [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllDevices([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                if (page < 1) page = 1;
                if (pageSize < 1) pageSize = 10;

                var totalCount = _userManager.Users.Count();

                var devices = _userManager.Users
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(d => new
                    {
                        d.Id,
                        d.DeviceId,
                        d.Name,
                        d.Status,
                        d.LastSeenAt
                    })
                    .ToList(); // Sync if async not supported

                return Ok(new
                {
                    TotalCount = totalCount,
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
}
