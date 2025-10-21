namespace TagPhotoAlbum.Server.Models;

public class TagInfo
{
    public string Name { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class TagsResponse
{
    public List<TagInfo> Tags { get; set; } = [];
    public int TotalCount { get; set; }
}