using DotnetSpotifyPlaylistSearchTool.Services;
using FastEndpoints;

namespace DotnetSpotifyPlaylistSearchTool.Features;

public static class SyncPlaylists
{
    public record Response(string Message);

    public class Endpoint(
        ISyncSpotifyPlaylistService syncSpotifyPlaylistService,
        ISpotifyAuthService spotifyAuthService
    ) : EndpointWithoutRequest<Response>
    {
        public override void Configure()
        {
            Post("/sync-playlists");
            AllowAnonymous();
        }

        public override async Task<Response> ExecuteAsync(CancellationToken ct)
        {
            var spotifyClient = await spotifyAuthService.GetSpotifyClientAsync(HttpContext, ct);

            await syncSpotifyPlaylistService.SyncSpotifyPlaylistAsync(
                spotifyClient,
                requiresProgressUpdates: true
            );

            return new Response("Syncing playlists...");
        }
    }
}
