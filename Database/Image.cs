using System.ComponentModel.DataAnnotations;

namespace DotnetSpotifyPlaylistSearchTool.Database;

public class Image(string url, int height, int width)
{
    public int ImageId { get; set; }

    [MaxLength(200)]
    public string Url { get; set; } = url;

    public int Height { get; set; } = height;

    public int Width { get; set; } = width;
}
