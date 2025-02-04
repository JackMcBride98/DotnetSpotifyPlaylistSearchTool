﻿using System.ComponentModel.DataAnnotations;

namespace DotnetSpotifyPlaylistSearchTool.Database;

public class User(string userId, string username, string accessToken, string refreshToken)
{
    [MaxLength(100)]
    public string UserId { get; set; } = userId;

    [MaxLength(100)]
    public string Username { get; set; } = username;

    [MaxLength(500)]
    public string AccessToken { get; set; } = accessToken;

    [MaxLength(500)]
    public string RefreshToken { get; set; } = refreshToken;

    public ICollection<Playlist>? Playlists { get; set; }
}
