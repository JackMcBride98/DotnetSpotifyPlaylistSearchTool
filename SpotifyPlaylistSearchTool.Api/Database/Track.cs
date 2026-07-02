using System.ComponentModel.DataAnnotations;

namespace DotnetSpotifyPlaylistSearchTool.Database;

public class Track(int index, string name, string artistName, string playlistId)
{
    public int TrackId { get; set; }

    public int Index { get; set; } = index;

    [MaxLength(5000)]
    public string Name { get; set; } = name;

    [MaxLength(5000)]
    public string ArtistName { get; set; } = artistName;

    [MaxLength(100)]
    public string PlaylistId { get; set; } = playlistId;
}
