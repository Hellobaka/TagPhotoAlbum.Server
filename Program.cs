using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using TagPhotoAlbum.Server.Data;
using TagPhotoAlbum.Server.Models;
using TagPhotoAlbum.Server.Services;
using NLog;
using NLog.Web;

var builder = WebApplication.CreateBuilder(args);

// Configure NLog
builder.Logging.ClearProviders();
builder.Host.UseNLog();

// Configure logging levels
builder.Logging.AddFilter("Microsoft.EntityFrameworkCore", Microsoft.Extensions.Logging.LogLevel.Warning);
builder.Logging.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", Microsoft.Extensions.Logging.LogLevel.Warning);
builder.Logging.AddFilter("Microsoft.AspNetCore.StaticFiles", Microsoft.Extensions.Logging.LogLevel.Warning);
builder.Logging.AddFilter("Microsoft.AspNetCore.Routing", Microsoft.Extensions.Logging.LogLevel.Warning);
builder.Logging.AddFilter("Microsoft.AspNetCore.Mvc", Microsoft.Extensions.Logging.LogLevel.Warning);
builder.Logging.AddFilter("Microsoft.AspNetCore.Hosting", Microsoft.Extensions.Logging.LogLevel.Warning);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Add Entity Framework with SQLite
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add JWT Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? ""))
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddControllers();

// Configure PhotoStorage options
builder.Services.Configure<PhotoStorageOptions>(
    builder.Configuration.GetSection("PhotoStorage"));

// Configure ImageCompression options
builder.Services.Configure<ImageCompressionOptions>(
    builder.Configuration.GetSection("ImageCompression"));

// Register services
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<PasskeyService>();
builder.Services.AddScoped<PhotoStorageService>();
builder.Services.AddScoped<ImageCompressionService>();
builder.Services.AddScoped<ExifService>();
builder.Services.AddHostedService<PhotoSyncService>();
builder.Services.AddHostedService<ConsoleCommandService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// Enable static file serving for uploads directory
app.UseStaticFiles();

// Enable static file serving for external storage
var externalStoragePaths = builder.Configuration.GetSection("PhotoStorage:ExternalStoragePaths").Get<string[]>() ?? Array.Empty<string>();
foreach (var storagePath in externalStoragePaths)
{
    if (!string.IsNullOrEmpty(storagePath) && Directory.Exists(storagePath))
    {
        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new PhysicalFileProvider(storagePath),
            RequestPath = "/external"
        });
    }
}

app.UseCors();

app.UseAuthentication();
app.UseAuthorization();

// Initialize database
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();
    var logger = loggerFactory.CreateLogger("SeedData");
    context.Database.EnsureCreated();
    SeedData.Initialize(logger, context);
}

app.MapControllers();

app.Run();
