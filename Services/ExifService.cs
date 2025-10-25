using System.Text.Json;
using ImageMagick;
using NLog;

namespace TagPhotoAlbum.Server.Services;

public class ExifService
{
    private static string[] FilterExifKey { get; set; } = ["MakerNote"];
    private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

    /// <summary>
    /// 从图片文件中提取EXIF信息
    /// </summary>
    public string? ExtractExifData(string filePath)
    {
        _logger.Info("开始提取EXIF信息 - 文件路径: {FilePath}", filePath);

        try
        {
            if (!File.Exists(filePath))
            {
                _logger.Warn("文件不存在，无法提取EXIF信息: {FilePath}", filePath);
                return null;
            }

            // 检查文件类型，只处理支持EXIF的图片格式
            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            var supportedFormats = new[] { ".jpg", ".jpeg", ".tiff", ".tif", ".png", ".bmp" };

            if (!supportedFormats.Contains(extension))
            {
                _logger.Info("文件格式不支持EXIF提取: {Extension}, 文件路径: {FilePath}", extension, filePath);
                return null;
            }

            var exifData = new Dictionary<string, object>();

            // 使用ImageMagick提取EXIF信息
            using var image = new MagickImage(filePath);

            _logger.Debug("成功加载图片文件: {FilePath}", filePath);

            // 提取所有EXIF属性
            var profile = image.GetExifProfile();
            if (profile != null)
            {
                _logger.Debug("找到EXIF配置文件，开始提取标签");

                var exifValues = new Dictionary<string, object>();

                foreach (var value in profile.Values)
                {
                    try
                    {
                        var tagName = value.Tag.ToString();
                        if (FilterExifKey.Contains(tagName))
                        {
                            _logger.Debug("跳过过滤的EXIF标签: {TagName}", tagName);
                            continue;
                        }
                        var tagValue = GetTagValue(value);

                        if (tagValue != null)
                        {
                            exifValues[tagName] = tagValue;
                            _logger.Debug("提取EXIF标签: {TagName}", tagName);
                        }
                    }
                    catch (Exception ex)
                    {
                        // 跳过无法处理的标签
                        _logger.Warn(ex, "处理标签 {TagName} 时出错", value.Tag);
                    }
                }

                if (exifValues.Count > 0)
                {
                    exifData["Exif"] = exifValues;
                    _logger.Info("成功提取EXIF数据 - 标签数量: {TagCount}", exifValues.Count);
                }
            }

            // 提取常用EXIF信息
            // var commonExif = ExtractCommonExifInfo(image);
            // if (commonExif.Count > 0)
            // {
            //     exifData["CommonExif"] = commonExif;
            // }

            // 提取图片基本信息
            // var imageInfo = ExtractImageInfo(image);
            // if (imageInfo.Count > 0)
            // {
            //     exifData["ImageInfo"] = imageInfo;
            // }

            if (exifData.Count > 0)
            {
                var json = JsonSerializer.Serialize(exifData, new JsonSerializerOptions
                {
                    WriteIndented = false,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
                _logger.Info("EXIF信息提取完成 - JSON长度: {JsonLength}", json.Length);
                return json;
            }
            else
            {
                _logger.Info("未找到EXIF信息: {FilePath}", filePath);
                return null;
            }
        }
        catch (Exception ex)
        {
            // 记录错误但不抛出异常，避免影响主流程
            _logger.Error(ex, "提取EXIF信息失败: {FilePath}", filePath);
            return null;
        }
    }

    /// <summary>
    /// 提取常用的EXIF信息
    /// </summary>
    private Dictionary<string, object> ExtractCommonExifInfo(MagickImage image)
    {
        var commonExif = new Dictionary<string, object>();

        try
        {
            var profile = image.GetExifProfile();
            if (profile == null)
            {
                return commonExif;
            }

            // 相机信息
            AddIfNotNull(commonExif, "Make", profile.GetValue(ExifTag.Make)?.Value);
            AddIfNotNull(commonExif, "Model", profile.GetValue(ExifTag.Model)?.Value);

            // 拍摄信息
            AddIfNotNull(commonExif, "DateTimeOriginal", profile.GetValue(ExifTag.DateTimeOriginal)?.Value);
            AddIfNotNull(commonExif, "ExposureTime", profile.GetValue(ExifTag.ExposureTime)?.Value);
            AddIfNotNull(commonExif, "FNumber", profile.GetValue(ExifTag.FNumber)?.Value);
            AddIfNotNull(commonExif, "ISOSpeedRatings", profile.GetValue(ExifTag.ISOSpeedRatings)?.Value);
            AddIfNotNull(commonExif, "FocalLength", profile.GetValue(ExifTag.FocalLength)?.Value);
            AddIfNotNull(commonExif, "LensModel", profile.GetValue(ExifTag.LensModel)?.Value);

            // GPS信息
            var gpsLatitude = profile.GetValue(ExifTag.GPSLatitude)?.Value;
            var gpsLatitudeRef = profile.GetValue(ExifTag.GPSLatitudeRef)?.Value;
            var gpsLongitude = profile.GetValue(ExifTag.GPSLongitude)?.Value;
            var gpsLongitudeRef = profile.GetValue(ExifTag.GPSLongitudeRef)?.Value;

            if (gpsLatitude != null && gpsLatitudeRef != null && gpsLatitude.Length == 3)
            {
                double degrees = gpsLatitude[0].ToDouble();
                double minutes = gpsLatitude[1].ToDouble();
                double seconds = gpsLatitude[2].ToDouble();
                double decimalLat = degrees + minutes / 60.0 + seconds / 3600.0;
                // 获取纬度参考 (N/S)
                if (gpsLatitudeRef == "S")
                { 
                    decimalLat = -decimalLat;
                }
                commonExif["GPSLatitude"] = decimalLat;
            }

            if (gpsLongitude != null && gpsLongitudeRef != null && gpsLongitude.Length == 3)
            {
                double degrees = gpsLongitude[0].ToDouble();
                double minutes = gpsLongitude[1].ToDouble();
                double seconds = gpsLongitude[2].ToDouble();
                double decimalLat = degrees + minutes / 60.0 + seconds / 3600.0;

                if (gpsLongitudeRef == "W")
                {
                    decimalLat = -decimalLat;
                }
                commonExif["GPSLongitude"] = decimalLat;
            }

            AddIfNotNull(commonExif, "GPSAltitude", profile.GetValue(ExifTag.GPSAltitude)?.Value);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"提取常用EXIF信息失败: {ex.Message}");
        }

        return commonExif;
    }

    /// <summary>
    /// 提取图片基本信息
    /// </summary>
    private Dictionary<string, object> ExtractImageInfo(MagickImage image)
    {
        var imageInfo = new Dictionary<string, object>();

        try
        {
            imageInfo["Width"] = image.Width;
            imageInfo["Height"] = image.Height;
            imageInfo["Format"] = image.Format.ToString();
            imageInfo["ColorSpace"] = image.ColorSpace.ToString();
            imageInfo["ResolutionX"] = image.Density.X;
            imageInfo["ResolutionY"] = image.Density.Y;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"提取图片信息失败: {ex.Message}");
        }

        return imageInfo;
    }

