using DotnetSpotifyPlaylistSearchTool.Database;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using SpotifyAPI.Web;

namespace DotnetSpotifyPlaylistSearchTool.Features;

public class GetProfile
{
    public record Response(PrivateUser User, int TotalPlaylists, string? LastSyncedAt);

    public class Endpoint(DataContext dataContext, IConfiguration configuration) : EndpointWithoutRequest<Response>
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
                if (!HttpContext.Request.Cookies.TryGetValue("RefreshToken", out var refreshToken))
                {
                    ThrowError("Access token or refresh token not found");
                }

                var spotifyClientId = configuration.GetValue<string>("Spotify:ClientId");
                var spotifyClientSecret = configuration.GetValue<string>("Spotify:ClientSecret");

                if (string.IsNullOrWhiteSpace(spotifyClientId) || string.IsNullOrWhiteSpace(spotifyClientSecret))
                {
                    ThrowError("Spotify client id or client secret is missing");
                }

                var response = await new OAuthClient().RequestToken(
                    new AuthorizationCodeRefreshRequest(spotifyClientId, spotifyClientSecret, refreshToken), cancel: ct
                );

                // Set HTTP Cookies for access token, refresh token remains the same
                HttpContext.Response.Cookies.Append("AccessToken", response.AccessToken, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    Expires = DateTimeOffset.UtcNow.AddSeconds(response.ExpiresIn),
                });

                accessToken = response.AccessToken;
            }

            var spotify = new SpotifyClient(accessToken);

            var profile = await spotify.UserProfile.Current(ct);

            var user = await dataContext.Users.Where(u => u.UserId == profile.Id).SingleAsync(ct);

            var lastUpdatedAtOrNull = user.UpdatedAt.HasValue ? user.UpdatedAt.ToString() : null;

            var totalPlaylists = await dataContext.Playlists.Include(p => p.Users).CountAsync(p => p.Users!.Any(u => u.UserId == profile.Id), ct);

            return new Response(profile, totalPlaylists, lastUpdatedAtOrNull);
        }
    }
}
