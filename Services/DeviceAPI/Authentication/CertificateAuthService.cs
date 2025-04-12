using DeviceAPI.DbContext;
using DeviceAPI.Models;
using MongoDB.Driver;
using System.Security.Cryptography;
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

        public async Task<Device> AuthenticateDeviceAsync(X509Certificate2 certificate)
        {
            var device = await ValidateCertificateAsync(certificate);
            if (device == null)
            {
                _logger.LogWarning("Device authentication failed");
                return null;
            }

            // Update last seen timestamp
            device.LastSeenAt = DateTime.UtcNow;
            await _devices.ReplaceOneAsync(d => d.Id == device.Id, device);

            return device;
        }

        public async Task<Device> CreateCertificateAsync(Device device)
        {
            try
            {
                // Generate a new RSA key pair
                using var rsa = RSA.Create(2048);

                // Create certificate request
                var request = new CertificateRequest(
                    new X500DistinguishedName($"CN={device.DeviceId}"),
                    rsa,
                    HashAlgorithmName.SHA256,
                    RSASignaturePadding.Pkcs1);

                // Add basic constraints
                request.CertificateExtensions.Add(
                    new X509BasicConstraintsExtension(
                        certificateAuthority: false,
                        hasPathLengthConstraint: false,
                        pathLengthConstraint: 0,
                        critical: true));

                // Add key usage
                request.CertificateExtensions.Add(
                    new X509KeyUsageExtension(
                        X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment,
                        critical: true));

                // Create self-signed certificate (valid for 1 year)
                var certificate = request.CreateSelfSigned(
                    DateTimeOffset.UtcNow,
                    DateTimeOffset.UtcNow.AddYears(1));

                // Export to PKCS#12 format with password protection
                var certificateBytes = certificate.Export(X509ContentType.Pfx, "1OTprojectSecurePassword333666999.com");
                var publicKeyBytes = certificate.Export(X509ContentType.Cert);

                // Update device with certificate information
                device.Authentication ??= new DeviceAuthentication();
                device.Authentication.Method = "Certificate";
                device.Authentication.CertificateId = certificate.Thumbprint;
                device.Authentication.Credentials = Convert.ToBase64String(certificateBytes);
                device.Authentication.LastRotatedAt = DateTime.UtcNow;

                // Save to database
                var filter = Builders<Device>.Filter.Eq(d => d.DeviceId, device.DeviceId);
                var update = Builders<Device>.Update
                    .Set(d => d.Authentication, device.Authentication)
                    .Set(d => d.LastSeenAt, DateTime.UtcNow);

                await _devices.UpdateOneAsync(filter, update);

                return device;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create certificate for device {DeviceId}", device.DeviceId);
                throw; // Or return null if you prefer to handle errors silently
            }
        }

    }
}
