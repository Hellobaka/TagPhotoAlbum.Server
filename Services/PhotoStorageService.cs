using Microsoft.Extensions.Options;

namespace TagPhotoAlbum.Server.Services;

public class PhotoStorageOptions
{
    public string[] ExternalStoragePaths { get; set; } = Array.Empty<string>();
}

public class PhotoStorageService
{
    private readonly string[] _externalStoragePaths;
    private readonly string _primaryStoragePath;

    public PhotoStorageService(IOptions<PhotoStorageOptions> options)
    {
        _externalStoragePaths = options.Value.ExternalStoragePaths;
        _primaryStoragePath = _externalStoragePaths.FirstOrDefault() ?? string.Empty;

        // 确保所有外部存储目录存在
        foreach (var path in _externalStoragePaths)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }
    }

    /// <summary>
    /// 保存上传的文件到外部存储
    /// </summary>
    public async Task<string> SaveFileAsync(IFormFile file, string folder)
    {
        if (string.IsNullOrEmpty(folder))
        {
            folder = "未分类";
        }

        // 使用主存储路径
        var folderPath = Path.Combine(_primaryStoragePath, folder);
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        // 使用原文件名
        var fileName = Path.GetFileName(file.FileName);
        var filePath = Path.Combine(folderPath, fileName);

        // 保存文件
        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        // 返回文件绝对路径
        return filePath;
    }

    /// <summary>
    /// 移动文件到新的文件夹
    /// </summary>
    public Task<string> MoveFileAsync(string currentFilePath, string newFolder)
    {
        if (string.IsNullOrEmpty(newFolder))
        {
            newFolder = "未分类";
        }

        // 从当前文件路径获取文件名
        var fileName = Path.GetFileName(currentFilePath);
        var currentFolder = Path.GetFileName(Path.GetDirectoryName(currentFilePath));

        if (string.IsNullOrEmpty(currentFolder) || string.IsNullOrEmpty(fileName))
        {
            throw new ArgumentException("无效的文件路径");
        }

        // 如果文件夹相同，不需要移动
        if (currentFolder == newFolder)
        {
            return Task.FromResult(currentFilePath);
        }

        // 确定文件当前所在的存储路径
        var currentStoragePath = _externalStoragePaths.FirstOrDefault(path => currentFilePath.StartsWith(path));
        if (string.IsNullOrEmpty(currentStoragePath))
        {
            throw new ArgumentException("文件不在任何配置的存储路径中");
        }

        // 使用当前存储路径创建新文件夹
        var newFolderPath = Path.Combine(currentStoragePath, newFolder);
        var newFilePath = Path.Combine(newFolderPath, fileName);

        // 确保新文件夹存在
        if (!Directory.Exists(newFolderPath))
        {
            Directory.CreateDirectory(newFolderPath);
        }

        // 移动文件
        if (File.Exists(currentFilePath))
        {
            // 如果目标文件已存在，先删除
            if (File.Exists(newFilePath))
            {
                File.Delete(newFilePath);
            }

            File.Move(currentFilePath, newFilePath);

            // 如果原文件夹为空，删除原文件夹
            var currentFolderPath = Path.GetDirectoryName(currentFilePath);
            if (Directory.Exists(currentFolderPath) && !Directory.EnumerateFileSystemEntries(currentFolderPath).Any())
            {
                Directory.Delete(currentFolderPath);
            }
        }

        // 返回新的文件绝对路径
        return Task.FromResult(newFilePath);
    }

    /// <summary>
    /// 删除文件
    /// </summary>
    public void DeleteFile(string filePath)
    {
        if (File.Exists(filePath))
        {
            File.Delete(filePath);

            // 如果文件夹为空，删除文件夹
            var folderPath = Path.GetDirectoryName(filePath);
            if (Directory.Exists(folderPath) && !Directory.EnumerateFileSystemEntries(folderPath).Any())
            {
                Directory.Delete(folderPath);
            }
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
            return string.Empty;
        }

        // 查找文件所在的存储路径
        var storagePath = _externalStoragePaths.FirstOrDefault(path => filePath.StartsWith(path));
        if (string.IsNullOrEmpty(storagePath))
        {
            return string.Empty;
        }

        var relativePath = filePath.Substring(storagePath.Length).Replace('\\', '/');
        return $"/external{relativePath}";
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
}