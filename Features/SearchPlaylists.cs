using DotnetSpotifyPlaylistSearchTool.Database;
using DotnetSpotifyPlaylistSearchTool.Services;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using SpotifyAPI.Web;

namespace DotnetSpotifyPlaylistSearchTool.Features;

public static class SearchPlaylists
{
    public record Request(string SearchTerm, bool ShowOnlyOwnPlaylists);

    public record Response(ICollection<PlaylistResponse> MatchingPlaylists);

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

    public class Endpoint(DataContext dataContext, ISpotifyAuthService spotifyAuthService)
        : Endpoint<Request, Response>
    {
        public override void Configure()
        {
            Get("/search-playlists");
            AllowAnonymous();
        }

        public override async Task<Response> ExecuteAsync(Request request, CancellationToken ct)
        {
            var spotifyUserProfile = await spotifyAuthService.GetCurrentUserProfileAsync(
                HttpContext,
                ct
            );

            var userPlaylists = await dataContext
                .Playlists.Include(p => p.Users)
                .Include(p => p.Tracks)
                .Include(p => p.Image)
                .Where(p => p.Users!.Any(u => u.UserId == spotifyUserProfile.Id))
                .ToListAsync(ct);

            var matchingPlaylists = userPlaylists.Where(p =>
                p.Tracks!.Any(t =>
                    t.Name.Contains(request.SearchTerm, StringComparison.OrdinalIgnoreCase)
                    || t.ArtistName.Contains(request.SearchTerm, StringComparison.OrdinalIgnoreCase)
                )
            );

            if (request.ShowOnlyOwnPlaylists)
            {
                matchingPlaylists = matchingPlaylists.Where(p =>
                    p.OwnerName == spotifyUserProfile.DisplayName
                );
            }

            return new Response(
                matchingPlaylists
                    .Select(p => new PlaylistResponse(
                        p.PlaylistId,
                        p.Name,
                        p.Description,
                        p.OwnerName,
                        new ImageResponse(p.Image!.Url),
                        p.Tracks!.Select(t => new TrackResponse(
                                t.Name,
                                t.ArtistName,
                                t.Name.Contains(
                                    request.SearchTerm,
                                    StringComparison.OrdinalIgnoreCase
                                )
                                    || t.ArtistName.Contains(
                                        request.SearchTerm,
                                        StringComparison.OrdinalIgnoreCase
                                    )
                            ))
                            .ToList()
                    ))
                    .ToList()
            );
        }
    }
}
