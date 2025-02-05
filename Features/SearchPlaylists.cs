using DotnetSpotifyPlaylistSearchTool.Database;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using SpotifyAPI.Web;

namespace DotnetSpotifyPlaylistSearchTool.Features;

public static class SearchPlaylists
{
    public record Request(string SearchTerm, bool ShowOnlyOwnPlaylists);

    public record Response(ICollection<PlaylistResponse> MatchingPlaylists);

    public record PlaylistResponse(string Name, string OwnerName, ImageResponse Image, ICollection<TrackResponse> Tracks);

    public record TrackResponse(string Name, string ArtistName, bool Match);

    public record ImageResponse(string Url);

    public class Endpoint(DataContext dataContext) : Endpoint<Request, Response>
    {
        public override void Configure()
        {
            Get("/search-playlists");
            AllowAnonymous();
        }

        public override async Task<Response> ExecuteAsync(Request request, CancellationToken ct)
        {
            if (!HttpContext.Request.Cookies.TryGetValue("AccessToken", out var accessToken))
            {
                ThrowError("Access token not found");
            }

            var spotify = new SpotifyClient(accessToken);

            var profile = await spotify.UserProfile.Current(ct);

            var userPlaylists = await dataContext.Playlists
                .Include(p => p.Users)
                .Include(p => p.Tracks)
                .Include(p => p.Image)
                .Where(p => p.Users!.Any(u => u.UserId == profile.Id))
                .ToListAsync(ct);

            var matchingPlaylists = userPlaylists
                .Where(p => p.Tracks!.Any(t =>
                    t.Name.Contains(request.SearchTerm, StringComparison.OrdinalIgnoreCase) || t.ArtistName.Contains(request.SearchTerm, StringComparison.OrdinalIgnoreCase)));

            if (request.ShowOnlyOwnPlaylists)
            {
                matchingPlaylists = matchingPlaylists.Where(p => p.OwnerName == profile.DisplayName);
            }

            return new Response(
                    matchingPlaylists.Select(p =>
                        new PlaylistResponse(
                            p.Name,
                            p.OwnerName,
                            new ImageResponse(p.Image!.Url),
                            p.Tracks!.Select(t => new TrackResponse(
                                t.Name,
                                t.ArtistName,
                                t.Name.Contains(request.SearchTerm, StringComparison.OrdinalIgnoreCase) || t.ArtistName.Contains(request.SearchTerm, StringComparison.OrdinalIgnoreCase)
                            )).ToList()
                        )).ToList()
            );
        }
    }
}
