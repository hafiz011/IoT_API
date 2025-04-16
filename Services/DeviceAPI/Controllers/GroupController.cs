using DeviceAPI.Models;
using DeviceAPI.Repository.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace DeviceAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GroupController : ControllerBase
    {
        private readonly IDeviceGroupRepository _groupRepository;
        private readonly ILogger<GroupController> _logger;

        public GroupController(
            IDeviceGroupRepository groupRepository,
            ILogger<GroupController> logger)
        {
            _groupRepository = groupRepository;
            _logger = logger;
        }

        public class CreateGroupRequest
        {
            public string Name { get; set; }
            public string Description { get; set; }
            public Dictionary<string, string> Metadata { get; set; }
            public string GroupType { get; set; }
        }

        public class AssignDeviceRequest
        {
            public string GroupId { get; set; }
            public string DeviceId { get; set; }
        }

        public class UpdateGroupRequest
        {
            public string GroupId { get; set; }
            public string Name { get; set; }
            public string Description { get; set; }
        }

        /// <summary>
        /// Create a new device group
        /// </summary>
        /// <param name="request">Group creation details</param>
        /// <returns>Newly created group</returns>
        [HttpPost("create")]
        [ProducesResponseType(typeof(DeviceGroup), (int)HttpStatusCode.Created)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> CreateGroup([FromBody] CreateGroupRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Name))
                {
                    return BadRequest(new { Message = "Group name is required" });
                }

                var group = new DeviceGroup
                {
                    Name = request.Name,
                    Description = request.Description,
                    Metadata = request.Metadata ?? new Dictionary<string, string>(),
                    GroupType = request.GroupType
                };

                await _groupRepository.CreateGroupAsync(group);
                return CreatedAtAction(nameof(GetGroupDetails), new { groupId = group.Id }, group);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating group");
                return StatusCode(500, new { Message = "An error occurred while creating the group" });
            }
        }

        /// <summary>
        /// Get list of all groups (Admin only)
        /// </summary>
        /// <returns>List of all device groups</returns>
        [HttpGet("list")]
        //[Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(List<DeviceGroup>), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetAllGroups()
        {
            try
            {
                var groups = await _groupRepository.GetAllGroupsAsync();
                return Ok(groups);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all groups");
                return StatusCode(500, new { Message = "An error occurred while retrieving groups" });
            }
        }

        /// <summary>
        /// Get details of a specific group
        /// </summary>
        /// <param name="groupId">ID of the group to retrieve</param>
        /// <returns>Group details</returns>
        [HttpGet("details")]
        [ProducesResponseType(typeof(DeviceGroup), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> GetGroupDetails([FromQuery] string groupId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(groupId))
                {
                    return BadRequest(new { Message = "Group ID is required" });
                }

                var group = await _groupRepository.GetGroupByIdAsync(groupId);
                if (group == null)
                {
                    return NotFound(new { Message = "Group not found" });
                }

                return Ok(group);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting group details for {GroupId}", groupId);
                return StatusCode(500, new { Message = "An error occurred while retrieving group details" });
            }
        }

        /// <summary>
        /// Update group information
        /// </summary>
        /// <param name="request">Group update details</param>
        /// <returns></returns>
        [HttpPut("update")]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> Update([FromBody] UpdateGroupRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.GroupId))
                {
                    return BadRequest(new { Message = "Group ID is required" });
                }

                var existingGroup = await _groupRepository.GetGroupByIdAsync(request.GroupId);
                if (existingGroup == null)
                {
                    return NotFound(new { Message = "Group not found" });
                }

                existingGroup.Name = request.Name;
                existingGroup.Description = request.Description;

                await _groupRepository.UpdateGroupAsync(existingGroup);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating group {GroupId}", request?.GroupId);
                return StatusCode(500, new { Message = "An error occurred while updating the group" });
            }
        }

        /// <summary>
        /// Delete a group
        /// </summary>
        /// <param name="groupId">ID of the group to delete</param>
        /// <returns></returns>
        [HttpDelete("delete")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> Delete([FromQuery] string groupId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(groupId))
                {
                    return BadRequest(new { Message = "Group ID is required" });
                }

                var group = await _groupRepository.GetGroupByIdAsync(groupId);
                if (group == null)
                {
                    return NotFound(new { Message = "Group not found" });
                }

                if (group.DeviceIds?.Count > 0)
                {
                    return BadRequest(new { Message = "Cannot delete group with assigned devices" });
                }

                await _groupRepository.DeleteGroupAsync(groupId);
                return Ok(new { Message = "Group deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting group {GroupId}", groupId);
                return StatusCode(500, new { Message = "An error occurred while deleting the group" });
            }
        }

        /// <summary>
        /// Assign device to a group
        /// </summary>
        /// <param name="request">Assignment details</param>
        /// <returns></returns>
        [HttpPost("assign-device")]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> AssignDevice([FromBody] AssignDeviceRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.GroupId) || string.IsNullOrWhiteSpace(request.DeviceId))
                {
                    return BadRequest(new { Message = "Group ID and Device ID are required" });
                }

                await _groupRepository.AssignDeviceToGroupAsync(request.DeviceId, request.GroupId);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Device or group not found");
                return NotFound(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning device {DeviceId} to group {GroupId}",
                    request.DeviceId, request.GroupId);
                return StatusCode(500, new { Message = "An error occurred while assigning device to group" });
            }
        }

        /// <summary>
        /// Get all devices in a group
        /// </summary>
        /// <param name="groupId">ID of the group</param>
        /// <returns>List of devices in the group</returns>
        [HttpGet("devices")]
        [ProducesResponseType(typeof(List<Device>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> GetDevicesInGroup([FromQuery] string groupId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(groupId))
                {
                    return BadRequest(new { Message = "Group ID is required" });
                }

                var devices = await _groupRepository.GetDevicesByGroupIdAsync(groupId);
                return Ok(devices);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting devices for group {GroupId}", groupId);
                return StatusCode(500, new { Message = "An error occurred while retrieving devices" });
            }
        }


        [HttpPost("remove-device")]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> RemoveDeviceFromGroup([FromBody] AssignDeviceRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.GroupId) || string.IsNullOrWhiteSpace(request.DeviceId))
                {
                    return BadRequest(new { Message = "Group ID and Device ID are required" });
                }


                await _groupRepository.RemoveDeviceFromGroupAsync(request.DeviceId, request.GroupId);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing device {DeviceId} from group {GroupId}",
                    request.DeviceId, request.GroupId);
                return StatusCode(500, new { Message = "An error occurred while removing device from group" });
            }
        }


    }
}