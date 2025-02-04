using System.ComponentModel.DataAnnotations;

namespace DotnetSpotifyPlaylistSearchTool.Database;

public class Playlist(string playlistId, string name, string ownerName, string snapshotId)
{
    [MaxLength(100)]
    public string PlaylistId { get; set; } = playlistId;

    [MaxLength(100)]
    public string Name { get; set; } = name;

    [MaxLength(100)]
    public string OwnerName { get; set; } = ownerName;

    public Image? Image { get; set; }

    [MaxLength(100)]
    public string SnapshotId { get; set; } = snapshotId;

    public ICollection<User>? Users { get; set; }

    public ICollection<Track>? Tracks { get; set; }
}
