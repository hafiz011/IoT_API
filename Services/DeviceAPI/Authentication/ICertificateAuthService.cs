using DeviceAPI.Models;
using System.Security.Cryptography.X509Certificates;

namespace DeviceAPI.Authentication
{
    public interface ICertificateAuthService
    {
        Task<Device> ValidateCertificateAsync(X509Certificate2 certificate);
        Task<Device> AuthenticateDeviceAsync(X509Certificate2 certificate);
        Task<DeviceCertificateResponse> CreateCertificateAsync(Device device);
        Task RevokeCertificateAsync(string deviceId);
    }
}
