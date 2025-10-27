using System.Security.Cryptography;
using System.Text;
using TagPhotoAlbum.Server.Models;
using TagPhotoAlbum.Server.Services;

namespace TagPhotoAlbum.Server.Data;

public static class SeedData
{
    public static void Initialize(ILogger _logger,
        AppDbContext context)
    {
        if (context.Users.Any() || context.Photos.Any())
        {
            _logger.LogInformation("数据库已包含数据，跳过种子数据初始化");
            return; // Database has been seeded
        }

        string password = Guid.NewGuid().ToString("N");
        // Add default user
        var user = new User
        {
            Username = "admin",
            Name = "Administrator",
            Email = "admin@example.com",
            PasswordHash = Convert.ToBase64String(SHA256.HashData(Encoding.ASCII.GetBytes(password)))
        };
        context.Users.Add(user);
        context.SaveChanges();
        _logger.LogInformation("初始用户已创建。用户名：admin; 密码：{Password}", password);
    }
}