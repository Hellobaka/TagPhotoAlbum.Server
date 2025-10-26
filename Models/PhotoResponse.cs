namespace TagPhotoAlbum.Server.Models;

public class PhotoResponse
{
    public int Id { get; set; }

    public string FilePath { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public List<string> Tags { get; set; } = [];

    public string Folder { get; set; } = string.Empty;

    public string Location { get; set; } = string.Empty;

    public DateTime Date { get; set; } = DateTime.Now;

    public double FileSizeKB { get; set; } // 文件大小，单位为KB

    public string? ExifData { get; set; } // EXIF元数据，JSON格式

    public string? CompressedFilePath { get; set; }

    public bool HasCompressedImage { get; set; }

    public double Rating { get; set; } // 评分，支持半分，范围0-5，默认0
}

public class UpdateRatingRequest
{
    public double Rating { get; set; }
}