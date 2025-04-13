using DeviceAPI.Authentication;
using DeviceAPI.DbContext;
using Microsoft.AspNetCore.Authentication.Certificate;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
using System.Security.Cryptography.X509Certificates;
using System.Text;



var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.Configure<MongoDbSettings>(builder.Configuration.GetSection("MongoDbSettings"));
builder.Services.AddSingleton<MongoDbContext>();

// Replace the MongoDbContext registration with:
builder.Services.AddSingleton<IMongoClient>(sp =>
    new MongoClient(builder.Configuration.GetSection("MongoDbSettings").GetValue<string>("ConnectionString")));

builder.Services.AddScoped<IMongoDatabase>(sp =>
    sp.GetRequiredService<IMongoClient>()
     .GetDatabase(builder.Configuration.GetSection("MongoDbSettings").GetValue<string>("DatabaseName")));

// Add certificate services
builder.Services.Configure<CertificateSettings>(builder.Configuration.GetSection("CertificateSettings"));
builder.Services.AddScoped<ICertificateAuthService, CertificateAuthService>();

// Add authentication services
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = "Hybrid";
    options.DefaultChallengeScheme = "Hybrid";
})

//.AddCertificate("Certificate", options =>
//{
//    options.AllowedCertificateTypes = CertificateTypes.All;
//    options.RevocationMode = X509RevocationMode.NoCheck; // Set to Online for production
//})

.AddCertificate("Certificate", options =>
{
    options.AllowedCertificateTypes = CertificateTypes.All;
    options.RevocationMode = X509RevocationMode.Online; // For production
})
.AddJwtBearer("JWT", options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"])),
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
})
.AddPolicyScheme("Hybrid", "Hybrid", options =>
{
    options.ForwardDefaultSelector = context =>
    {
        var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
        if (authHeader?.StartsWith("Bearer ") == true)
            return "JWT";

        if (context.Connection.ClientCertificate != null)
            return "Certificate";

        return "JWT"; // Fallback
    };
});

// Register services
builder.Services.AddScoped<CertificateAuthService>();
builder.Services.AddScoped<JwtService>();


builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
