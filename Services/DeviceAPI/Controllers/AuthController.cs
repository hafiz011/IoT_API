using DeviceAPI.Models;

using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
namespace DeviceAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<Device> _userManager;
        private readonly SignInManager<Device> _signInManager;
        private readonly RoleManager<DeviceRole> _roleManager;
        private readonly IMongoCollection<Device> _devices;
        private readonly ILogger<AuthController> _logger;
        private readonly IConfiguration _configuration;

        public AuthController(
            UserManager<Device> userManager,
            SignInManager<Device> signInManager,
            RoleManager<DeviceRole> roleManager,
            IMongoDatabase database,
            ILogger<AuthController> logger,
            IConfiguration configuration)
        {
            _devices = database.GetCollection<Device>("devices");
            _logger = logger;
            _configuration = configuration;
        }

        public class LoginRequestModel
        {
            public string deviceId { get; set; }
            public string Password { get; set; }
        }

        [HttpPost("devicelogin")]
        public async Task<IActionResult> Login([FromBody] LoginRequestModel model)
        {

            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var user = await _userManager.FindByEmailAsync(model.deviceId);
                if (user == null)
                    return Unauthorized(new { Message = "Invalid deviceId or password" });

                if (user == null || !await _userManager.CheckPasswordAsync(user, model.Password))
                    return Unauthorized(new { Message = "Invalid deviceId or password" });

                if (user.LockoutEnd.HasValue && user.LockoutEnd > DateTime.UtcNow)
                    return Unauthorized(new { Message = "Your deviceId is locked. Please try again later." });

                var roles = await _userManager.GetRolesAsync(user);
                var role = roles.Count > 0 ? roles[0] : "Device";

                var token = JwtTokenHelper.GenerateToken(user.Id.ToString(), role, _configuration["JwtSettings:Key"], _configuration["JwtSettings:Issuer"], _configuration["JwtSettings:Audience"]);
                _logger.LogInformation($"User {user.Email} successfully logged in.");

                return Ok(new
                {
                    Token = token,
                    User = new
                    {
                        user.Id,
                        user.DeviceId,
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "An unexpected error occurred. Please try again later." });
            }
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return Ok(new { Message = "Logged out successfully." });
        }


        public class StatusUpdateDto
        {
            public DeviceStatus Status { get; set; }
        }

        [HttpPost("status")]
        //[Authorize]
        public async Task<IActionResult> UpdateStatus([FromBody] StatusUpdateDto statusUpdate)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized(new { Message = "User not authenticated." });

                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                    return NotFound(new { Message = "User not found." });

                user.Status = statusUpdate.Status;
                user.LastSeenAt = DateTime.UtcNow;

                var result = await _userManager.UpdateAsync(user);
                if (!result.Succeeded)
                {
                    return BadRequest(new { Message = "Failed to update status." });
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update device status");
                return StatusCode(500, new { Message = "Internal server error" });
            }
        }


    }
} 