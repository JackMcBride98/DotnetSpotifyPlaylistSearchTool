using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SpotifyAPI.Web;
using SpotifyPlaylistSearchTool.Api.Configuration;
using SpotifyPlaylistSearchTool.Api.Database;

namespace SpotifyPlaylistSearchTool.Api.Services;

public interface ISpotifyAuthService
{
    Task<ISpotifyClient> GetSpotifyClientAsync(
        HttpContext httpContext,
        CancellationToken ct,
        string? passedAccessToken = null,
        string? passedRefreshToken = null
    );

    Task HandleCallbackAndUpsertUserAsync(
        string code,
        HttpContext httpContext,
        CancellationToken ct
    );

    Task<PrivateUser> GetCurrentUserProfileAsync(HttpContext httpContext, CancellationToken ct);
}

public class SpotifyAuthService(
    IOptions<SpotifyOptions> spotifyOptions,
    DataContext dataContext,
    ISpotifyClientFactory spotifyClientFactory
) : ISpotifyAuthService
{
    private class UserState
    {
        public string? UserId { get; set; }
    }

    public virtual async Task<AuthorizationCodeTokenResponse> RequestTokenAsync(
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

    public virtual async Task<AuthorizationCodeRefreshResponse> RefreshTokenAsync(
        string refreshToken,
        CancellationToken ct
    )
    {
        return await new OAuthClient().RequestToken(
            new AuthorizationCodeRefreshRequest(
                spotifyOptions.Value.ClientId,
                spotifyOptions.Value.ClientSecret,
                refreshToken
            ),
            cancel: ct
        );
    }

    public async Task HandleCallbackAndUpsertUserAsync(
        string code,
        HttpContext httpContext,
        CancellationToken ct
    )
    {
        var tokenResponse = await RequestTokenAsync(code, ct);

        SetTokenHttpContextCookies(
            httpContext,
            tokenResponse.AccessToken,
            tokenResponse.RefreshToken
        );

        var client = spotifyClientFactory.CreateClient(tokenResponse.AccessToken);
        var profile = await client.UserProfile.Current(ct);

        var user = await dataContext.Users.SingleOrDefaultAsync(u => u.UserId == profile.Id, ct);

        if (user == null)
        {
            user = new User(
                profile.Id,
                profile.DisplayName,
                tokenResponse.AccessToken,
                tokenResponse.RefreshToken
            );
            dataContext.Users.Add(user);
        }
        else
        {
            user.Username = profile.DisplayName;
            user.AccessToken = tokenResponse.AccessToken;
            user.RefreshToken = tokenResponse.RefreshToken;
        }

        await dataContext.SaveChangesAsync(ct);
    }

    public async Task<ISpotifyClient> GetSpotifyClientAsync(
        HttpContext httpContext,
        CancellationToken ct,
        string? passedAccessToken = null,
        string? passedRefreshToken = null
    )
    {
        var accessToken = passedAccessToken ?? httpContext.Request.Cookies["AccessToken"];
        var refreshToken = passedRefreshToken ?? httpContext.Request.Cookies["RefreshToken"];

        if (string.IsNullOrEmpty(refreshToken))
        {
            throw new InvalidOperationException(
                "Refresh token not found in HttpContext or parameters."
            );
        }

        if (string.IsNullOrEmpty(accessToken))
        {
            return await RefreshTokensAndCreateClientAsync(httpContext, refreshToken, ct);
        }

        var client = spotifyClientFactory.CreateClient(accessToken);
        return client;
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

    private async Task<ISpotifyClient> RefreshTokensAndCreateClientAsync(
        HttpContext httpContext,
        string refreshToken,
        CancellationToken ct
    )
    {
        var tokenResponse = await RefreshTokenAsync(refreshToken, ct);
        var newAccessToken = tokenResponse.AccessToken;
        var newRefreshToken = tokenResponse.RefreshToken ?? refreshToken;

        var client = spotifyClientFactory.CreateClient(newAccessToken);

        var profile = await client.UserProfile.Current(ct);

        SetTokenHttpContextCookies(httpContext, newAccessToken, newRefreshToken);
        await UpdateUserTokensInDatabaseAsync(profile.Id, newAccessToken, newRefreshToken, ct);

        return client;
    }

    private async Task UpdateUserTokensInDatabaseAsync(
        string userId,
        string accessToken,
        string? refreshToken,
        CancellationToken ct
    )
    {
        var user = await dataContext.Users.SingleOrDefaultAsync(u => u.UserId == userId, ct);

        if (user != null)
        {
            user.AccessToken = accessToken;
            user.RefreshToken = refreshToken;
            await dataContext.SaveChangesAsync(ct);
        }
        else
        {
            Console.WriteLine("User not found when trying to update tokens in database.");
        }
    }

    private void SetTokenHttpContextCookies(
        HttpContext httpContext,
        string accessToken,
        string? refreshToken
    )
    {
        httpContext.Response.Cookies.Append(
            "AccessToken",
            accessToken,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTimeOffset.UtcNow.AddSeconds(3600),
            }
        );

        if (!string.IsNullOrEmpty(refreshToken))
        {
            httpContext.Response.Cookies.Append(
                "RefreshToken",
                refreshToken,
                new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    Expires = DateTimeOffset.UtcNow.AddDays(90),
                }
            );
        }
    }
}
