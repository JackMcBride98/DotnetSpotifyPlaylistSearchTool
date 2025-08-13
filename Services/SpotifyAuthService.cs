using SpotifyAPI.Web;
using DotnetSpotifyPlaylistSearchTool.Database;
using Microsoft.EntityFrameworkCore;

namespace DotnetSpotifyPlaylistSearchTool.Services;

public interface ISpotifyAuthService
{
  Task<SpotifyClient> GetSpotifyClientAsync(HttpContext httpContext, CancellationToken ct);
  Task<PrivateUser> GetCurrentUserProfileAsync(HttpContext httpContext, CancellationToken ct);
}

public class SpotifyAuthService(IConfiguration configuration, DataContext dataContext) : ISpotifyAuthService
{
  public async Task<SpotifyClient> GetSpotifyClientAsync(HttpContext httpContext, CancellationToken ct)
  {
    var accessToken = httpContext.Request.Cookies["AccessToken"];
    var refreshToken = httpContext.Request.Cookies["RefreshToken"];

    if (string.IsNullOrEmpty(accessToken))
    {
      if (string.IsNullOrEmpty(refreshToken))
      {
        throw new Exception("Access token or refresh token not found");
      }

      var clientId = configuration["Spotify:ClientId"];
      var clientSecret = configuration["Spotify:ClientSecret"];

      if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
      {
        throw new Exception("Spotify client id or client secret is missing");
      }

      var response = await new OAuthClient().RequestToken(
          new AuthorizationCodeRefreshRequest(clientId, clientSecret, refreshToken), cancel: ct
      );

      httpContext.Response.Cookies.Append("AccessToken", response.AccessToken, new CookieOptions
      {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.Strict,
        Expires = DateTimeOffset.UtcNow.AddSeconds(response.ExpiresIn),
      });

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

  public async Task<PrivateUser> GetCurrentUserProfileAsync(HttpContext httpContext, CancellationToken ct)
  {
    var spotifyClient = await GetSpotifyClientAsync(httpContext, ct);
    var profile = await spotifyClient.UserProfile.Current(ct);

    return profile;
  }
}
