using ImageMagick;
using Microsoft.Extensions.Options;

namespace TagPhotoAlbum.Server.Services;

public class ImageCompressionOptions
{
    public uint Quality { get; set; } = 80;

    public string CompressedFolder { get; set; } = "compressed";
}

public class ImageCompressionService
{
    private readonly ImageCompressionOptions _options;
    private readonly PhotoStorageService _photoStorageService;

    public ImageCompressionService(
        PhotoStorageService photoStorageService,
        IOptions<ImageCompressionOptions> options)
    {
        _photoStorageService = photoStorageService;
        _options = options.Value;
    }

    /// <summary>
    /// 压缩图片并保存到压缩文件夹
    /// </summary>
    public async Task<string?> CompressImageAsync(string originalFilePath)
    {
        try
        {
            // 检查原文件是否存在
            if (!File.Exists(originalFilePath))
            {
                return null;
            }

            // 检查文件类型，只处理支持的图片格式
            var extension = Path.GetExtension(originalFilePath).ToLowerInvariant();
            var supportedFormats = new[] { ".jpg", ".jpeg", ".png", ".bmp", ".webp", ".gif", ".tiff" };

            if (!supportedFormats.Contains(extension))
            {
                return null;
            }

            // 获取原文件的文件夹和文件名
            var originalFolder = Path.GetFileName(Path.GetDirectoryName(originalFilePath));
            var fileName = Path.GetFileNameWithoutExtension(originalFilePath);
            var compressedFileName = $"{fileName}_compressed.jpg";

            // 生成压缩文件的路径
            var compressedFolder = Path.Combine(_options.CompressedFolder, originalFolder ?? "未分类");
            var compressedFilePath = await GetCompressedFilePathAsync(compressedFolder, compressedFileName);

            // 如果压缩文件已存在，直接返回路径
            if (File.Exists(compressedFilePath))
            {
                return compressedFilePath;
            }

            // 使用ImageMagick压缩图片
            using var image = new MagickImage(originalFilePath);

            // 设置压缩质量
            image.Quality = _options.Quality;

            // 确保压缩文件目录存在
            var compressedFileDir = Path.GetDirectoryName(compressedFilePath);
            if (!Directory.Exists(compressedFileDir))
            {
                Directory.CreateDirectory(compressedFileDir!);
            }

            // 保存压缩图片为JPEG格式
            await image.WriteAsync(compressedFilePath);

            return compressedFilePath;
        }
        catch (Exception ex)
        {
            // 记录错误但不抛出异常，避免影响主流程
            Console.WriteLine($"压缩图片失败: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// 获取压缩文件的URL
    /// </summary>
    public string? GetCompressedFileUrl(string originalFilePath)
    {
        if (string.IsNullOrEmpty(originalFilePath))
        {
            return null;
        }

        var originalFolder = Path.GetFileName(Path.GetDirectoryName(originalFilePath));
        var fileName = Path.GetFileNameWithoutExtension(originalFilePath);
        var compressedFileName = $"{fileName}_compressed.jpg";
        var compressedFolder = Path.Combine(_options.CompressedFolder, originalFolder ?? "未分类");

        // 生成压缩文件路径
        var compressedFilePath = Path.Combine(_photoStorageService.GetPrimaryStoragePath(), compressedFolder, compressedFileName);

        // 如果压缩文件不存在，返回 null
        if (!File.Exists(compressedFilePath))
        {
            return null;
        }

        return _photoStorageService.GetFileUrl(compressedFilePath);
    }

    /// <summary>
    /// 检查压缩文件是否存在
    /// </summary>
    public bool CompressedFileExists(string originalFilePath)
    {
        var compressedFileUrl = GetCompressedFileUrl(originalFilePath);
        return !string.IsNullOrEmpty(compressedFileUrl);
    }

    /// <summary>
    /// 删除压缩文件
    /// </summary>
    public void DeleteCompressedFile(string originalFilePath)
    {
        try
        {
            var originalFolder = Path.GetFileName(Path.GetDirectoryName(originalFilePath));
            var fileName = Path.GetFileNameWithoutExtension(originalFilePath);
            var compressedFileName = $"{fileName}_compressed.jpg";
            var compressedFolder = Path.Combine(_options.CompressedFolder, originalFolder ?? "未分类");

            var compressedFilePath = Path.Combine(_photoStorageService.GetPrimaryStoragePath(), compressedFolder, compressedFileName);

            if (File.Exists(compressedFilePath))
            {
                File.Delete(compressedFilePath);

                // 如果压缩文件夹为空，删除文件夹
                var compressedFolderPath = Path.GetDirectoryName(compressedFilePath);
                if (Directory.Exists(compressedFolderPath) && !Directory.EnumerateFileSystemEntries(compressedFolderPath).Any())
                {
                    Directory.Delete(compressedFolderPath);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"删除压缩文件失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 移动压缩文件（当原文件移动时调用）
    /// </summary>
    public async Task MoveCompressedFileAsync(string originalOldFilePath, string originalNewFilePath)
    {
        try
        {
            var oldCompressedFileUrl = GetCompressedFileUrl(originalOldFilePath);
            if (string.IsNullOrEmpty(oldCompressedFileUrl))
            {
                return; // 没有压缩文件，无需移动
            }

            // 获取原压缩文件路径
            var oldCompressedFilePath = GetCompressedFilePathFromUrl(oldCompressedFileUrl);
            if (string.IsNullOrEmpty(oldCompressedFilePath) || !File.Exists(oldCompressedFilePath))
            {
                return;
            }

            // 生成新的压缩文件路径
            var newOriginalFolder = Path.GetFileName(Path.GetDirectoryName(originalNewFilePath));
            var fileName = Path.GetFileNameWithoutExtension(originalNewFilePath);
            var compressedFileName = $"{fileName}_compressed.jpg";
            var compressedFolder = Path.Combine(_options.CompressedFolder, newOriginalFolder ?? "未分类");
            var newCompressedFilePath = await GetCompressedFilePathAsync(compressedFolder, compressedFileName);

            // 确保新目录存在
            var newCompressedFileDir = Path.GetDirectoryName(newCompressedFilePath);
            if (!Directory.Exists(newCompressedFileDir))
            {
                Directory.CreateDirectory(newCompressedFileDir!);
            }

            // 移动文件
            File.Move(oldCompressedFilePath, newCompressedFilePath);

            // 清理旧目录（如果为空）
            var oldCompressedFileDir = Path.GetDirectoryName(oldCompressedFilePath);
            if (Directory.Exists(oldCompressedFileDir) && !Directory.EnumerateFileSystemEntries(oldCompressedFileDir).Any())
            {
                Directory.Delete(oldCompressedFileDir);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"移动压缩文件失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 批量压缩图片
    /// </summary>
    public async Task<List<string>> BatchCompressImagesAsync(List<string> originalFilePaths)
    {
        var tasks = originalFilePaths.Select(CompressImageAsync);
        var results = await Task.WhenAll(tasks);
        return results.Where(path => !string.IsNullOrEmpty(path)).ToList()!;
    }

    private async Task<string> GetCompressedFilePathAsync(string compressedFolder, string compressedFileName)
    {
        var compressedFolderPath = Path.Combine(_photoStorageService.GetPrimaryStoragePath(), compressedFolder);

        // 确保压缩文件夹存在
        if (!Directory.Exists(compressedFolderPath))
        {
            Directory.CreateDirectory(compressedFolderPath);
        }

        return Path.Combine(compressedFolderPath, compressedFileName);
    }

    private string? GetCompressedFilePathFromUrl(string compressedFileUrl)
    {
        if (string.IsNullOrEmpty(compressedFileUrl))
        {
            return null;
        }

        // 从URL中提取相对路径
        var relativePath = compressedFileUrl.Replace("/external/", "").Replace('/', '\\');

        // 查找文件所在的存储路径
        var storagePath = _photoStorageService.GetStoragePaths()
            .FirstOrDefault(path => File.Exists(Path.Combine(path, relativePath)));

        return storagePath != null ? Path.Combine(storagePath, relativePath) : null;
    }
}