    /// <summary>
    /// 获取标签值
    /// </summary>
    private object? GetTagValue(IExifValue value)
    {
        try
        {
            var tagValue = value.GetValue();

            // 处理不同类型的值
            return tagValue switch
            {
                byte[] bytes => Convert.ToBase64String(bytes),
                _ => tagValue
            };
        }
        catch
        {
            return value.GetValue()?.ToString();
        }
    }

    /// <summary>
    /// 如果值不为 null，添加到字典中
    /// </summary>
    private void AddIfNotNull(Dictionary<string, object> dict, string key, object? value)
    {
        if (value != null && !string.IsNullOrEmpty(value.ToString()))
        {
            dict[key] = value;
        }
    }

    /// <summary>
    /// 批量提取EXIF信息
    /// </summary>
    public Dictionary<string, string?> BatchExtractExifData(List<string> filePaths)
    {
        _logger.Info("开始批量提取EXIF信息 - 文件数量: {FileCount}", filePaths.Count);

        var results = new Dictionary<string, string?>();

        foreach (var filePath in filePaths)
        {
            var exifData = ExtractExifData(filePath);
            results[filePath] = exifData;
        }

        _logger.Info("批量提取EXIF信息完成 - 处理文件数量: {ProcessedCount}", results.Count);

        return results;
    }
}