using Microsoft.EntityFrameworkCore;
using SpotifyPlaylistSearchTool.Api.Database;
using SpotifyPlaylistSearchTool.Api.Services;

namespace SpotifyPlaylistSearchTool.Api.Features.User;

public class GetProfile
{
    public record UserProfileResponse(
        string Id,
        string DisplayName,
        string? ProfileImageUrl,
        int TotalPlaylists
    );

    public record SyncStatusResponse(
        SyncStatus Status,
        int? TotalPlaylists,
        string? ErrorMessage,
        DateTimeOffset? CompletedAt
    );

    public record Response(UserProfileResponse User, SyncStatusResponse SyncStatus);

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
                .Users.AsNoTracking()
                .Where(u => u.UserId == spotifyUserProfile.Id)
                .Select(u => new
                {
                    PlaylistCount = u.Playlists != null ? u.Playlists.Count : 0,
                    SyncStatus = u.SyncState.Status,
                    SyncErrorMessage = u.SyncState.ErrorMessage,
                    SyncCompletedAt = u.SyncState.CompletedAt,
                    SyncTotalPlaylists = u.SyncState.TotalPlaylists,
                })
                .SingleOrDefaultAsync(ct);

            if (profileData == null)
            {
                ThrowError("User not found, try logging in again", 404);
            }

            return new Response(
                new UserProfileResponse(
                    spotifyUserProfile.Id,
                    spotifyUserProfile.DisplayName,
                    spotifyUserProfile.Images.FirstOrDefault()?.Url,
                    profileData.PlaylistCount
                ),
                new SyncStatusResponse(
                    profileData.SyncStatus,
                    profileData.SyncTotalPlaylists,
                    profileData.SyncErrorMessage,
                    profileData.SyncCompletedAt?.ToDateTimeOffset()
                )
            );
        }
    }
}
