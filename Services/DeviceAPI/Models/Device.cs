
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace DeviceAPI.Models
{
    public class Device
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public string DeviceId { get; set; } // Custom unique identifier
        public string Name { get; set; }
        public string DeviceTypeId { get; set; }
        public string FirmwareVersion { get; set; }
        public string HardwareVersion { get; set; }
        public string SerialNumber { get; set; }
        public string MacAddress { get; set; }
        public string IpAddress { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastSeenAt { get; set; }
        public DeviceStatus Status { get; set; } = DeviceStatus.Offline;
        public bool IsActive { get; set; } = true;

        // Authentication info (embedded document)
        public DeviceAuthentication Authentication { get; set; }

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

    public class DeviceAuthentication
    {
        [BsonElement("method")]
        public string Method { get; set; } // "Certificate", "JWT", or "Hybrid"

        [BsonElement("certificateId")]
        public string CertificateId { get; set; } // Thumbprint or unique identifier

        [BsonElement("encryptedPrivateKey")]
        public string EncryptedPrivateKey { get; set; } // For certificate auth
        public string Credentials { get; set; }

        [BsonElement("token")]
        public string Token { get; set; } // JWT token

        [BsonElement("tokenExpiresAt")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime? TokenExpiresAt { get; set; }

        [BsonElement("lastRotatedAt")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime? LastRotatedAt { get; set; }
    }


    public class DeviceLocation
    {
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public string Address { get; set; }
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }
}
