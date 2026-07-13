using System.ComponentModel.DataAnnotations;

namespace SpotifyPlaylistSearchTool.Api.Configuration;

public class SpotifyOptions
{
    public const string Position = "Spotify";

    [Required(AllowEmptyStrings = false, ErrorMessage = "Spotify client id is missing")]
    public string ClientId { get; set; } = string.Empty;

    [Required(AllowEmptyStrings = false, ErrorMessage = "Spotify client secret is missing")]
    public string ClientSecret { get; set; } = string.Empty;

    [Required(AllowEmptyStrings = false, ErrorMessage = "Spotify redirect uri is missing")]
    [Url(ErrorMessage = "Spotify redirect uri must be a valid URL")]
    public string RedirectUri { get; set; } = string.Empty;
}
