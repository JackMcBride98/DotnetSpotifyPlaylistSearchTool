using DotnetSpotifyPlaylistSearchTool.Database;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using SpotifyAPI.Web;
using Image = DotnetSpotifyPlaylistSearchTool.Database.Image;

namespace DotnetSpotifyPlaylistSearchTool.Services;

public interface ISyncSpotifyPlaylistService
{
    Task SyncSpotifyPlaylistAsync(string userId, bool requiresProgressUpdates);
}

public class SyncSpotifyPlaylistService(DataContext dataContext) : ISyncSpotifyPlaylistService
{
    public async Task SyncSpotifyPlaylistAsync(string userId, bool requiresProgressUpdates)
    {
        var user = await dataContext.Users
            .Include(u => u.Playlists)
            .SingleOrDefaultAsync(u => u.UserId == userId);

        if (user == null)
        {
            throw new InvalidOperationException("User not found");
        }

        var spotify = new SpotifyClient(user.AccessToken);

        var playlists = (await spotify.PaginateAll(await spotify.Playlists.GetUsers(user.UserId))).DistinctBy(p => p.Id).ToList();

        if (requiresProgressUpdates)
        {
            user.FirstSyncTotalPlaylists = playlists.Count;
            await dataContext.SaveChangesAsync();
        }

        var newPlaylists = new List<Playlist>();

        foreach (var playlist in playlists)
        {
            if (playlist.Id == null)
            {
                continue;
            }

            var existingPlaylist = await dataContext.Playlists.Include(p => p.Users).Include(p => p.Tracks).SingleOrDefaultAsync(p => p.PlaylistId == playlist.Id);
            if (existingPlaylist != null && existingPlaylist.SnapshotId == playlist.SnapshotId)
            {
                if (!existingPlaylist.Users!.Any(u => u.UserId == userId))
                {
                    existingPlaylist.Users!.Add(user);
                }
                continue;
            }

            var tracks = await spotify.PaginateAll(await spotify.Playlists.GetItems(playlist.Id));

            var trackEntities = tracks.Select((t, i) => ToTrack(t, playlist.Id, i)).Where(t => t != null).Select(t => t!).ToList();

            var firstImageOrNull = playlist.Images?.FirstOrDefault();

            var playlistImage = firstImageOrNull == null ? null : new Image(firstImageOrNull.Url, firstImageOrNull.Width, firstImageOrNull.Height);

            var existingPlaylistUsers = new List<User>{ user };
            if (existingPlaylist is not null)
            {
                existingPlaylistUsers = existingPlaylist.Users!.ToList();
                dataContext.Playlists.Remove(existingPlaylist);
            }

            var newPlaylist = new Playlist(
                playlist.Id,
                playlist.Name ?? "",
                playlist.Description ?? "",
                playlist.Owner?.DisplayName ?? "",
                playlist.SnapshotId ?? ""
            )
            {
                Tracks = trackEntities,
                Users = existingPlaylistUsers,
                Image = playlistImage,
            };
            
            newPlaylists.Add(newPlaylist);
            dataContext.Playlists.Add(newPlaylist);
            
            if (requiresProgressUpdates)
            {
                await dataContext.SaveChangesAsync();
            }
        }
        
        user.UpdatedAt = SystemClock.Instance.GetCurrentInstant();
        await dataContext.SaveChangesAsync();
    }

    private static Track? ToTrack(PlaylistTrack<IPlayableItem> playlistTrack, string playlistId, int index)
    {
        if (playlistTrack.Track is FullTrack fullTrack)
        {
            return new Track(
                index,
                fullTrack.Name,
                string.Join(", ", fullTrack.Artists.Select(a => a.Name)),
                playlistId
            );
        }

        if (playlistTrack.Track is FullEpisode fullEpisode)
        {
            return new Track(
                index,
                $"{fullEpisode.Show.Name} - {fullEpisode.Name}",
                fullEpisode.Show.Publisher,
                playlistId
            );
        }

        return null;
    }
}
