using System.Security.Cryptography;
using System.Text;
using TagPhotoAlbum.Server.Models;
using TagPhotoAlbum.Server.Services;

namespace TagPhotoAlbum.Server.Data;

public static class SeedData
{
    public static void Initialize(ExifService exifService,
        ImageCompressionService compressionService,
        AppDbContext context,
        string[] externalStoragePaths)
    {
        if (context.Users.Any() || context.Photos.Any())
        {
            return; // Database has been seeded
        }

        // Add default user
        var user = new User
        {
            Username = "admin",
            Name = "Administrator",
            Email = "admin@example.com",
            PasswordHash = Convert.ToBase64String(SHA256.HashData(Encoding.ASCII.GetBytes("admin123")))
        };
        context.Users.Add(user);

        // Add sample photos
        var photos = new List<Photo>();
        foreach(var dir in externalStoragePaths)
        {
            if (Directory.Exists(dir))
            {
                var files = Directory.GetFiles(dir);
                string[] availableExtensions = [".jpg", ".jpeg", ".png", ".bmp", ".gif"];
                foreach (var file in files.Where(x => availableExtensions.Contains(Path.GetExtension(x).ToLower())))
                {
                    // 获取文件大小（单位：KB）
                    var fileInfo = new FileInfo(file);
                    var fileSizeKB = fileInfo.Exists ? Math.Round(fileInfo.Length / 1024.0, 2) : 0;

                    photos.Add(new Photo
                    {
                        FilePath = file,
                        Folder = "未分类",
                        Title = Path.GetFileNameWithoutExtension(file),
                        Date = File.GetCreationTime(file),
                        Tags = [],
                        Description = string.Empty,
                        Location = string.Empty,
                        FileSizeKB = fileSizeKB,
                        ExifData = exifService.ExtractExifData(file)
                    });
                    compressionService.CompressImageAsync(file).Wait();
                }
            }
        }

        context.Photos.AddRange(photos);
        context.SaveChanges();
    }
}