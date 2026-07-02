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

            var userPlaylistCount = await dataContext.Playlists.Include(p => p.Users)
                .Where(p => p.Users!.Any(u => u.UserId == spotifyUserProfile.Id))
                .CountAsync(ct);

            if (userPlaylistCount == 0)
                ThrowError("User has no playlists");

            var skip = new Random().Next(userPlaylistCount);

            var randomPlaylist = await dataContext.Playlists
                .Include(p => p.Tracks)
                .Include(p => p.Image)
                .Where(p => p.Users!.Any(u => u.UserId == spotifyUserProfile.Id))
                .Skip(skip)
                .Take(1)
                .SingleOrDefaultAsync(ct);


            if (randomPlaylist == null)
            {
                ThrowError("Random playlist not found");
            }

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
