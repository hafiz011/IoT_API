using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace DeviceAPI.Models
{
    public class FirmwareUpdate
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public string DeviceId { get; set; }
        public string FirmwareId { get; set; }
        public FirmwareUpdateStatus Status { get; set; } = FirmwareUpdateStatus.Pending;
        public DateTime InitiatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; set; }
        public string ErrorMessage { get; set; }
    }

    public enum FirmwareUpdateStatus
    {
        Pending,
        Downloading,
        Updating,
        Success,
        Failed
    }
}
