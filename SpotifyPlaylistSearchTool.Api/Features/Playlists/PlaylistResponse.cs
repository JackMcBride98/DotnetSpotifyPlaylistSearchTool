namespace SpotifyPlaylistSearchTool.Api.Features.Playlists;

public record PlaylistResponse(
    string Id,
    string Name,
    string Description,
    string OwnerName,
    ImageResponse Image,
    ICollection<TrackResponse> Tracks
);

public record TrackResponse(string Name, string ArtistName, bool Match);

public record ImageResponse(string Url);
