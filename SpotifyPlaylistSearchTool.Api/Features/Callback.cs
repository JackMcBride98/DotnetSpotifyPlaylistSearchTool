using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SpotifyAPI.Web;
using SpotifyPlaylistSearchTool.Api.Configuration;
using SpotifyPlaylistSearchTool.Api.Database;

namespace SpotifyPlaylistSearchTool.Api.Features;

public static class Callback
{
    public record Request(string Code);

    public class Endpoint(DataContext dataContext, IOptions<SpotifyOptions> spotifyOptions)
        : Endpoint<Request, EmptyResponse>
    {
        public override void Configure()
        {
            Get("/callback");
            AllowAnonymous();
        }

        public override async Task<FastEndpoints.Void> HandleAsync(
            Request req,
            CancellationToken ct
        )
        {
            var response = await new OAuthClient().RequestToken(
                new AuthorizationCodeTokenRequest(
                    spotifyOptions.Value.ClientId,
                    spotifyOptions.Value.ClientSecret,
                    req.Code,
                    new Uri(spotifyOptions.Value.RedirectUri)
                ),
                cancel: ct
            );

            // Set HTTP Cookies for access and refresh tokens
            HttpContext.Response.Cookies.Append(
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

            HttpContext.Response.Cookies.Append(
                "RefreshToken",
                response.RefreshToken,
                new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    Expires = DateTimeOffset.UtcNow.AddDays(90),
                }
            );

            var spotify = new SpotifyClient(response.AccessToken);
            var currentUser = await spotify.UserProfile.Current(ct);

            var userOrNull = await dataContext.Users.SingleOrDefaultAsync(
                u => u.UserId == currentUser.Id,
                ct
            );

            if (userOrNull == null)
            {
                var newUser = new User(
                    currentUser.Id,
                    currentUser.DisplayName,
                    response.AccessToken,
                    response.RefreshToken
                );
                dataContext.Users.Add(newUser);
            }
            else
            {
                userOrNull.AccessToken = response.AccessToken;
                userOrNull.RefreshToken = response.RefreshToken;
            }

            await dataContext.SaveChangesAsync(ct);
            return await Send.ResultAsync(Results.Redirect("/profile"));
        }
    }
}
