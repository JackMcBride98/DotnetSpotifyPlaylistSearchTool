using DotnetSpotifyPlaylistSearchTool.Database;
using DotnetSpotifyPlaylistSearchTool.Services;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace DotnetSpotifyPlaylistSearchTool.Features;

public static class SyncProgress
{
    public record Response(int? TotalPlaylists, int SyncedPlaylists);

    public class Endpoint(DataContext dataContext, ISpotifyAuthService spotifyAuthService)
        : EndpointWithoutRequest<Response>
    {
        public override void Configure()
        {
            Get("/sync-progress");
            AllowAnonymous();
        }

        public override async Task<Response> ExecuteAsync(CancellationToken ct)
        {
            var spotifyUserProfile = await spotifyAuthService.GetCurrentUserProfileAsync(
                HttpContext,
                ct
            );

            var user = await dataContext.Users.SingleOrDefaultAsync(
                u => u.UserId == spotifyUserProfile.Id,
                ct
            );

            if (user == null)
            {
                ThrowError("User not found");
            }

            var totalPlaylists = user.FirstSyncTotalPlaylists;
            var syncedPlaylists = await dataContext.Playlists.CountAsync(
                p => p.Users!.Any(u => u.UserId == user.UserId),
                ct
            );

            return new Response(totalPlaylists, syncedPlaylists);
        }
    }
}
