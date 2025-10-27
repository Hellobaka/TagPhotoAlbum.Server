using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TagPhotoAlbum.Server.Data;
using TagPhotoAlbum.Server.Models;
using System.IO;

namespace TagPhotoAlbum.Server.Services;

public class PhotoSyncService : BackgroundService
{
    private readonly ILogger<PhotoSyncService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly string[] _externalStoragePaths;
    private readonly TimeSpan _syncInterval;

    public PhotoSyncService(
        ILogger<PhotoSyncService> logger,
        IServiceProvider serviceProvider,
        IOptions<PhotoStorageOptions> photoStorageOptions,
        IConfiguration configuration)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _externalStoragePaths = photoStorageOptions.Value.ExternalStoragePaths ?? Array.Empty<string>();

        // 从配置中获取同步间隔，默认为1小时
        var syncIntervalMinutes = configuration.GetValue<int?>("PhotoSync:SyncIntervalMinutes") ?? 60;
        _syncInterval = TimeSpan.FromMinutes(syncIntervalMinutes);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("图片同步服务已启动，同步间隔: {Interval}", _syncInterval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await SyncPhotosAsync();
                await Task.Delay(_syncInterval, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("图片同步服务正在停止...");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "图片同步过程中发生错误");
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken); // 出错后等待5分钟再重试
            }
        }

        _logger.LogInformation("图片同步服务已停止");
    }

    private async Task SyncPhotosAsync()
    {
        _logger.LogInformation("开始扫描外部存储路径中的新增图片...");

        var newPhotos = new List<Photo>();

        // 获取当前存储路径中的所有实际文件
        var actualFiles = new HashSet<string>();

        foreach (var storagePath in _externalStoragePaths)
        {
            if (string.IsNullOrEmpty(storagePath) || !Directory.Exists(storagePath))
            {
                _logger.LogWarning("存储路径不存在或为空: {StoragePath}", storagePath);
                continue;
            }

            _logger.LogInformation("扫描存储路径: {StoragePath}", storagePath);

            // 获取所有支持的图片文件
            var supportedExtensions = new[] { ".jpg", ".jpeg", ".png", ".bmp", ".gif", ".webp", ".svg" };
            var files = Directory.GetFiles(storagePath)
                .Where(file => supportedExtensions.Contains(Path.GetExtension(file).ToLowerInvariant()));

            // 使用SemaphoreSlim限制并发数量，避免过多数据库连接
            var semaphore = new SemaphoreSlim(Environment.ProcessorCount);
            var tasks = new List<Task>();

            foreach (var file in files)
            {
                await semaphore.WaitAsync();
                tasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        await ProcessFileAsync(file, actualFiles, newPhotos);
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }));
            }

            await Task.WhenAll(tasks);
        }

        // 使用新的scope来处理数据库操作
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // 检查数据库中的记录是否对应实际存在的文件
        var deletedPhotos = new List<Photo>();
        var allPhotos = await context.Photos.ToListAsync();
        foreach (var photo in allPhotos)
        {
            if (!actualFiles.Contains(photo.FilePath) && !System.IO.File.Exists(photo.FilePath))
            {
                deletedPhotos.Add(photo);
                _logger.LogInformation("发现已删除的图片记录: {FilePath}", photo.FilePath);
            }
        }

        // 先删除不存在的记录
        if (deletedPhotos.Count > 0)
        {
            context.Photos.RemoveRange(deletedPhotos);
            _logger.LogInformation("删除 {Count} 条不存在的图片记录", deletedPhotos.Count);
        }

        // 再添加新记录
        if (newPhotos.Count > 0)
        {
            await context.Photos.AddRangeAsync(newPhotos);
            _logger.LogInformation("添加 {Count} 张新图片到数据库", newPhotos.Count);
        }

        // 保存所有更改
        if (newPhotos.Count > 0 || deletedPhotos.Count > 0)
        {
            await context.SaveChangesAsync();
            _logger.LogInformation("同步完成 - 新增: {AddedCount}, 删除: {DeletedCount}", newPhotos.Count, deletedPhotos.Count);
        }
        else
        {
            _logger.LogInformation("未发现新增或删除的图片，已扫描文件数: {ScannedFiles}", actualFiles.Count);
        }
    }

    private async Task ProcessFileAsync(string file, HashSet<string> actualFiles, List<Photo> newPhotos)
    {
        try
        {
            lock (actualFiles)
            {
                actualFiles.Add(file);
            }

            // 为每个文件创建独立的scope和DbContext
            using var fileScope = _serviceProvider.CreateScope();
            var fileContext = fileScope.ServiceProvider.GetRequiredService<AppDbContext>();
            var exifService = fileScope.ServiceProvider.GetRequiredService<ExifService>();
            var photoStorageService = fileScope.ServiceProvider.GetRequiredService<PhotoStorageService>();
            var compressionService = fileScope.ServiceProvider.GetRequiredService<ImageCompressionService>();

            // 检查文件是否已在数据库中
            var existingPhoto = await fileContext.Photos
                .FirstOrDefaultAsync(p => p.FilePath == file);

            if (existingPhoto != null)
            {
                // 文件已存在，跳过
                return;
            }

            // 创建新的照片记录
            var fileInfoObj = new FileInfo(file);
            var fileSizeKB = fileInfoObj.Exists ? Math.Round(fileInfoObj.Length / 1024.0, 2) : 0;

            var photo = new Photo
            {
                FilePath = file,
                Title = Path.GetFileNameWithoutExtension(file),
                Description = string.Empty,
                Folder = GetFolderFromPath(file),
                Location = "未指定",
                Date = photoStorageService.GetFileDate(file),
                FileSizeKB = fileSizeKB,
                ExifData = exifService.ExtractExifData(file),
                Tags = [],
                Rating = 0 // 默认评分为0
            };

            lock (newPhotos)
            {
                newPhotos.Add(photo);
            }

            // 异步压缩图片
            _ = Task.Run(async () =>
            {
                try
                {
                    await compressionService.CompressImageAsync(file);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "压缩图片失败: {FilePath}", file);
                }
            });

            _logger.LogInformation("发现新图片: {FilePath}", file);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理文件时发生错误: {FilePath}", file);
        }
    }

    private string GetFolderFromPath(string filePath)
    {
        var fileName = Path.GetFileName(filePath);
        var directory = Path.GetDirectoryName(filePath);

        if (string.IsNullOrEmpty(directory))
            return "未分类";

        // 查找相对于外部存储路径的文件夹
        foreach (var storagePath in _externalStoragePaths)
        {
            if (directory.StartsWith(storagePath, StringComparison.OrdinalIgnoreCase))
            {
                var relativePath = directory.Substring(storagePath.Length).TrimStart(Path.DirectorySeparatorChar);
                return string.IsNullOrEmpty(relativePath) ? "未分类" : relativePath;
            }
        }

        return "未分类";
    }
}