using Microsoft.Extensions.Options;
using DeviceAPI.Models;
using MongoDB.Driver;
namespace DeviceAPI.DbContext
{
    public class MongoDbContext
    {
        private readonly IMongoDatabase _database;

        public MongoDbContext(IOptions<MongoDbSettings> settings)
        {
            var client = new MongoClient(settings.Value.ConnectionString);
            _database = client.GetDatabase(settings.Value.DatabaseName);
        }

        public IMongoCollection<Device> Devices => _database.GetCollection<Device>("devices");

        public IMongoCollection<DeviceType> DeviceTypes => _database.GetCollection<DeviceType>("device_Types");
        public IMongoCollection<DeviceGroup> DeviceGroups => _database.GetCollection<DeviceGroup>("device_Groups");
        public IMongoCollection<DeviceShadow> DeviceShadows => _database.GetCollection<DeviceShadow>("device_Shadows");
        public IMongoCollection<Firmware> Firmwares => _database.GetCollection<Firmware>("firmwares");
        public IMongoCollection<FirmwareUpdate> FirmwareUpdates => _database.GetCollection<FirmwareUpdate>("firmware_Updates");

    }

}
