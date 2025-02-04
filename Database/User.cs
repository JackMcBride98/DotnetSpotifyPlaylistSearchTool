using System.ComponentModel.DataAnnotations;

namespace DotnetSpotifyPlaylistSearchTool.Database;

public class User(string id, string username, string accessToken, string refreshToken)
{
    [MaxLength(100)]
    public string Id { get; set; } = id;

    [MaxLength(100)]
    public string Username { get; set; } = username;

    [MaxLength(100)]
    public string AccessToken { get; set; } = accessToken;

    [MaxLength(100)]
    public string RefreshToken { get; set; } = refreshToken;

    public ICollection<Playlist>? Playlists { get; set; }
}
