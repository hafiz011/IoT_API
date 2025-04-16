using DeviceAPI.Models;

namespace DeviceAPI.Repository.Interface
{
    public interface IDeviceGroupRepository
    {
        Task CreateGroupAsync(DeviceGroup group);
        Task<List<DeviceGroup>> GetAllGroupsAsync();
        Task<DeviceGroup> GetGroupByIdAsync(string groupId);
        Task UpdateGroupAsync(DeviceGroup group);
        Task DeleteGroupAsync(string groupId);
        Task<List<DeviceGroup>> GetDevicesByGroupIdAsync(string groupId);
        Task AssignDeviceToGroupAsync(string deviceId, string groupId);
        Task RemoveDeviceFromGroupAsync(string deviceId, string groupId);
    }
}
