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

        public IMongoCollection<DeviceType> DeviceTypes => _database.GetCollection<DeviceType>("IoT_Device_Types");
        public IMongoCollection<DeviceGroup> DeviceGroups => _database.GetCollection<DeviceGroup>("IoT_Device_Groups");
        public IMongoCollection<DeviceShadow> DeviceShadows => _database.GetCollection<DeviceShadow>("IoT_Device_Shadows");
        public IMongoCollection<Firmware> Firmwares => _database.GetCollection<Firmware>("Firmwares");
        public IMongoCollection<FirmwareUpdate> FirmwareUpdates => _database.GetCollection<FirmwareUpdate>("Firmware_Updates");

    }

}
