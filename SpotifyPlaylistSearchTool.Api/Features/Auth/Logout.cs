using Microsoft.EntityFrameworkCore;
using SpotifyPlaylistSearchTool.Api.Database;
using SpotifyPlaylistSearchTool.Api.Services;
using Void = FastEndpoints.Void;

namespace SpotifyPlaylistSearchTool.Api.Features.Auth;

public static class Logout
{
    public class Endpoint(DataContext dataContext, ISpotifyAuthService spotifyAuthService)
        : EndpointWithoutRequest<Void>
    {
        public override void Configure()
        {
            Post("/logout");
            AllowAnonymous();
        }

        public override async Task<Void> ExecuteAsync(CancellationToken ct)
        {
            var spotifyUser = await spotifyAuthService.GetCurrentUserProfileAsync(HttpContext, ct);

            var user = await dataContext
                .Users.Where(u => u.UserId == spotifyUser.Id)
                .SingleOrDefaultAsync(ct);

            if (user == null)
            {
                ThrowError("User not found", 404);
            }

            user.AccessToken = null;

            HttpContext.Response.Cookies.Delete("AccessToken");
            HttpContext.Response.Cookies.Delete("RefreshToken");

            await dataContext.SaveChangesAsync(ct);

            return await Send.NoContentAsync(ct);
        }
    }
}
