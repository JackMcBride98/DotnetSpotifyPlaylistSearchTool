using FastEndpoints;
using SpotifyAPI.Web;
using Microsoft.Extensions.Options;

namespace DotnetSpotifyPlaylistSearchTool.Features;

public static class LogIn
{
    public record Response(Uri LoginUri);

    public class Endpoint(IOptions<SpotifyOptions> spotifyOptions) : EndpointWithoutRequest<Response>
    {
        public override void Configure()
        {
            Post("/login");
            AllowAnonymous();
        }

        public override async Task<Response> ExecuteAsync(CancellationToken cancellationToken)
        {
            var loginRequest = new LoginRequest(
                new Uri(spotifyOptions.Value.RedirectUri),
                spotifyOptions.Value.ClientId,
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
