namespace TagPhotoAlbum.Server.Models;

public class Photo
{
    public int Id { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ICollection<PhotoTag> Tags { get; set; } = [];
    public string Folder { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public DateTime Date { get; set; } = DateTime.UtcNow;
    public double FileSizeKB { get; set; } // 文件大小，单位为KB
}

public class Tag
{
    public int Id { get; set; }
    public string Name { get; set; }
    public ICollection<PhotoTag> PhotoTags { get; set; } // �������Ե�������
}

// PhotoTag (������/����ʵ��)
public class PhotoTag
{
    public int PhotoId { get; set; }
    public Photo Photo { get; set; }

    public int TagId { get; set; }
    public Tag Tag { get; set; }
}