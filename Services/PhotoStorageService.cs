using Microsoft.Extensions.Options;
using NLog;

namespace TagPhotoAlbum.Server.Services;

public class PhotoStorageOptions
{
    public string[] ExternalStoragePaths { get; set; } = Array.Empty<string>();
}

public class PhotoStorageService
{
    private readonly string[] _externalStoragePaths;
    private readonly string _primaryStoragePath;
    private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

    public PhotoStorageService(IOptions<PhotoStorageOptions> options)
    {
        _externalStoragePaths = options.Value.ExternalStoragePaths;
        _primaryStoragePath = _externalStoragePaths.FirstOrDefault() ?? string.Empty;

        //_logger.Info("初始化PhotoStorageService - 存储路径数量: {StoragePathCount}", _externalStoragePaths.Length);

        // 确保所有外部存储目录存在
        foreach (var path in _externalStoragePaths)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
                _logger.Info("创建存储目录: {StoragePath}", path);
            }
        }
    }

    /// <summary>
    /// 保存上传的文件到外部存储
    /// </summary>
    public async Task<string> SaveFileAsync(IFormFile file)
    {
        _logger.Info("开始保存文件 - 文件名: {FileName}", file.FileName);

        // 使用主存储路径
        var folderPath = _primaryStoragePath;
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
            _logger.Info("创建文件夹: {FolderPath}", folderPath);
        }

        // 使用原文件名
        var fileName = Path.GetFileName(file.FileName);
        var filePath = Path.Combine(folderPath, fileName);

        _logger.Info("准备保存文件 - 文件路径: {FilePath}", filePath);

        // 保存文件
        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        _logger.Info("文件保存成功 - 文件路径: {FilePath}", filePath);

        // 返回文件绝对路径
        return filePath;
    }

    /// <summary>
    /// 移动文件到新的文件夹
    /// </summary>
    public Task<string> MoveFileAsync(string currentFilePath, string newFolder)
    {
        _logger.Info("开始移动文件 - 当前文件路径: {CurrentFilePath}, 新文件夹: {NewFolder}", currentFilePath, newFolder);

        if (string.IsNullOrEmpty(newFolder))
        {
            newFolder = "未分类";
        }

        // 从当前文件路径获取文件名
        var fileName = Path.GetFileName(currentFilePath);
        var currentFolder = Path.GetFileName(Path.GetDirectoryName(currentFilePath));

        if (string.IsNullOrEmpty(currentFolder) || string.IsNullOrEmpty(fileName))
        {
            _logger.Error("移动文件失败 - 无效的文件路径: {CurrentFilePath}", currentFilePath);
            throw new ArgumentException("无效的文件路径");
        }

        // 如果文件夹相同，不需要移动
        if (currentFolder == newFolder)
        {
            _logger.Info("文件夹相同，无需移动 - 文件路径: {CurrentFilePath}", currentFilePath);
            return Task.FromResult(currentFilePath);
        }

        // 确定文件当前所在的存储路径
        var currentStoragePath = _externalStoragePaths.FirstOrDefault(path => currentFilePath.StartsWith(path));
        if (string.IsNullOrEmpty(currentStoragePath))
        {
            _logger.Error("移动文件失败 - 文件不在任何配置的存储路径中: {CurrentFilePath}", currentFilePath);
            throw new ArgumentException("文件不在任何配置的存储路径中");
        }

        // 使用当前存储路径创建新文件夹
        var newFolderPath = Path.Combine(currentStoragePath, newFolder);
        var newFilePath = Path.Combine(newFolderPath, fileName);

        _logger.Info("计算新文件路径 - 新文件夹路径: {NewFolderPath}, 新文件路径: {NewFilePath}", newFolderPath, newFilePath);

        // 确保新文件夹存在
        if (!Directory.Exists(newFolderPath))
        {
            Directory.CreateDirectory(newFolderPath);
            _logger.Info("创建新文件夹: {NewFolderPath}", newFolderPath);
        }

        // 移动文件
        if (File.Exists(currentFilePath))
        {
            // 如果目标文件已存在，先删除
            if (File.Exists(newFilePath))
            {
                File.Delete(newFilePath);
                _logger.Info("删除已存在的目标文件: {NewFilePath}", newFilePath);
            }

            File.Move(currentFilePath, newFilePath);
            _logger.Info("文件移动成功 - 原路径: {CurrentFilePath}, 新路径: {NewFilePath}", currentFilePath, newFilePath);

            // 如果原文件夹为空，删除原文件夹
            var currentFolderPath = Path.GetDirectoryName(currentFilePath);
            if (Directory.Exists(currentFolderPath) && !Directory.EnumerateFileSystemEntries(currentFolderPath).Any())
            {
                Directory.Delete(currentFolderPath);
                _logger.Info("删除空的原文件夹: {CurrentFolderPath}", currentFolderPath);
            }
        }

        // 返回新的文件绝对路径
        _logger.Info("文件移动完成 - 新文件路径: {NewFilePath}", newFilePath);
        return Task.FromResult(newFilePath);
    }

    /// <summary>
    /// 删除文件
    /// </summary>
    public void DeleteFile(string filePath)
    {
        _logger.Info("开始删除文件 - 文件路径: {FilePath}", filePath);

        if (File.Exists(filePath))
        {
            File.Delete(filePath);
            _logger.Info("文件删除成功 - 文件路径: {FilePath}", filePath);

            // 如果文件夹为空，删除文件夹
            var folderPath = Path.GetDirectoryName(filePath);
            if (Directory.Exists(folderPath) && !Directory.EnumerateFileSystemEntries(folderPath).Any())
            {
                Directory.Delete(folderPath);
                _logger.Info("删除空文件夹: {FolderPath}", folderPath);
            }
        }
        else
        {
            _logger.Warn("文件不存在，无法删除 - 文件路径: {FilePath}", filePath);
        }
    }

    /// <summary>
    /// 检查文件是否存在
    /// </summary>
    public bool FileExists(string filePath)
    {
        return File.Exists(filePath);
    }

    /// <summary>
    /// 根据文件路径生成URL
    /// </summary>
    public string GetFileUrl(string filePath)
    {
        if (string.IsNullOrEmpty(filePath))
        {
            _logger.Debug("文件路径为空，返回空URL");
            return string.Empty;
        }

        // 查找文件所在的存储路径
        var storagePath = _externalStoragePaths.FirstOrDefault(path => filePath.StartsWith(path));
        if (string.IsNullOrEmpty(storagePath))
        {
            _logger.Warn("文件不在任何配置的存储路径中: {FilePath}", filePath);
            return string.Empty;
        }

        var relativePath = filePath.Substring(storagePath.Length).Replace('\\', '/');
        var url = $"/external{relativePath}";

        _logger.Debug("生成文件URL - 文件路径: {FilePath}, URL: {Url}", filePath, url);

        return url;
    }

    /// <summary>
    /// 获取所有存储路径
    /// </summary>
    public string[] GetStoragePaths()
    {
        return _externalStoragePaths;
    }

    /// <summary>
    /// 获取主存储路径
    /// </summary>
    public string GetPrimaryStoragePath()
    {
        return _primaryStoragePath;
    }

    public DateTime GetFileDate(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException(filePath);
        }
        var createTime = File.GetCreationTime(filePath);
        var lastModifyTime = File.GetLastWriteTime(filePath);
        var lastAccessTime = File.GetLastAccessTime(filePath);
        if (createTime.Year > 2000)
        {
            return createTime;
        }
        if (lastModifyTime.Year > 2000)
        {
            return lastModifyTime;
        }
        if (lastAccessTime.Year > 2000)
        {
            return lastAccessTime;
        }

        return DateTime.Now;
    }
}