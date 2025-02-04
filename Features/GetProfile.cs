using DotnetSpotifyPlaylistSearchTool.Database;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using SpotifyAPI.Web;

namespace DotnetSpotifyPlaylistSearchTool.Features;

public class GetProfile
{
    public record Response(PrivateUser User, int TotalPlaylists);

    public class Endpoint(DataContext dataContext) : EndpointWithoutRequest<Response>
    {
        public override void Configure()
        {
            Get("/profile");
            AllowAnonymous();
        }

        public override async Task<Response> ExecuteAsync(CancellationToken ct)
        {
            if (!HttpContext.Request.Cookies.TryGetValue("AccessToken", out var accessToken))
            {
                ThrowError("Access token not found");
            }

            var spotify = new SpotifyClient(accessToken);

            var profile = await spotify.UserProfile.Current(ct);

            var totalPlaylists = await dataContext.Playlists.Include(p => p.Users).CountAsync(p => p.Users!.Any(u => u.UserId == profile.Id), ct);

            return new Response(profile, totalPlaylists);
        }
    }
}
