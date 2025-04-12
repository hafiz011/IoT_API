using DeviceAPI.DbContext;
using DeviceAPI.Models;
using MongoDB.Driver;
using System.Security.Cryptography.X509Certificates;

namespace DeviceAPI.Authentication
{

    public class CertificateAuthService
    {
        private readonly IMongoCollection<Device> _devices;
        private readonly ILogger<CertificateAuthService> _logger;

        public CertificateAuthService(MongoDbContext dbContext, ILogger<CertificateAuthService> logger)
        {
            _devices = dbContext.Devices;
            _logger = logger;
        }

        public async Task<Device> ValidateCertificateAsync(X509Certificate2 certificate)
        {
            try
            {
                // Basic certificate validation
                if (certificate == null || !certificate.Verify())
                {
                    _logger.LogWarning("Invalid certificate presented");
                    return null;
                }

                // Check against database
                return await _devices.Find(d =>
                    d.Authentication.CertificateId == certificate.Thumbprint &&
                    d.IsActive)
                    .FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Certificate validation failed");
                return null;
            }
        }
    }
}
