using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace DeviceAPI.Models
{
    public class DeviceGroup
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public List<string> DeviceIds { get; set; } // References to Device collection
        public Dictionary<string, string> Metadata { get; set; } // Flexible key-value pairs
        public string GroupType { get; set; } // e.g., "Location", "Function", "Customer"
    }
}
