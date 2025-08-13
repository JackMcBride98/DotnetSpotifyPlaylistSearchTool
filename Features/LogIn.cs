using FastEndpoints;
using SpotifyAPI.Web;

namespace DotnetSpotifyPlaylistSearchTool.Features;

public static class LogIn
{
    public record Response(Uri LoginUri);

    public class Endpoint(IConfiguration config) : EndpointWithoutRequest<Response>
    {
        public override void Configure()
        {
            Post("/login");
            AllowAnonymous();
        }

        public override async Task<Response> ExecuteAsync(CancellationToken cancellationToken)
        {
            var spotifyClientId = config.GetValue<string>("Spotify:ClientId");
            if (string.IsNullOrWhiteSpace(spotifyClientId))
            {
                ThrowError("Spotify client id is missing");
            }

            var loginRequest = new LoginRequest(
                new Uri("http://localhost:5030/api/callback"),
                spotifyClientId,
                LoginRequest.ResponseType.Code
            )
            {
                Scope =
                [
                    Scopes.PlaylistReadPrivate,
                    Scopes.PlaylistReadCollaborative,
                    Scopes.UserReadPrivate,
                    Scopes.UserReadEmail,
                ],
            };

            var uri = loginRequest.ToUri();

            return new Response(uri);
        }
    }
}
