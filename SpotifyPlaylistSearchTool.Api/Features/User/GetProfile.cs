using Microsoft.EntityFrameworkCore;
using SpotifyAPI.Web;
using SpotifyPlaylistSearchTool.Api.Database;
using SpotifyPlaylistSearchTool.Api.Services;

namespace SpotifyPlaylistSearchTool.Api.Features.User;

public class GetProfile
{
    public record Response(PrivateUser User, int TotalPlaylists, string? LastSyncedAt);

    public class Endpoint(DataContext dataContext, ISpotifyAuthService spotifyAuthService)
        : EndpointWithoutRequest<Response>
    {
        public override void Configure()
        {
            Get("/profile");
            AllowAnonymous();
        }

        public override async Task<Response> ExecuteAsync(CancellationToken ct)
        {
            var spotifyUserProfile = await spotifyAuthService.GetCurrentUserProfileAsync(
                HttpContext,
                ct
            );

            var profileData = await dataContext
                .Users.Where(u => u.UserId == spotifyUserProfile.Id)
                .Select(u => new
                {
                    UpdatedAt = u.UpdatedAt,
                    PlaylistCount = u.Playlists != null ? u.Playlists.Count : 0,
                })
                .SingleOrDefaultAsync(ct);

            if (profileData == null)
            {
                ThrowError("User not found, try logging in again");
            }

            var lastUpdatedAtOrNull = profileData.UpdatedAt.HasValue
                ? profileData.UpdatedAt.ToString()
                : null;

            return new Response(spotifyUserProfile, profileData.PlaylistCount, lastUpdatedAtOrNull);
        }
    }
}
