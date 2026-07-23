using SpotifyPlaylistSearchTool.Api.Jobs;
using SpotifyPlaylistSearchTool.Api.Services;
using TickerQ.Utilities.Entities;
using TickerQ.Utilities.Interfaces.Managers;

namespace SpotifyPlaylistSearchTool.Api.Features.Sync;

public static class SyncPlaylists
{
    public record Response(string Message);

    public class Endpoint(
        ISyncSpotifyPlaylistService syncSpotifyPlaylistService,
        ISpotifyAuthService spotifyAuthService,
        ITimeTickerManager<TimeTickerEntity> ticker
    ) : EndpointWithoutRequest<Response>
    {
        public override void Configure()
        {
            Post("/sync-playlists");
            AllowAnonymous();
        }

        public override async Task<Response> ExecuteAsync(CancellationToken ct)
        {
            var spotifyUserProfile = await spotifyAuthService.GetCurrentUserProfileAsync(
                HttpContext,
                ct
            );

            ticker.AddAsync<InitialSyncJob, InitialSyncPayload>(
                DateTime.UtcNow,
                new InitialSyncPayload(spotifyUserProfile.Id),
                ct
            );

            return new Response("Syncing playlists...");
        }
    }
}
