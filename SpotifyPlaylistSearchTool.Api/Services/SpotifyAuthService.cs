using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SpotifyAPI.Web;
using SpotifyPlaylistSearchTool.Api.Configuration;
using SpotifyPlaylistSearchTool.Api.Database;

namespace SpotifyPlaylistSearchTool.Api.Services;

public interface ISpotifyAuthService
{
    Task<AuthorizationCodeTokenResponse> RequestTokenAsync(string code, CancellationToken ct);
    Task<SpotifyClient> GetSpotifyClientAsync(HttpContext httpContext, CancellationToken ct);
    Task<PrivateUser> GetCurrentUserProfileAsync(HttpContext httpContext, CancellationToken ct);
}

public class SpotifyAuthService(IOptions<SpotifyOptions> spotifyOptions, DataContext dataContext)
    : ISpotifyAuthService
{
    public async Task<AuthorizationCodeTokenResponse> RequestTokenAsync(
        string code,
        CancellationToken ct
    )
    {
        return await new OAuthClient().RequestToken(
            new AuthorizationCodeTokenRequest(
                spotifyOptions.Value.ClientId,
                spotifyOptions.Value.ClientSecret,
                code,
                new Uri(spotifyOptions.Value.RedirectUri)
            ),
            cancel: ct
        );
    }

    public async Task<SpotifyClient> GetSpotifyClientAsync(
        HttpContext httpContext,
        CancellationToken ct
    )
    {
        var accessToken = httpContext.Request.Cookies["AccessToken"];
        var refreshToken = httpContext.Request.Cookies["RefreshToken"];

        if (string.IsNullOrEmpty(accessToken))
        {
            if (string.IsNullOrEmpty(refreshToken))
            {
                throw new Exception("Access token or refresh token not found");
            }

            var response = await new OAuthClient().RequestToken(
                new AuthorizationCodeRefreshRequest(
                    spotifyOptions.Value.ClientId,
                    spotifyOptions.Value.ClientSecret,
                    refreshToken
                ),
                cancel: ct
            );

            httpContext.Response.Cookies.Append(
                "AccessToken",
                response.AccessToken,
                new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    Expires = DateTimeOffset.UtcNow.AddSeconds(response.ExpiresIn),
                }
            );

            accessToken = response.AccessToken;
        }

        var spotifyClient = new SpotifyClient(accessToken);
        var userId = (await spotifyClient.UserProfile.Current(ct)).Id;

        var user = await dataContext.Users.Where(u => u.UserId == userId).SingleOrDefaultAsync(ct);

        if (user != null)
        {
            user.AccessToken = accessToken;
            await dataContext.SaveChangesAsync(ct);
        }

        return spotifyClient;
    }

    public async Task<PrivateUser> GetCurrentUserProfileAsync(
        HttpContext httpContext,
        CancellationToken ct
    )
    {
        var spotifyClient = await GetSpotifyClientAsync(httpContext, ct);
        var profile = await spotifyClient.UserProfile.Current(ct);

        return profile;
    }
}
