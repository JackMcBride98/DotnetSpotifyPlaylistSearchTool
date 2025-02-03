using FastEndpoints;
using SpotifyAPI.Web;

namespace DotnetSpotifyPlaylistSearchTool.Features;

public class GetProfile
{
    public record Request(string SearchTerm);
    
    public record Response(PrivateUser User);

    public class Endpoint : Endpoint<Request, Response>
    {
        public override void Configure()
        {
            Get("/profile");
            AllowAnonymous();
        }

        public override async Task<Response> ExecuteAsync(Request req, CancellationToken ct)
        {
            await Task.Delay(100, ct);
            if (!HttpContext.Request.Cookies.TryGetValue("AccessToken", out var accessToken))
            {
                ThrowError("Access token not found");
            }

            var spotify = new SpotifyClient(accessToken);

            var profile = await spotify.UserProfile.Current(ct);

            return new Response(profile);
        }
    }
}
