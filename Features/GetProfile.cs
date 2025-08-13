using DotnetSpotifyPlaylistSearchTool.Database;
using DotnetSpotifyPlaylistSearchTool.Services;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using SpotifyAPI.Web;

namespace DotnetSpotifyPlaylistSearchTool.Features;

public class GetProfile
{
    public record Response(PrivateUser User, int TotalPlaylists, string? LastSyncedAt);

    public class Endpoint(DataContext dataContext, ISpotifyAuthService spotifyAuthService) : EndpointWithoutRequest<Response>
    {
        public override void Configure()
        {
            Get("/profile");
            AllowAnonymous();
        }

        public override async Task<Response> ExecuteAsync(CancellationToken ct)
        {
            var spotifyUserProfile = await spotifyAuthService.GetCurrentUserProfileAsync(HttpContext, ct);
            
            var user = await dataContext.Users.Where(u => u.UserId == spotifyUserProfile.Id).SingleAsync(ct);
            
            var lastUpdatedAtOrNull = user.UpdatedAt.HasValue ? user.UpdatedAt.ToString() : null;
            
            var totalPlaylists = await dataContext.Playlists.Include(p => p.Users).CountAsync(p => p.Users!.Any(u => u.UserId == spotifyUserProfile.Id), ct);
            
            return new Response(spotifyUserProfile, totalPlaylists, lastUpdatedAtOrNull);
        }
    }
}
