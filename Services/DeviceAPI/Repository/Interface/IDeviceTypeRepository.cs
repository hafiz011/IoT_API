using DeviceAPI.Models;

namespace DeviceAPI.Repository.Interface
{
    public interface IDeviceTypeRepository
    {
        Task<DeviceType> CreateAsync(DeviceType deviceType);
        Task<DeviceType> GetByIdAsync(string id);
        Task<List<DeviceType>> GetAllAsync();
        Task UpdateAsync(string id, DeviceType deviceType);
        Task DeleteAsync(string id);
        Task<bool> ExistsAsync(string id);
    }
}
