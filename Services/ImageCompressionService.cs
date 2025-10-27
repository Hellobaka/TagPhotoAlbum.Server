using ImageMagick;
using Microsoft.Extensions.Options;
using NLog;

namespace TagPhotoAlbum.Server.Services;

public class ImageCompressionOptions
{
    public bool EnableCompress { get; set; } = false;

    public uint Quality { get; set; } = 80;

    public string CompressedFolder { get; set; } = "compressed";
}

public class ImageCompressionService
{
    private readonly ImageCompressionOptions _options;
    private readonly PhotoStorageService _photoStorageService;
    private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

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
            if (!_options.EnableCompress)
            {
                _logger.Info("图片压缩功能未启用");
                return null;
            }
            _logger.Info("开始压缩图片 - 原文件路径: {OriginalFilePath}", originalFilePath);

            // 检查原文件是否存在
            if (!File.Exists(originalFilePath))
            {
                _logger.Warn("原文件不存在，无法压缩: {OriginalFilePath}", originalFilePath);
                return null;
            }

            // 检查文件类型，只处理支持的图片格式
            var extension = Path.GetExtension(originalFilePath).ToLowerInvariant();
            var supportedFormats = new[] { ".jpg", ".jpeg", ".png", ".bmp", ".webp", ".gif", ".tiff" };

            if (!supportedFormats.Contains(extension))
            {
                _logger.Info("文件格式不支持压缩: {Extension}, 文件路径: {OriginalFilePath}", extension, originalFilePath);
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
            long compressedFileLength = new FileInfo(compressedFilePath).Length;
            long originalFileLength = new FileInfo(originalFilePath).Length;
            double compressRate = compressedFileLength / (originalFileLength * 1.0);

            _logger.Info("图片压缩成功 - 压缩文件路径: {CompressedFilePath}; 压缩后大小: {CompressedFileLength:f1} KB 压缩比: {CompressRate:f1}%", compressedFilePath, compressedFileLength / 1024.0, compressRate * 100);

            return compressedFilePath;
        }
        catch (Exception ex)
        {
            // 记录错误但不抛出异常，避免影响主流程
            _logger.Error(ex, "压缩图片失败: {OriginalFilePath}", originalFilePath);
            return null;
        }
    }

    /// <summary>
    /// 获取压缩文件的URL
    /// </summary>
    public string? GetCompressedFileUrl(string originalFilePath)
    {
        _logger.Debug("开始获取压缩文件URL - 原文件路径: {OriginalFilePath}", originalFilePath);

        if (string.IsNullOrEmpty(originalFilePath))
        {
            _logger.Debug("原文件路径为空，返回null");
            return null;
        }

        var fileName = Path.GetFileNameWithoutExtension(originalFilePath);
        var compressedFileName = $"{fileName}_compressed.jpg";
        var compressedFolder = _options.CompressedFolder;

        // 生成压缩文件路径
        var compressedFilePath = Path.Combine(_photoStorageService.GetPrimaryStoragePath(), compressedFolder, compressedFileName);

        _logger.Debug("生成压缩文件路径: {CompressedFilePath}", compressedFilePath);

        // 如果压缩文件不存在，返回 null
        if (!File.Exists(compressedFilePath))
        {
            _logger.Debug("压缩文件不存在: {CompressedFilePath}", compressedFilePath);
            return null;
        }

        var url = _photoStorageService.GetFileUrl(compressedFilePath);
        _logger.Debug("获取压缩文件URL成功: {Url}", url);

        return url;
    }

    /// <summary>
    /// 检查压缩文件是否存在
    /// </summary>
    public bool CompressedFileExists(string originalFilePath)
    {
        _logger.Debug("检查压缩文件是否存在 - 原文件路径: {OriginalFilePath}", originalFilePath);

        var compressedFileUrl = GetCompressedFileUrl(originalFilePath);
        var exists = !string.IsNullOrEmpty(compressedFileUrl);

        _logger.Debug("压缩文件存在性检查结果: {Exists} - 原文件路径: {OriginalFilePath}", exists, originalFilePath);

        return exists;
    }

    private async Task<string> GetCompressedFilePathAsync(string compressedFolder, string compressedFileName)
    {
        var compressedFolderPath = Path.Combine(_photoStorageService.GetPrimaryStoragePath(), compressedFolder);

        _logger.Debug("生成压缩文件夹路径: {CompressedFolderPath}", compressedFolderPath);

        // 确保压缩文件夹存在
        if (!Directory.Exists(compressedFolderPath))
        {
            Directory.CreateDirectory(compressedFolderPath);
            _logger.Debug("创建压缩文件夹: {CompressedFolderPath}", compressedFolderPath);
        }

        var compressedFilePath = Path.Combine(compressedFolderPath, compressedFileName);
        _logger.Debug("生成压缩文件路径: {CompressedFilePath}", compressedFilePath);

        return compressedFilePath;
    }
}