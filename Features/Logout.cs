using DotnetSpotifyPlaylistSearchTool.Database;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using SpotifyAPI.Web;

namespace DotnetSpotifyPlaylistSearchTool.Features;

public static class Logout
{
    public class Endpoint(DataContext dataContext) : EndpointWithoutRequest<EmptyResponse>
    {
        public override void Configure()
        {
            Post("/logout");
            AllowAnonymous();
        }

        public override async Task<EmptyResponse> ExecuteAsync(CancellationToken ct)
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

            return new EmptyResponse();
        }
    }
}
