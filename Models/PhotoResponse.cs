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
   
    public DateTime Date { get; set; } = DateTime.UtcNow;

    public double FileSizeKB { get; set; } // 文件大小，单位为KB

    public string? CompressedFilePath { get; set; }

    public bool HasCompressedImage { get; set; }
}