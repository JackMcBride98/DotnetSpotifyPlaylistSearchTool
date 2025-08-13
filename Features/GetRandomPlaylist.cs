using DotnetSpotifyPlaylistSearchTool.Database;
using DotnetSpotifyPlaylistSearchTool.Services;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using SpotifyAPI.Web;

namespace DotnetSpotifyPlaylistSearchTool.Features;

public static class GetRandomPlaylist
{
    public record Request(bool OnlyOwnPlaylists);

    public record Response(SearchPlaylists.PlaylistResponse RandomPlaylist);

    public class Endpoint(DataContext dataContext, ISpotifyAuthService spotifyAuthService)
        : Endpoint<Request, Response>
    {
        public override void Configure()
        {
            Get("/random-playlist");
            AllowAnonymous();
        }

        public override async Task<Response> ExecuteAsync(Request request, CancellationToken ct)
        {
            var spotifyUserProfile = await spotifyAuthService.GetCurrentUserProfileAsync(
                HttpContext,
                ct
            );

            // TODO: adapt this to do the random select in the SQL rather than loading all playlists into memory
            var userPlaylists = await dataContext
                .Playlists.Include(p => p.Users)
                .Include(p => p.Tracks)
                .Include(p => p.Image)
                .Where(p => p.Users!.Any(u => u.UserId == spotifyUserProfile.Id))
                .Where(p =>
                    !request.OnlyOwnPlaylists || p.OwnerName == spotifyUserProfile.DisplayName
                )
                .ToListAsync(ct);

            var rand = new Random();

            var randomPlaylist = userPlaylists[rand.Next(userPlaylists.Count)];

            var randomPlaylistResponse = new SearchPlaylists.PlaylistResponse(
                randomPlaylist.PlaylistId,
                randomPlaylist.Name,
                randomPlaylist.Description,
                randomPlaylist.OwnerName,
                new SearchPlaylists.ImageResponse(randomPlaylist.Image!.Url),
                randomPlaylist
                    .Tracks!.Select(t => new SearchPlaylists.TrackResponse(
                        t.Name,
                        t.ArtistName,
                        false
                    ))
                    .ToList()
            );

            return new Response(randomPlaylistResponse);
        }
    }
}
