using DotnetSpotifyPlaylistSearchTool.Database;
using DotnetSpotifyPlaylistSearchTool.Services;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using SpotifyAPI.Web;

namespace DotnetSpotifyPlaylistSearchTool.Features;

public static class SyncPlaylists
{
    public record Response(string Message);

    public class Endpoint(ISyncSpotifyPlaylistService syncSpotifyPlaylistService, DataContext dataContext) : EndpointWithoutRequest<Response>
    {
        public override void Configure()
        {
            Post("/sync-playlists");
            AllowAnonymous();
        }

        public override async Task<Response> ExecuteAsync(CancellationToken ct)
        {
            if (!HttpContext.Request.Cookies.TryGetValue("AccessToken", out var accessToken))
            {
                ThrowError("Access token not found");
            }

            var spotify = new SpotifyClient(accessToken);
            var currentUser = await spotify.UserProfile.Current(ct);

            var anyExistingPlaylistsForCurrentUser = await dataContext.Playlists.Include(p => p.Users).AnyAsync(p => p.Users!.Any(u => u.UserId == currentUser.Id), ct);

            if (anyExistingPlaylistsForCurrentUser)
            {
                ThrowError("Playlists already in db, sync not allowed except from background job.");
            }

            await syncSpotifyPlaylistService.SyncSpotifyPlaylistAsync(currentUser.Id);

            return new Response("Syncing playlists...");
        }
    }
}
