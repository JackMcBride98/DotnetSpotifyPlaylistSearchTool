using FastEndpoints;
using SpotifyAPI.Web;

namespace DotnetSpotifyPlaylistSearchTool.Features;

public static class Callback
{
    public record Request(string Code);

    public class Endpoint(IConfiguration configuration) : Endpoint<Request, EmptyResponse>
    {
        public override void Configure()
        {
            Get("/callback");
            AllowAnonymous();
        }

        public override async Task HandleAsync(Request req, CancellationToken ct)
        {
            var spotifyClientId = configuration.GetValue<string>("Spotify:ClientId");
            var spotifyClientSecret = configuration.GetValue<string>("Spotify:ClientSecret");

            if (string.IsNullOrWhiteSpace(spotifyClientId) || string.IsNullOrWhiteSpace(spotifyClientSecret))
            {
                ThrowError("Spotify client id or client secret is missing");
            }

            var response = await new OAuthClient().RequestToken(
                new AuthorizationCodeTokenRequest(spotifyClientId, spotifyClientSecret, req.Code, new Uri("http://localhost:5030/api/callback")), cancel: ct
            );

            // Set HTTP Cookies for access and refresh tokens
            HttpContext.Response.Cookies.Append("AccessToken", response.AccessToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTimeOffset.UtcNow.AddSeconds(response.ExpiresIn),
            });

            HttpContext.Response.Cookies.Append("RefreshToken", response.RefreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTimeOffset.UtcNow.AddDays(90),
            });

            await SendRedirectAsync("/profile");
        }
    }
}
