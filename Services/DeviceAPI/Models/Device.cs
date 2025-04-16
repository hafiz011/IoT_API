
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using AspNetCore.Identity.MongoDbCore.Models;

namespace DeviceAPI.Models
{
    public class Device : MongoIdentityUser<Guid>
    {
        public string DeviceId { get; set; }
        public string Name { get; set; }
        public string DeviceTypeId { get; set; }
        public string FirmwareVersion { get; set; }
        public string FirmwareUpdateVersion { get; set; }
        public string HardwareVersion { get; set; }
        public string SerialNumber { get; set; }
        public string MacAddress { get; set; }
        public string IpAddress { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastSeenAt { get; set; }
        public DeviceStatus Status { get; set; } = DeviceStatus.Offline;
        public bool IsActive { get; set; } = true;

        // Location (embedded document)
        public DeviceLocation Location { get; set; }

        // Tags
        public Dictionary<string, string> Tags { get; set; } = new();

        // Groups
        public List<string> GroupIds { get; set; } = new();
    }

    public enum DeviceStatus
    {
        Online,
        Offline,
        Maintenance,
        Retired
    }

    public class DeviceLocation
    {
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public string Address { get; set; }
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }
}
