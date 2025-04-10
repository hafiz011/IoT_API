using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using System.Threading.Tasks;
using System.Linq;
using UserRoleAPI.Models;
using Microsoft.IdentityModel.Tokens;
using UserRoleAPI.Models;

namespace WebApp.Controllers
{
    [Authorize(Roles = "Admin")]
    [Route("api/[controller]")]
    [ApiController]
    public class AdminController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;

        public AdminController(UserManager<ApplicationUser> userManager,
            RoleManager<ApplicationRole> roleManager
            )
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        // GET: api/admin/users
        [HttpGet("users")]
        public IActionResult GetUsers()
        {
            var users = _userManager.Users.Select(user => new
            {
                user.Id,
                user.FirstName,
                user.LastName,
                user.Email,
                user.PhoneNumber,
                user.LockoutEnd
            }).ToList();

            return Ok(users);
        }


        // GET: api/admin/user/{id}
        [HttpGet("user/{id}")]
        public async Task<IActionResult> GetUserById(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return NotFound(new { Message = "User not found" });

            return Ok(new
            {
                user.Id,
                user.FirstName,
                user.LastName,
                user.Email,
                user.PhoneNumber,
                user.UserName,
                user.Address.address,
                user.Address.City,
                user.Address.Country,
                user.Roles,
                user.LockoutEnd,
                user.CreatedOn
            });
        }

        // POST: api/admin/create-role
        [HttpPost("create-role")]
        public async Task<IActionResult> CreateRole([FromQuery] string roleName, [FromQuery] string description)
        {
            if (await _roleManager.RoleExistsAsync(roleName))
                return BadRequest(new { Message = "Role already exists" });

            var role = new ApplicationRole
            {
                Name = roleName,
                Description = description
            };

            var result = await _roleManager.CreateAsync(role);
            if (!result.Succeeded)
                return BadRequest(new { Errors = result.Errors });

            return Ok(new { Message = "Role created successfully" });
        }


        // POST: api/admin/assign-role
        [HttpPost("assign-role")]
        public async Task<IActionResult> AssignRoleToUser([FromQuery] string userId, [FromQuery] string roleName)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return NotFound(new { Message = "User not found" });

            if (!await _roleManager.RoleExistsAsync(roleName))
                return NotFound(new { Message = "Role not found" });

            var result = await _userManager.AddToRoleAsync(user, roleName);
            if (result.Succeeded)
                return Ok(new { Message = "Role assigned successfully" });

            return BadRequest(result.Errors);
        }

        // POST: api/admin/remove-role
        [HttpPost("remove-role")]
        public async Task<IActionResult> RemoveRoleFromUser([FromQuery] string userId, [FromQuery] string roleName)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return NotFound(new { Message = "User not found" });

            if (!await _userManager.IsInRoleAsync(user, roleName))
                return BadRequest(new { Message = "User does not have this role." });

            var result = await _userManager.RemoveFromRoleAsync(user, roleName);
            if (!result.Succeeded)
                return BadRequest(new { Errors = result.Errors });

            return Ok(new { Message = "Role removed from user." });
        }



        // POST: api/admin/disable-login
        [HttpPost("disable-login")]
        public async Task<IActionResult> DisableLogin([FromQuery] string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound(new { Message = "User not found." });
            }

            user.LockoutEnd = DateTimeOffset.MaxValue;
            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                return Ok(new { Message = $"User '{user.UserName}' login has been disabled." });
            }

            return BadRequest(new { Message = $"Failed to disable login for user '{user.UserName}'." });
        }

        // POST: api/admin/enable-login
        [HttpPost("enable-login")]
        public async Task<IActionResult> EnableLogin([FromQuery] string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound(new { Message = "User not found." });
            }

            user.LockoutEnd = null;
            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                return Ok(new { Message = $"User '{user.UserName}' login has been enabled." });
            }

            return BadRequest(new { Message = $"Failed to enable login for user '{user.UserName}'." });
        }

        // DELETE: api/admin/delete-user/{id}
        [HttpDelete("delete-user/{id}")]
        public async Task<IActionResult> DeleteUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return NotFound(new { Message = "User not found" });

            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
                return BadRequest(result.Errors);

            return Ok(new { Message = "User deleted successfully" });
        }


    }

}