using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SpotifyAPI.Web;
using SpotifyPlaylistSearchTool.Api.Configuration;
using SpotifyPlaylistSearchTool.Api.Database;
using SpotifyPlaylistSearchTool.Api.Services;
using Void = FastEndpoints.Void;

namespace SpotifyPlaylistSearchTool.Api.Features;

public static class Callback
{
    public record Request(string Code);

    public class Validator : Validator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.Code).NotEmpty().WithMessage("Authorization code is required.");
        }
    }

    public class Endpoint(DataContext dataContext, ISpotifyAuthService spotifyAuthService)
        : Endpoint<Request, EmptyResponse>
    {
        public override void Configure()
        {
            Get("/callback");
            AllowAnonymous();
        }

        public override async Task<Void> HandleAsync(Request req, CancellationToken ct)
        {
            AuthorizationCodeTokenResponse response;

            try
            {
                response = await spotifyAuthService.RequestTokenAsync(req.Code, ct);
            }
            catch (APIException ex)
            {
                // Add logging here
                AddError(r => r.Code, $"Spotify authentication failed: {ex.Message}");

                await Send.ErrorsAsync(StatusCodes.Status400BadRequest, ct);
                return new Void();
            }

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

            var currentUser = await spotifyAuthService.GetCurrentUserProfileAsync(HttpContext, ct);

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
                userOrNull.Username = currentUser.DisplayName;
                userOrNull.AccessToken = response.AccessToken;
                userOrNull.RefreshToken = response.RefreshToken;
            }

            await dataContext.SaveChangesAsync(ct);
            return await Send.ResultAsync(Results.Redirect("/profile"));
        }
    }
}
