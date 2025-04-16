using DeviceAPI.Models;
using DeviceAPI.Repository.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using MongoDB.Driver;
using MongoDB.Bson;

namespace DeviceAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DeviceTypeController : ControllerBase
    {
        private readonly IDeviceTypeRepository _repository;
        private readonly ILogger<DeviceTypeController> _logger;

        public DeviceTypeController(
            IDeviceTypeRepository repository,
            ILogger<DeviceTypeController> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        [HttpPost("Create")]
        [ProducesResponseType(typeof(DeviceType), (int)HttpStatusCode.Created)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> Create([FromBody] DeviceTypeCreateDto dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Name))
                {
                    return BadRequest("Name is required");
                }

                var deviceType = new DeviceType
                {
                    Name = dto.Name,
                    Description = dto.Description,
                    Manufacturer = dto.Manufacturer,
                    ModelNumber = dto.ModelNumber,
                    Capabilities = dto.Capabilities
                };

                var created = await _repository.CreateAsync(deviceType);
                return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
            }
            catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
            {
                _logger.LogWarning(ex, "Duplicate device type name: {Name}", dto.Name);
                return Conflict($"Device type with name '{dto.Name}' already exists");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating device type");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(DeviceType), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> GetById(string id)
        {
            try
            {
                var deviceType = await _repository.GetByIdAsync(id);
                return deviceType != null ? Ok(deviceType) : NotFound();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting device type with ID {Id}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("GetAll")]
        [ProducesResponseType(typeof(List<DeviceType>), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var deviceTypes = await _repository.GetAllAsync();
                return Ok(deviceTypes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all device types");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPut("{id}")]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> Update(string id, [FromBody] DeviceTypeUpdateDto dto)
        {
            try
            {
                if (!await _repository.ExistsAsync(id))
                {
                    return NotFound();
                }

                var deviceType = new DeviceType
                {
                    Id = id,
                    Name = dto.Name,
                    Description = dto.Description,
                    Manufacturer = dto.Manufacturer,
                    ModelNumber = dto.ModelNumber,
                    Capabilities = dto.Capabilities
                };

                await _repository.UpdateAsync(id, deviceType);
                return NoContent();
            }
            catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
            {
                _logger.LogWarning(ex, "Duplicate device type name: {Name}", dto.Name);
                return Conflict($"Device type with name '{dto.Name}' already exists");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating device type with ID {Id}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpDelete("{id}")]
        //[Authorize(Roles = "Admin")]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> Delete(string id)
        {
            try
            {
                if (!await _repository.ExistsAsync(id))
                {
                    return NotFound();
                }

                await _repository.DeleteAsync(id);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting device type with ID {Id}", id);
                return StatusCode(500, "Internal server error");
            }
        }


        public class DeviceTypeCreateDto
        {
            public string Name { get; set; }
            public string Description { get; set; }
            public string Manufacturer { get; set; }
            public string ModelNumber { get; set; }
            public BsonDocument Capabilities { get; set; }
        }

        public class DeviceTypeUpdateDto
        {
            public string Name { get; set; }
            public string Description { get; set; }
            public string Manufacturer { get; set; }
            public string ModelNumber { get; set; }
            public BsonDocument Capabilities { get; set; }
        }

    }
}