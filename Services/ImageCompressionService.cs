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
            var fileName = Path.GetFileNameWithoutExtension(originalFilePath);
            var compressedFileName = $"{fileName}_compressed.jpg";

            // 生成压缩文件的路径
            var compressedFilePath = await GetCompressedFilePathAsync(_options.CompressedFolder, compressedFileName);

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

        var fileName = Path.GetFileNameWithoutExtension(originalFilePath);
        var compressedFileName = $"{fileName}_compressed.jpg";
        var compressedFolder = _options.CompressedFolder;

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
}