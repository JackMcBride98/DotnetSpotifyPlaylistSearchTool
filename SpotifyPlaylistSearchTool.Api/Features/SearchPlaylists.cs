using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using SpotifyPlaylistSearchTool.Api.Database;
using SpotifyPlaylistSearchTool.Api.Services;

namespace SpotifyPlaylistSearchTool.Api.Features;

public static class SearchPlaylists
{
    public record Request(string SearchTerm, bool ShowOnlyOwnPlaylists);

    public record Response(ICollection<PlaylistResponse> MatchingPlaylists, int TotalPlaylists);

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

            var totalUserPlaylists = await dataContext
                .Users.Where(u => u.UserId == spotifyUserProfile.Id)
                .Select(u => u.Playlists!.Count)
                .FirstOrDefaultAsync(ct);

            var query = dataContext
                .Playlists.AsSplitQuery()
                .Include(p => p.Users)
                .Include(p => p.Tracks)
                .Include(p => p.Image)
                .Where(p => p.Users!.Any(u => u.UserId == spotifyUserProfile.Id));

            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var lowerSearch = request.SearchTerm.ToLower();

                query = query.Where(p =>
                    p.Tracks!.Any(t =>
                        t.Name.ToLower().Contains(lowerSearch)
                        || t.ArtistName.ToLower().Contains(lowerSearch)
                    )
                );
            }

            if (request.ShowOnlyOwnPlaylists)
            {
                query = query.Where(p => p.OwnerName == spotifyUserProfile.DisplayName);
            }

            var matchingPlaylists = await query.ToListAsync(ct);

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
                    .ToList(),
                totalUserPlaylists
            );
        }
    }
}
