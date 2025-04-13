using System.Security.Cryptography;

namespace DeviceAPI.Authentication
{
    public interface IKeyVaultService
    {
        Task<string> StorePrivateKeyAsync(RSA privateKey, string deviceId);
        Task<string> GeneratePasswordAsync(string deviceId);
        Task<RSA> GetPrivateKeyAsync(string keyId);
    }
}
