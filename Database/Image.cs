using System.ComponentModel.DataAnnotations;

namespace DotnetSpotifyPlaylistSearchTool.Database;

public class Image
{
    [MaxLength(100)]
    public string Id { get; set; }
    [MaxLength(200)]
    public string Url { get; set; }
    public int Height { get; set; }
    public int Width { get; set; }
}
