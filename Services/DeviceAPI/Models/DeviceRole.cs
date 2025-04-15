using AspNetCore.Identity.MongoDbCore.Models;

namespace DeviceAPI.Models
{
    public class DeviceRole : MongoIdentityRole<Guid>
    {
        public string Description { get; set; }
    }
}
