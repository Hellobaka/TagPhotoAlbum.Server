using TagPhotoAlbum.Server.Models;

namespace TagPhotoAlbum.Server.Data;

public static class SeedData
{
    public static void Initialize(AppDbContext context)
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
            PasswordHash = "admin123" // In production, use proper password hashing
        };
        context.Users.Add(user);

        // Add sample photos
        var photos = new List<Photo>
        {
            new Photo
            {
                FilePath = "",
                Title = "抽象艺术作品",
                Description = "色彩丰富的抽象艺术作品，展现现代艺术的魅力",
                Tags = new List<string> { "艺术", "抽象", "色彩" },
                Folder = "艺术",
                Location = "画廊",
                Date = new DateTime(2024, 1, 15)
            },
            new Photo
            {
                FilePath = "",
                Title = "海洋日落",
                Description = "壮丽的海洋日落景色，金色的阳光洒在海面上",
                Tags = new List<string> { "风景", "海洋", "日落" },
                Folder = "旅行",
                Location = "海边",
                Date = new DateTime(2024, 2, 20)
            },
            new Photo
            {
                FilePath = "",
                Title = "海浪拍岸",
                Description = "海浪拍打岩石的壮观瞬间",
                Tags = new List<string> { "风景", "海洋", "自然" },
                Folder = "旅行",
                Location = "海岸",
                Date = new DateTime(2024, 2, 21)
            },
            new Photo
            {
                FilePath = "",
                Title = "日落海景",
                Description = "美丽的日落海景，天空与海面的完美结合",
                Tags = new List<string> { "风景", "海洋", "日落" },
                Folder = "旅行",
                Location = "海滩",
                Date = new DateTime(2024, 2, 22)
            },
            new Photo
            {
                FilePath = "",
                Title = "现代抽象画",
                Description = "现代风格的抽象画作，几何图形的巧妙组合",
                Tags = new List<string> { "艺术", "抽象", "现代" },
                Folder = "艺术",
                Location = "美术馆",
                Date = new DateTime(2024, 1, 20)
            },
            new Photo
            {
                FilePath = "",
                Title = "色彩抽象",
                Description = "充满活力的色彩抽象艺术",
                Tags = new List<string> { "艺术", "抽象", "色彩" },
                Folder = "艺术",
                Location = "画廊",
                Date = new DateTime(2024, 1, 25)
            },
            new Photo
            {
                FilePath = "",
                Title = "森林深处",
                Description = "神秘的森林深处，阳光透过树叶洒下",
                Tags = new List<string> { "风景", "森林", "自然" },
                Folder = "旅行",
                Location = "森林公园",
                Date = new DateTime(2024, 3, 15)
            },
            new Photo
            {
                FilePath = "",
                Title = "森林小径",
                Description = "蜿蜒的森林小径，通往未知的目的地",
                Tags = new List<string> { "风景", "森林", "小径" },
                Folder = "旅行",
                Location = "森林",
                Date = new DateTime(2024, 3, 16)
            },
            new Photo
            {
                FilePath = "",
                Title = "抽象几何",
                Description = "几何形状的抽象组合",
                Tags = new List<string> { "艺术", "抽象", "几何" },
                Folder = "艺术",
                Location = "工作室",
                Date = new DateTime(2024, 1, 30)
            },
            new Photo
            {
                FilePath = "",
                Title = "人像摄影",
                Description = "专业的人像摄影作品",
                Tags = new List<string> { "人像", "摄影", "专业" },
                Folder = "人像",
                Location = "摄影棚",
                Date = new DateTime(2024, 4, 1)
            },
            new Photo
            {
                FilePath = "",
                Title = "现代艺术",
                Description = "现代风格的抽象艺术作品",
                Tags = new List<string> { "艺术", "抽象", "现代" },
                Folder = "艺术",
                Location = "画廊",
                Date = new DateTime(2024, 2, 5)
            },
            new Photo
            {
                FilePath = "",
                Title = "森林光影",
                Description = "森林中的光影交错",
                Tags = new List<string> { "风景", "森林", "光影" },
                Folder = "旅行",
                Location = "森林",
                Date = new DateTime(2024, 3, 20)
            },
            new Photo
            {
                FilePath = "",
                Title = "抽象色彩",
                Description = "丰富的色彩抽象表现",
                Tags = new List<string> { "艺术", "抽象", "色彩" },
                Folder = "艺术",
                Location = "工作室",
                Date = new DateTime(2024, 2, 10)
            },
            new Photo
            {
                FilePath = "",
                Title = "艺术画作",
                Description = "精美的艺术画作展示",
                Tags = new List<string> { "艺术", "画作", "精美" },
                Folder = "艺术",
                Location = "美术馆",
                Date = new DateTime(2024, 2, 15)
            },
            new Photo
            {
                FilePath = "",
                Title = "海滩风光",
                Description = "美丽的海滩风光",
                Tags = new List<string> { "风景", "海滩", "夏天" },
                Folder = "旅行",
                Location = "海滩",
                Date = new DateTime(2024, 5, 1)
            }
        };

        context.Photos.AddRange(photos);
        context.SaveChanges();
    }
}