using FastEndpoints;

namespace DotnetSpotifyPlaylistSearchTool.Features;

public class GetPlaylists
{
    public record Request(string SearchTerm);
    
    public record Response(ICollection<PlaylistResponse> Playlists);

    public record PlaylistResponse(string Name);

    public class Endpoint : Endpoint<Request, Response>
    {
        public override void Configure()
        {
            Get("/playlists");
            AllowAnonymous();
        }

        public override async Task<Response> HandleAsync(Request req, CancellationToken ct)
        {
            await Task.Delay(100, ct);
            return new GetPlaylists.Response([]);
        }
    }
}