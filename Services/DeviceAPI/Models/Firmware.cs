using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace DeviceAPI.Models
{
    public class Firmware
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public string Version { get; set; }
        public string DeviceTypeId { get; set; }
        public string DownloadUrl { get; set; }
        public string Checksum { get; set; }
        public bool IsCritical { get; set; } = false;
        public string ReleaseNotes { get; set; }
        public DateTime ReleasedAt { get; set; } = DateTime.UtcNow;
        public string MinHardwareVersion { get; set; }
        public string MaxHardwareVersion { get; set; }
    }
}
