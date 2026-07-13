using FastEndpoints;
using Microsoft.Extensions.Options;
using SpotifyAPI.Web;
using SpotifyPlaylistSearchTool.Api.Configuration;

namespace SpotifyPlaylistSearchTool.Api.Features;

public static class LogIn
{
    public record Response(Uri LoginUri);

    public class Endpoint(IOptions<SpotifyOptions> spotifyOptions)
        : EndpointWithoutRequest<Response>
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
                    SpotifyAPI.Web.Scopes.PlaylistReadPrivate,
                    SpotifyAPI.Web.Scopes.PlaylistReadCollaborative,
                    SpotifyAPI.Web.Scopes.UserReadPrivate,
                    SpotifyAPI.Web.Scopes.UserReadEmail,
                ],
            };

            var uri = loginRequest.ToUri();

            return new Response(uri);
        }
    }
}
