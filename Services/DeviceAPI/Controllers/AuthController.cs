using DeviceAPI.Authentication;
using DeviceAPI.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using DeviceAPI.DbContext;
using System.Net;

namespace DeviceAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly ICertificateAuthService _certAuthService;
        private readonly JwtService _jwtService;
        private readonly IMongoCollection<Device> _devices;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            CertificateAuthService certAuthService,
            JwtService jwtService,
            IMongoDatabase database,
            ILogger<AuthController> logger)
        {
            _certAuthService = certAuthService;
            _jwtService = jwtService;
            _devices = database.GetCollection<Device>("devices");
            _logger = logger;
        }


        [HttpPost("certificate")]
        public async Task<IActionResult> AuthenticateWithCertificate()
        {
            try
            {
                var certificate = await HttpContext.Connection.GetClientCertificateAsync();
                if (certificate == null)
                {
                    return Unauthorized(new ProblemDetails
                    {
                        Title = "Certificate required",
                        Detail = "Client certificate must be provided",
                        Status = 401
                    });
                }

                var device = await _certAuthService.AuthenticateDeviceAsync(certificate);
                if (device == null)
                {
                    return Unauthorized(new ProblemDetails
                    {
                        Title = "Authentication failed",
                        Detail = "Invalid certificate or device not registered",
                        Status = 401
                    });
                }

                // Generate JWT
                var token = _jwtService.GenerateToken(device);
                var expiryMinutes = _jwtService.GetExpiryMinutes();
                var ipaddress = HttpContext.Connection.RemoteIpAddress.ToString();
                // Update device with new token
                var update = Builders<Device>.Update
                    .Set(d => d.Authentication.Token, token)
                    .Set(d => d.Authentication.TokenExpiresAt, DateTime.UtcNow.AddMinutes(expiryMinutes))
                    .Set(d => d.LastSeenAt, DateTime.UtcNow)
                    .Set(d => d.Status, DeviceStatus.Online)
                    .Set(d => d.IpAddress, ipaddress);

                await _devices.UpdateOneAsync(d => d.Id == device.Id, update);

                _logger.LogInformation("Device {DeviceId} authenticated successfully", device.DeviceId);

                return Ok(new
                {
                    Token = token,
                    ExpiresIn = expiryMinutes * 60,
                    DeviceId = device.DeviceId,
                    Thumbprint = certificate.Thumbprint
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Device authentication failed");
                return StatusCode(500, new { Message = "Internal server error" });
            }
        }

     

        [HttpPost("validate")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public IActionResult ValidateToken()
        {
            var deviceId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var expiry = User.FindFirstValue(JwtRegisteredClaimNames.Exp);

            if (string.IsNullOrEmpty(deviceId))
            {
                return Unauthorized(new ProblemDetails
                {
                    Title = "Invalid token",
                    Detail = "Token does not contain required claims",
                    Status = 401
                });
            }

            return Ok(new
            {
                DeviceId = deviceId,
                IsValid = true,
                Expires = expiry
            });
        }



        [HttpPost("refresh")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> RefreshToken()
        {
            try
            {
                var deviceId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(deviceId))
                {
                    return Unauthorized(new { Message = "Invalid token claims" });
                }

                var device = await _devices.Find(d => d.DeviceId == deviceId).FirstOrDefaultAsync();
                if (device == null)
                {
                    return NotFound(new { Message = "Device not found" });
                }

                // Generate new JWT
                var token = _jwtService.GenerateToken(device);
                var expiryMinutes = _jwtService.GetExpiryMinutes();
                var ipaddress = HttpContext.Connection.RemoteIpAddress.ToString();
                // Update device with new token
                var update = Builders<Device>.Update
                    .Set(d => d.Authentication.Token, token)
                    .Set(d => d.Authentication.TokenExpiresAt, DateTime.UtcNow.AddMinutes(expiryMinutes))
                    .Set(d => d.LastSeenAt, DateTime.UtcNow)
                    .Set(d => d.IpAddress, ipaddress);

                await _devices.UpdateOneAsync(d => d.Id == device.Id, update);

                return Ok(new
                {
                    Token = token,
                    ExpiresIn = expiryMinutes * 60,
                    DeviceId = device.DeviceId
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Token refresh failed");
                return StatusCode(500, new { Message = "Internal server error" });
            }
        }


    }
} 