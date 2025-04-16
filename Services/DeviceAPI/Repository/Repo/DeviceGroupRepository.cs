using DeviceAPI.Models;
using DeviceAPI.Repository.Interface;
using Microsoft.AspNetCore.Identity;
using MongoDB.Driver;

namespace DeviceAPI.Repository.Implementation
{
    public class DeviceGroupRepository : IDeviceGroupRepository
    {
        private readonly IMongoCollection<DeviceGroup> _groupsCollection;
        private readonly UserManager<Device> _devicesCollection;

        public DeviceGroupRepository(IMongoDatabase database, UserManager<Device> userManager)
        {
            _groupsCollection = database.GetCollection<DeviceGroup>("IoT_Device_Groups");
            _devicesCollection = userManager;
        }

        public async Task CreateGroupAsync(DeviceGroup group)
        {
            if (group == null)
                throw new ArgumentNullException(nameof(group));

            group.CreatedAt = DateTime.UtcNow;
            group.UpdatedAt = DateTime.UtcNow;

            // Initialize device list if null
            group.DeviceIds ??= new List<string>();

            await _groupsCollection.InsertOneAsync(group);
        }

        public async Task<List<DeviceGroup>> GetAllGroupsAsync()
        {
            return await _groupsCollection.Find(_ => true).ToListAsync();
        }

        public async Task<DeviceGroup> GetGroupByIdAsync(string groupId)
        {
            if (string.IsNullOrWhiteSpace(groupId))
                throw new ArgumentException("Group ID cannot be empty", nameof(groupId));

            return await _groupsCollection.Find(g => g.Id == groupId).FirstOrDefaultAsync();
        }

        public async Task UpdateGroupAsync(DeviceGroup group)
        {
            if (group == null)
                throw new ArgumentNullException(nameof(group));

            if (string.IsNullOrWhiteSpace(group.Id))
                throw new ArgumentException("Group must have an ID", nameof(group));

            // Update the timestamp
            group.UpdatedAt = DateTime.UtcNow;

            var result = await _groupsCollection.ReplaceOneAsync(
                g => g.Id == group.Id,
                group);

            if (result.MatchedCount == 0)
                throw new KeyNotFoundException($"Group with ID {group.Id} not found");
        }

        public async Task DeleteGroupAsync(string groupId)
        {
            if (string.IsNullOrWhiteSpace(groupId))
                throw new ArgumentException("Group ID cannot be empty", nameof(groupId));

            var result = await _groupsCollection.DeleteOneAsync(g => g.Id == groupId);

            if (result.DeletedCount == 0)
                throw new KeyNotFoundException($"Group with ID {groupId} not found");
        }

        public async Task<List<DeviceGroup>> GetDevicesByGroupIdAsync(string groupId)
        {
            // This method name seems misleading - it returns groups but name suggests devices
            // Assuming you want to get a single group with its devices

            var group = await _groupsCollection.Find(g => g.Id == groupId).FirstOrDefaultAsync();
            return group != null ? new List<DeviceGroup> { group } : new List<DeviceGroup>();
        }

        public async Task AssignDeviceToGroupAsync(string deviceId, string groupId)
        {
            if (string.IsNullOrWhiteSpace(deviceId))
                throw new ArgumentException("Device ID cannot be empty", nameof(deviceId));

            if (string.IsNullOrWhiteSpace(groupId))
                throw new ArgumentException("Group ID cannot be empty", nameof(groupId));

            // Verify device exists
            var deviceExists = await _devicesCollection.FindByNameAsync(deviceId);
            if (deviceExists.DeviceId == null)
                throw new KeyNotFoundException($"Device with ID {deviceId} not found");

            // Add device to group's device list
            var update = Builders<DeviceGroup>.Update
                .AddToSet(g => g.DeviceIds, deviceId)
                .Set(g => g.UpdatedAt, DateTime.UtcNow);

            var result = await _groupsCollection.UpdateOneAsync(
                g => g.Id == groupId,
                update);

            if (result.MatchedCount == 0)
                throw new KeyNotFoundException($"Group with ID {groupId} not found");
        }

        // Additional helpful methods you might want to add:

        public async Task<bool> GroupExistsAsync(string groupId)
        {
            return await _groupsCollection.Find(g => g.Id == groupId).AnyAsync();
        }

        public async Task RemoveDeviceFromGroupAsync(string deviceId, string groupId)
        {
           var group = await _groupsCollection.Find(g => g.Id == groupId).FirstOrDefaultAsync();
            if (group == null)
            {
                throw new KeyNotFoundException($"Group with ID {groupId} not found");
            }
            var update = Builders<DeviceGroup>.Update
                .Pull(g => g.DeviceIds, deviceId)
                .Set(g => g.UpdatedAt, DateTime.UtcNow);

            await _groupsCollection.UpdateOneAsync(g => g.Id == groupId, update);
        }

        //public async Task<List<Device>> GetDevicesInGroupAsync(string groupId)
        //{
        //    var group = await GetGroupByIdAsync(groupId);
        //    if (group == null || group.DeviceIds == null || !group.DeviceIds.Any())
        //        return new List<Device>();

        //    var filter = Builders<Device>.Filter.In(d => d.DeviceId, group.DeviceIds);
        //    return await _devicesCollection.FindByNameAsync(filter.DeviceId);
        //}
    }
}