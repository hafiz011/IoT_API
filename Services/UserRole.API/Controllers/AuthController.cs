using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Security.Claims;
using UserRoleAPI.Helpers;
using UserRoleAPI.Models;

namespace UserRoleAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly IConfiguration _configuration;
        private readonly EmailService _emailService;
        private readonly ILogger<AuthController> _logger;



        public AuthController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IConfiguration configuration,
            RoleManager<ApplicationRole> roleManager,
            EmailService emailSender,
            ILogger<AuthController> logger
            )
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _configuration = configuration;
            _roleManager = roleManager;
            _emailService = emailSender;
            _logger = logger;
        }


        // POST: api/auth/login
        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequestModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);


                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user == null)
                    return Unauthorized(new { Message = "Invalid email or password" });

                if (!user.EmailConfirmed)
                {
                    return Unauthorized(new { Message = "You need to confirm your email before logging in." });
                }

                if (user == null || !await _userManager.CheckPasswordAsync(user, model.Password))
                    return Unauthorized(new { Message = "Invalid email or password" });

                if (user.LockoutEnd.HasValue && user.LockoutEnd > DateTime.UtcNow)
                    return Unauthorized(new { Message = "Your account is locked. Please try again later." });

                var roles = await _userManager.GetRolesAsync(user);
                var role = roles.Count > 0 ? roles[0] : "User";

                var token = JwtTokenHelper.GenerateToken(user.Id.ToString(), role, _configuration["JwtSettings:Key"], _configuration["JwtSettings:Issuer"], _configuration["JwtSettings:Audience"]);
                _logger.LogInformation($"User {user.Email} successfully logged in.");

                return Ok(new
                {
                    Token = token,
                    User = new
                    {
                        user.Id,
                        user.FirstName,
                        user.LastName,
                        user.Email,
                        user.PhoneNumber,
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



        // POST: api/auth/register
        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var existingUser = await _userManager.FindByEmailAsync(model.Email);
            if (existingUser != null)
                return BadRequest(new { Message = "Email already in use" });

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FirstName = model.FirstName,
                LastName = model.LastName,
                PhoneNumber = model.Phone,
                Address = model.Address
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
                return BadRequest(new { Message = "User registration failed", Errors = result.Errors });

            const string defaultRole = "User";

            var roleExists = await _roleManager.RoleExistsAsync(defaultRole);
            if (!roleExists)
            {
                var roleResult = await _roleManager.CreateAsync(new ApplicationRole { Name = defaultRole });
                if (!roleResult.Succeeded)
                {
                    return BadRequest(new { Message = "Failed to create default role", Errors = roleResult.Errors });
                }
            }

            await _userManager.AddToRoleAsync(user, defaultRole);

            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);

            var confirmationLink = Url.Action("ConfirmEmail", "Auth",
                   new { userId = user.Id, token }, Request.Scheme);

            bool emailSent = await _emailService.SendEmailAsync(user.Email, "Confirm your email",
                $"Please confirm your account by clicking this link: <a href='{confirmationLink}'>Confirm Email</a>");

            if (!emailSent)
                return StatusCode(500, new { Message = "User registered, but failed to send confirmation email." });

            return Ok(new { Message = "User registered successfully! Please check your email for confirmation." });
        }

        [HttpPost("confirm-email")]
        public async Task<IActionResult> ConfirmEmail(ConfirmEmailModel model)
        {
            if (string.IsNullOrEmpty(model.UserId) || string.IsNullOrEmpty(model.Token))
            {
                return BadRequest(new { Message = "Invalid confirmation request. User ID and Token are required." });
            }

            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user == null)
            {
                return NotFound(new { Message = "User not found." });
            }

            if (user.EmailConfirmed)
            {
                return Ok(new { Message = "Your email is already confirmed. You can log in now." });
            }
            var decodedToken = WebUtility.UrlDecode(model.Token);
            var result = await _userManager.ConfirmEmailAsync(user, decodedToken);
            if (!result.Succeeded)
            {
                return BadRequest(new { Message = "Email confirmation failed. Invalid or expired token." });
            }

            return Ok(new { Message = "Email confirmed successfully! You can now log in." });
        }


        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new
                {
                    Message = "Invalid input",
                    Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)
                });
            }

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
                return NotFound(new { Message = "User not found" });

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var encodedToken = WebUtility.UrlEncode(token);
            var resetUrl = $"{Request.Scheme}://{Request.Host}/reset-password?token={encodedToken}&email={model.Email}";

            _logger.LogInformation($"Generated password reset token for {model.Email}");

            bool emailSent = await _emailService.SendEmailAsync(user.Email, "Password Reset",
                $"Click here to reset your password: <a href='{resetUrl}'>Reset Password</a>");

            if (!emailSent)
                return StatusCode(500, new { Message = "Failed to send reset password email." });

            return Ok(new { Message = "Password reset link sent to your email." });
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword(ResetPasswordRequestModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new
                {
                    Message = "Invalid input",
                    Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)
                });
            }

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
                return NotFound(new { Message = "User not found" });
            var decodedToken = WebUtility.UrlDecode(model.Token);
            var result = await _userManager.ResetPasswordAsync(user, decodedToken, model.NewPassword);

            if (!result.Succeeded)
            {
                return BadRequest(new
                {
                    Message = "Invalid or expired token",
                    Errors = result.Errors.Select(e => e.Description)
                });
            }

            _logger.LogInformation($"Password successfully reset for {model.Email}");
            return Ok(new { Message = "Password reset successfully." });
        }

        [HttpGet("account")]
        public async Task<IActionResult> GetAccountDetails()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { Message = "User not authenticated." });

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return NotFound(new { Message = "User not found." });

            return Ok(new
            {
                user.Id,
                user.FirstName,
                user.LastName,
                user.Email,
                user.PhoneNumber,
                user.Address,
                user.ImagePath
            });
        }

        [HttpPut("account")]
        public async Task<IActionResult> UpdateAccount(UpdateAccountModel model)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { Message = "User not authenticated." });

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return NotFound(new { Message = "User not found." });

            user.FirstName = model.FirstName ?? user.FirstName;
            user.LastName = model.LastName ?? user.LastName;
            user.PhoneNumber = model.Phone ?? user.PhoneNumber;
            user.Address = model.Address ?? user.Address;

            if (model.ImagePath != null)
            {
                var imagesPath = Path.Combine("wwwroot", "images", "profile");
                if (!Directory.Exists(imagesPath))
                {
                    Directory.CreateDirectory(imagesPath);
                }

                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(model.ImagePath.FileName);
                var filePath = Path.Combine(imagesPath, fileName);

                try
                {
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await model.ImagePath.CopyToAsync(stream);
                    }

                    if (!string.IsNullOrEmpty(user.ImagePath))
                    {
                        var oldFilePath = Path.Combine("wwwroot", user.ImagePath.TrimStart('/'));
                        if (System.IO.File.Exists(oldFilePath))
                        {
                            System.IO.File.Delete(oldFilePath);
                        }
                    }

                    user.ImagePath = $"/images/profile/{fileName}";
                }
                catch (Exception ex)
                {
                    return StatusCode(500, new { Message = "An error occurred while uploading the image.", Error = ex.Message });
                }
            }

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                return BadRequest(new { Message = "Failed to update account.", Errors = result.Errors });
            }

            return Ok(new { Message = "Account updated successfully.", ImagePath = user.ImagePath });
        }



    }
}
