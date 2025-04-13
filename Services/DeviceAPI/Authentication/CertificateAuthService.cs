using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using DeviceAPI.DbContext;
using DeviceAPI.Models;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace DeviceAPI.Authentication
{


    public class CertificateAuthService : ICertificateAuthService
    {
        private readonly IMongoCollection<Device> _devices;
        private readonly ILogger<CertificateAuthService> _logger;
        private readonly CertificateSettings _settings;

        public CertificateAuthService(
            MongoDbContext dbContext,
            ILogger<CertificateAuthService> logger,
            IOptions<CertificateSettings> settings)
        {
            _devices = dbContext.Devices;
            _logger = logger;
            _settings = settings.Value;
        }

        public async Task<Device> ValidateCertificateAsync(X509Certificate2 certificate)
        {
            try
            {
                if (certificate == null)
                {
                    _logger.LogWarning("Null certificate presented");
                    return null;
                }

                // Enhanced certificate validation
                if (!certificate.Verify() ||
                    certificate.NotAfter < DateTime.UtcNow ||
                    certificate.NotBefore > DateTime.UtcNow)
                {
                    _logger.LogWarning("Invalid certificate presented (thumbprint: {Thumbprint})",
                        certificate.Thumbprint);
                    return null;
                }

                return await _devices.Find(d =>
                    d.Authentication.CertificateId == certificate.Thumbprint &&
                    d.IsActive &&
                    d.Status != DeviceStatus.Retired)
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
            if (device == null) return null;

            // Update device status
            var update = Builders<Device>.Update
                .Set(d => d.LastSeenAt, DateTime.UtcNow)
                .Set(d => d.Status, DeviceStatus.Online);

            await _devices.UpdateOneAsync(d => d.Id == device.Id, update);
            return device;
        }

        public async Task<DeviceCertificateResponse> CreateCertificateAsync(Device device)
        {
            if (device == null) throw new ArgumentNullException(nameof(device));

            try
            {
                // Check for existing certificate
                var existingCert = await _devices.Find(d => d.DeviceId == device.DeviceId)
                    .Project(d => d.Authentication)
                    .FirstOrDefaultAsync();

                if (existingCert?.CertificateId != null)
                {
                    throw new InvalidOperationException(
                        $"Device {device.DeviceId} already has certificate {existingCert.CertificateId}");
                }

                // Generate certificate
                using var rsa = RSA.Create(_settings.KeySize);
                var password = GenerateSecurePassword();

                var request = new CertificateRequest(
                    new X500DistinguishedName($"CN={device.DeviceId},OU=IoT,O=DreamTech"),
                    rsa,
                    HashAlgorithmName.SHA256,
                    RSASignaturePadding.Pkcs1);

                AddCertificateExtensions(request);

                var certificate = request.CreateSelfSigned(
                    DateTimeOffset.UtcNow.AddDays(-1), // Backdate slightly
                    DateTimeOffset.UtcNow.AddMonths(_settings.ValidityMonths));

                // Store certificate data
                var response = await StoreCertificateData(device, certificate, rsa, password);

                _logger.LogInformation("Created certificate for device {DeviceId} (thumbprint: {Thumbprint})",
                    device.DeviceId, certificate.Thumbprint);

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create certificate for device {DeviceId}", device.DeviceId);
                throw new CertificateOperationException("Certificate creation failed", ex);
            }
        }

        public async Task RevokeCertificateAsync(string deviceId)
        {
            var update = Builders<Device>.Update
                .Set(d => d.Authentication.CertificateId, null)
                .Set(d => d.Authentication.EncryptedPrivateKey, null)
                .Set(d => d.Authentication.Credentials, null)
                .Set(d => d.Authentication.LastRotatedAt, DateTime.UtcNow);

            var result = await _devices.UpdateOneAsync(
                d => d.DeviceId == deviceId,
                update);

            if (result.MatchedCount == 0)
            {
                throw new KeyNotFoundException($"Device {deviceId} not found");
            }

            _logger.LogInformation("Revoked certificate for device {DeviceId}", deviceId);
        }

        private void AddCertificateExtensions(CertificateRequest request)
        {
            // Basic constraints
            request.CertificateExtensions.Add(
                new X509BasicConstraintsExtension(
                    certificateAuthority: false,
                    hasPathLengthConstraint: false,
                    pathLengthConstraint: 0,
                    critical: true));

            // Key usage
            request.CertificateExtensions.Add(
                new X509KeyUsageExtension(
                    X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment,
                    critical: true));

            // Subject Alternative Name (SAN)
            if (_settings.AddSubjectAlternativeName)
            {
                var sanBuilder = new SubjectAlternativeNameBuilder();
                sanBuilder.AddDnsName(request.SubjectName.Name); // Adds CN as DNS name
                request.CertificateExtensions.Add(sanBuilder.Build());
            }
        }

        private async Task<DeviceCertificateResponse> StoreCertificateData(
            Device device,
            X509Certificate2 certificate,
            RSA rsa,
            string password)
        {
            // Export certificate data
            var publicCertBytes = certificate.Export(X509ContentType.Cert);
            var privateKeyBytes = rsa.ExportEncryptedPkcs8PrivateKey(
                password,
                new PbeParameters(
                    PbeEncryptionAlgorithm.Aes256Cbc,
                    HashAlgorithmName.SHA256,
                    iterationCount: 100_000));

            // Update device
            device.Authentication ??= new DeviceAuthentication();
            device.Authentication.Method = "Certificate";
            device.Authentication.CertificateId = certificate.Thumbprint;
            device.Authentication.EncryptedPrivateKey = Convert.ToBase64String(privateKeyBytes);
            device.Authentication.Credentials = Convert.ToBase64String(publicCertBytes);
            device.Authentication.LastRotatedAt = DateTime.UtcNow;

            var filter = Builders<Device>.Filter.Eq(d => d.DeviceId, device.DeviceId);
            var update = Builders<Device>.Update
                .Set(d => d.Authentication, device.Authentication)
                .Set(d => d.LastSeenAt, DateTime.UtcNow);

            await _devices.UpdateOneAsync(filter, update);

            return new DeviceCertificateResponse(
                device.DeviceId,
                certificate.Thumbprint,
                Convert.ToBase64String(publicCertBytes),
                certificate.NotAfter);
        }

        private string GenerateSecurePassword()
        {
            using var rng = RandomNumberGenerator.Create();
            var bytes = new byte[32];
            rng.GetBytes(bytes);
            return Convert.ToBase64String(bytes);
        }
    }

    public class CertificateSettings
    {
        public int KeySize { get; set; } = 2048;
        public int ValidityMonths { get; set; } = 12;
        public bool AddSubjectAlternativeName { get; set; } = true;
    }

    public class DeviceCertificateResponse
    {
        public string DeviceId { get; }
        public string Thumbprint { get; }
        public string PublicCertificate { get; }
        public DateTime Expiration { get; }

        public DeviceCertificateResponse(
            string deviceId,
            string thumbprint,
            string publicCertificate,
            DateTime expiration)
        {
            DeviceId = deviceId;
            Thumbprint = thumbprint;
            PublicCertificate = publicCertificate;
            Expiration = expiration;
        }
    }

    public class CertificateOperationException : Exception
    {
        public CertificateOperationException(string message, Exception innerException)
            : base(message, innerException) { }
    }
}