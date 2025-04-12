using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace DeviceAPI.Models
{
    public class DeviceShadow
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public string DeviceId { get; set; }
        public BsonDocument ReportedState { get; set; }
        public BsonDocument DesiredState { get; set; }
        public BsonDocument Metadata { get; set; }
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }
}
