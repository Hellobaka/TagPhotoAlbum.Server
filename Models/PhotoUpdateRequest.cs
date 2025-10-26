namespace TagPhotoAlbum.Server.Models;

public class PhotoUpdateRequest
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = [];
    public string Folder { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public double? Rating { get; set; } = 0;
}