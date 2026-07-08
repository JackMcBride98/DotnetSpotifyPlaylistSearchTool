using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using SpotifyAPI.Web;
using SpotifyPlaylistSearchTool.Api.Database;

namespace SpotifyPlaylistSearchTool.Api.Features;

public static class Logout
{
    public class Endpoint(DataContext dataContext) : EndpointWithoutRequest<FastEndpoints.Void>
    {
        public override void Configure()
        {
            Post("/logout");
            AllowAnonymous();
        }

        public override async Task<FastEndpoints.Void> ExecuteAsync(CancellationToken ct)
        {
            if (!HttpContext.Request.Cookies.TryGetValue("AccessToken", out var accessToken))
            {
                ThrowError("Access token not found");
            }

            var spotify = new SpotifyClient(accessToken);

            var spotifyUser = await spotify.UserProfile.Current(ct);

            var user = await dataContext
                .Users.Where(u => u.UserId == spotifyUser.Id)
                .SingleOrDefaultAsync(ct);

            if (user == null)
            {
                ThrowError("User not found");
            }

            user.AccessToken = null;

            HttpContext.Response.Cookies.Delete("AccessToken");
            HttpContext.Response.Cookies.Delete("RefreshToken");

            await dataContext.SaveChangesAsync(ct);

            return await Send.NoContentAsync(ct);
        }
    }
}
