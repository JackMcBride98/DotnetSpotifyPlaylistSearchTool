using System.ComponentModel.DataAnnotations;

namespace DotnetSpotifyPlaylistSearchTool.Database;

public class Track
{
    [MaxLength(100)]
    public string Id { get; set; }
    [MaxLength(200)]
    public string Name { get; set; }
    [MaxLength(200)]
    public string ArtistName { get; set; }
    [MaxLength(100)]
    public string PlaylistId { get; set; }
}
