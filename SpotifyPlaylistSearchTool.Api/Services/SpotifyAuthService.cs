using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SpotifyAPI.Web;
using SpotifyPlaylistSearchTool.Api.Configuration;
using SpotifyPlaylistSearchTool.Api.Database;

namespace SpotifyPlaylistSearchTool.Api.Services;

public interface ISpotifyAuthService
{
    Task<AuthorizationCodeTokenResponse> RequestTokenAsync(string code, CancellationToken ct);
    Task<SpotifyClient> GetSpotifyClientAsync(
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

    Task<PrivateUser> GetCurrentUserProfileAsync(
        HttpContext httpContext,
        CancellationToken ct
    );
}

public class SpotifyAuthService(IOptions<SpotifyOptions> spotifyOptions, DataContext dataContext)
    : ISpotifyAuthService
{
    private class UserState
    {
        public string? UserId { get; set; }
    }

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

        var client = new SpotifyClient(tokenResponse.AccessToken);

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

    public async Task<SpotifyClient> GetSpotifyClientAsync(
        HttpContext httpContext,
        CancellationToken ct,
        string? passedAccessToken = null,
        string? passedRefreshToken = null
    )
    {
        var accessToken = passedAccessToken ?? httpContext.Request.Cookies["AccessToken"];
        var refreshToken = passedRefreshToken ?? httpContext.Request.Cookies["RefreshToken"];

        if (string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(refreshToken))
        {
            throw new InvalidOperationException(
                "Access token or refresh token not found in HttpContext."
            );
        }

        var tokenResponse = new AuthorizationCodeTokenResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            CreatedAt = DateTime.UtcNow,
            ExpiresIn = 3600,
            TokenType = "Bearer",
        };

        var authenticator = new AuthorizationCodeAuthenticator(
            spotifyOptions.Value.ClientId,
            spotifyOptions.Value.ClientSecret,
            tokenResponse
        );

        var userState = new UserState();
        var wasRefreshed = false;

        authenticator.TokenRefreshed += async (sender, tokenRefreshedResponse) =>
        {
            wasRefreshed = true;

            accessToken = tokenRefreshedResponse.AccessToken;
            refreshToken = tokenRefreshedResponse.RefreshToken;

            SetTokenHttpContextCookies(httpContext, accessToken, refreshToken);

            if (!string.IsNullOrEmpty(userState.UserId))
            {
                Console.WriteLine($"User {userState.UserId} Tokens Refreshed and updated in database");
                await UpdateUserTokensInDatabaseAsync(
                    userState.UserId,
                    accessToken,
                    refreshToken,
                    ct
                );
            }
        };

        var config = SpotifyClientConfig.CreateDefault().WithAuthenticator(authenticator);
        var client = new SpotifyClient(config);

        var profile = await client.UserProfile.Current(ct);
        userState.UserId = profile.Id;

        if (wasRefreshed)
        {
            Console.WriteLine($"User {userState.UserId} Tokens Refreshed and updated in database (In first request to get client)");
            await UpdateUserTokensInDatabaseAsync(userState.UserId, accessToken, refreshToken, ct);
        }

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
            // Log!
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
