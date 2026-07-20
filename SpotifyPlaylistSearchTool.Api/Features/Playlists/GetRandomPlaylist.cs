using Microsoft.EntityFrameworkCore;
using SpotifyPlaylistSearchTool.Api.Database;
using SpotifyPlaylistSearchTool.Api.Services;

namespace SpotifyPlaylistSearchTool.Api.Features.Playlists;

public static class GetRandomPlaylist
{
    public record Request(bool OnlyOwnPlaylists);

    public record Response(PlaylistResponse RandomPlaylist);

    public class Endpoint(DataContext dataContext, ISpotifyAuthService spotifyAuthService)
        : Endpoint<Request, Response>
    {
        public override void Configure()
        {
            Get("/random-playlist");
            AllowAnonymous();
        }

        public override async Task<Response> ExecuteAsync(Request request, CancellationToken ct)
        {
            var spotifyUserProfile = await spotifyAuthService.GetCurrentUserProfileAsync(
                HttpContext,
                ct
            );

            var randomPlaylistQuery = dataContext.Playlists.Where(p =>
                p.Users!.Any(u => u.UserId == spotifyUserProfile.Id)
            );

            if (request.OnlyOwnPlaylists)
            {
                randomPlaylistQuery = randomPlaylistQuery.Where(p =>
                    p.OwnerName == spotifyUserProfile.DisplayName
                );
            }

            var randomPlaylistResponse = await randomPlaylistQuery
                .OrderBy(p => EF.Functions.Random())
                .Select(p => new PlaylistResponse(
                    p.PlaylistId,
                    p.Name,
                    p.Description,
                    p.OwnerName,
                    new ImageResponse(p.Image != null ? p.Image.Url : ""),
                    p.Tracks != null
                        ? p
                            .Tracks.Select(t => new TrackResponse(t.Name, t.ArtistName, false))
                            .ToList()
                        : new List<TrackResponse>()
                ))
                .FirstOrDefaultAsync(ct);

            if (randomPlaylistResponse == null)
            {
                ThrowError("User has no playlists or random playlist not found");
            }

            return new Response(randomPlaylistResponse);
        }
    }
}
