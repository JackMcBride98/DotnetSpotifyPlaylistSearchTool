using Microsoft.EntityFrameworkCore;
using SpotifyPlaylistSearchTool.Api.Database;
using SpotifyPlaylistSearchTool.Api.Services;

namespace SpotifyPlaylistSearchTool.Api.Features.Playlists;

public static class SearchPlaylists
{
    public record Request(string SearchTerm, bool ShowOnlyOwnPlaylists);

    public record Response(ICollection<PlaylistResponse> MatchingPlaylists, int TotalPlaylists);

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

            var baseQuery = dataContext.Playlists.Where(p =>
                p.Users!.Any(u => u.UserId == spotifyUserProfile.Id)
            );

            if (request.ShowOnlyOwnPlaylists)
            {
                baseQuery = baseQuery.Where(p => p.OwnerName == spotifyUserProfile.DisplayName);
            }

            var totalUserPlaylists = await baseQuery.CountAsync(ct);

            var filteredQuery = baseQuery;

            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var lowerSearch = request.SearchTerm.ToLower();

                filteredQuery = filteredQuery.Where(p =>
                    p.Tracks!.Any(t =>
                        t.Name.ToLower().Contains(lowerSearch)
                        || t.ArtistName.ToLower().Contains(lowerSearch)
                    )
                );
            }

            var matchingPlaylists = await filteredQuery
                .Select(p => new PlaylistResponse(
                    p.PlaylistId,
                    p.Name,
                    p.Description,
                    p.OwnerName,
                    new ImageResponse(p.Image!.Url),
                    p.Tracks!.Select(t => new TrackResponse(
                            t.Name,
                            t.ArtistName,
                            t.Name.Contains(request.SearchTerm, StringComparison.OrdinalIgnoreCase)
                                || t.ArtistName.Contains(
                                    request.SearchTerm,
                                    StringComparison.OrdinalIgnoreCase
                                )
                        ))
                        .ToList()
                ))
                .ToListAsync(ct);

            return new Response(matchingPlaylists, totalUserPlaylists);
        }
    }
}
