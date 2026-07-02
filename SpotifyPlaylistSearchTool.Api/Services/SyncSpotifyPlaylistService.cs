using DotnetSpotifyPlaylistSearchTool.Database;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using SpotifyAPI.Web;
using Image = DotnetSpotifyPlaylistSearchTool.Database.Image;

namespace DotnetSpotifyPlaylistSearchTool.Services;

public interface ISyncSpotifyPlaylistService
{
    Task SyncSpotifyPlaylistAsync(SpotifyClient spotifyClient, bool requiresProgressUpdates);
}

public class SyncSpotifyPlaylistService(DataContext dataContext) : ISyncSpotifyPlaylistService
{
    public async Task SyncSpotifyPlaylistAsync(SpotifyClient spotifyClient, bool requiresProgressUpdates)
    {
        var profile = await spotifyClient.UserProfile.Current();
        var userId = profile.Id;
        var user = await dataContext.Users
            .Include(u => u.Playlists)
            .SingleOrDefaultAsync(u => u.UserId == userId);

        if (user == null)
        {
            throw new InvalidOperationException("User not found");
        }

        var playlists = (await spotifyClient.PaginateAll(await spotifyClient.Playlists.CurrentUsers())).DistinctBy(p => p.Id).Where(p => p.Collaborative == true || p.Owner?.Id == userId).ToList();

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

            var tracks = await spotifyClient.PaginateAll(await spotifyClient.Playlists.GetPlaylistItems(playlist.Id));

            var trackEntities = tracks.Select((t, i) => ToTrack(t, playlist.Id, i)).Where(t => t != null).Select(t => t!).ToList();

            var firstImageOrNull = playlist.Images?.FirstOrDefault();

            var playlistImage = firstImageOrNull == null ? null : new Image(firstImageOrNull.Url, firstImageOrNull.Width, firstImageOrNull.Height);

            var existingPlaylistUsers = new List<User> { user };
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
        if (playlistTrack.Item.Type == ItemType.Track)
        {
            var fullTrack = (FullTrack)playlistTrack.Item;

            return new Track(
                index,
                fullTrack.Name,
                string.Join(", ", fullTrack.Artists.Select(a => a.Name)),
                playlistId
            );
        }

        if (playlistTrack.Item.Type == ItemType.Episode)
        {
            var fullEpisode = (FullEpisode)playlistTrack.Item;

            return new Track(
                index,
                $"{fullEpisode.Show.Name} - {fullEpisode.Name}",
                fullEpisode.Show.Publisher ?? "Show publisher deprecated now",
                playlistId
            );
        }

        return null;
    }
}
