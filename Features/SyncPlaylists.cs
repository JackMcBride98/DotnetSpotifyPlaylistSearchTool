using FastEndpoints;

namespace DotnetSpotifyPlaylistSearchTool.Features;

public static class SyncPlaylists
{
    public record Response(string Message);

    public class Endpoint : EndpointWithoutRequest<Response>
    {
        public override void Configure()
        {
            Get("/profile");
            AllowAnonymous();
        }

        public override async Task<Response> ExecuteAsync(CancellationToken ct)
        {
            if (!HttpContext.Request.Cookies.TryGetValue("AccessToken", out var accessToken))
            {
                ThrowError("Access token not found");
            }

            // If playlists already in db, sync not allowed.

            return new Response("Syncing playlists...");
        }
    }
}
