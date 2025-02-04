using System.ComponentModel.DataAnnotations;

namespace DotnetSpotifyPlaylistSearchTool.Database;

public class Track(int index, string name, string artistName, string playlistId)
{
    public int TrackId { get; set; }

    public int Index { get; set; } = index;

    [MaxLength(200)]
    public string Name { get; set; } = name;

    [MaxLength(200)]
    public string ArtistName { get; set; } = artistName;

    [MaxLength(100)]
    public string PlaylistId { get; set; } = playlistId;
}
