using System.ComponentModel.DataAnnotations;

namespace DotnetSpotifyPlaylistSearchTool.Database;

public class Playlist
{
    [MaxLength(100)]
    public string Id { get; set; }

    [MaxLength(100)]
    public string Name { get; set; }

    [MaxLength(100)]
    public string OwnerName { get; set; }

    public Image Image { get; set; }

    [MaxLength(100)]
    public string SnapshotId { get; set; }

    public ICollection<User>? Users { get; set; }

    public ICollection<Track>? Tracks { get; set; }
}
