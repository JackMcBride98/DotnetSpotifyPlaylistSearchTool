using DotnetSpotifyPlaylistSearchTool.Database;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using SpotifyAPI.Web;

namespace DotnetSpotifyPlaylistSearchTool.Features;

public static class GetRandomPlaylist
{
    public record Request(bool OnlyOwnPlaylists);

    public record Response(SearchPlaylists.PlaylistResponse RandomPlaylist);

    public class Endpoint(DataContext dataContext) : Endpoint<Request, Response>
    {
        public override void Configure()
        {
            Get("/random-playlist");
            AllowAnonymous();
        }

        public override async Task<Response> ExecuteAsync(Request request, CancellationToken ct)
        {
            if (!HttpContext.Request.Cookies.TryGetValue("AccessToken", out var accessToken))
            {
                ThrowError("Access token not found");
            }

            var spotify = new SpotifyClient(accessToken);

            var user = await spotify.UserProfile.Current(ct);

            var userPlaylists = await dataContext.Playlists
                .Include(p => p.Users)
                .Include(p => p.Tracks)
                .Include(p => p.Image)
                .Where(p => p.Users!.Any(u => u.UserId == user.Id))
                .Where(p => !request.OnlyOwnPlaylists || p.OwnerName == user.DisplayName)
                .ToListAsync(ct);

            var rand = new Random();
            var randomPlaylist = userPlaylists[rand.Next(userPlaylists.Count)];

            var randomPlaylistResponse = new SearchPlaylists.PlaylistResponse(
                randomPlaylist.PlaylistId,
                randomPlaylist.Name,
                randomPlaylist.Description,
                randomPlaylist.OwnerName,
                new SearchPlaylists.ImageResponse(randomPlaylist.Image!.Url),
                randomPlaylist.Tracks!.Select(t => new SearchPlaylists.TrackResponse(t.Name, t.ArtistName, false)).ToList()
            );

            return new Response(randomPlaylistResponse);
        }
    }
}
