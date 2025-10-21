namespace TagPhotoAlbum.Server.Models;

public class PhotoCreateRequest
{
    public string FilePath { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = [];
    public string Folder { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
}