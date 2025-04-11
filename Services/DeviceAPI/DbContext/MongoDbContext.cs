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

        public IMongoCollection<Device> Devices => _database.GetCollection<Device>("Devices");
        //public IMongoCollection<ApplicationUser> Users => _database.GetCollection<ApplicationUser>("Users");
        //public IMongoCollection<ApplicationRole> Roles => _database.GetCollection<ApplicationRole>("Roles");

        //public IMongoCollection<GeolocationModel> UserGeolocation => _database.GetCollection<GeolocationModel>("User_Geolocation");
        //public IMongoCollection<ActivityLogsModel> UserLogs => _database.GetCollection<ActivityLogsModel>("User_Logs");


    }

}
