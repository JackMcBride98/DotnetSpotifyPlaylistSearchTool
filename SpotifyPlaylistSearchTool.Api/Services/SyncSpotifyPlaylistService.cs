using Microsoft.EntityFrameworkCore;
using NodaTime.Extensions;
using SpotifyAPI.Web;
using SpotifyPlaylistSearchTool.Api.Database;
using Image = SpotifyPlaylistSearchTool.Api.Database.Image;

namespace SpotifyPlaylistSearchTool.Api.Services;

public interface ISyncSpotifyPlaylistService
{
    Task SyncSpotifyPlaylistAsync(string UserId, bool requiresProgressUpdates);
}

public class SyncSpotifyPlaylistService(
    DataContext dataContext,
    ISpotifyAuthService spotifyAuthService
) : ISyncSpotifyPlaylistService
{
    public async Task SyncSpotifyPlaylistAsync(string UserId, bool requiresProgressUpdates)
    {
        var user = await dataContext
            .Users.Include(u => u.Playlists)
            .SingleOrDefaultAsync(u => u.UserId == UserId);

        if (user == null)
        {
            throw new InvalidOperationException("User not found");
        }

        if (requiresProgressUpdates)
        {
            user.SyncState.Status = SyncStatus.InProgress;
            await dataContext.SaveChangesAsync();
        }

        try
        {
            var spotifyClient = await spotifyAuthService.GetSpotifyClientAsync(
                user.RefreshToken,
                requiresProgressUpdates ? user.AccessToken : null,
                CancellationToken.None
            );

            var playlists = (
                await spotifyClient.PaginateAll(
                    await spotifyClient.Playlists.CurrentUsers(
                        new PlaylistCurrentUsersRequest { Limit = 50 }
                    )
                )
            )
                .DistinctBy(p => p.Id)
                .Where(p => p.Collaborative == true || p.Owner?.Id == UserId)
                .ToList();

            if (requiresProgressUpdates)
            {
                user.SyncState.TotalPlaylists = playlists.Count;
                await dataContext.SaveChangesAsync();
            }

            var newPlaylists = new List<Playlist>();

            foreach (var (index, playlist) in playlists.Index())
            {
                if (playlist.Id == null)
                {
                    continue;
                }

                var existingPlaylist = await dataContext
                    .Playlists.Include(p => p.Users)
                    .Include(p => p.Tracks)
                    .SingleOrDefaultAsync(p => p.PlaylistId == playlist.Id);
                if (existingPlaylist != null && existingPlaylist.SnapshotId == playlist.SnapshotId)
                {
                    if (!existingPlaylist.Users!.Any(u => u.UserId == UserId))
                    {
                        existingPlaylist.Users!.Add(user);
                    }

                    continue;
                }

                var tracks = await spotifyClient.PaginateAll(
                    await spotifyClient.Playlists.GetPlaylistItems(
                        playlist.Id,
                        new PlaylistGetItemsRequest { Limit = 50 }
                    )
                );

                var trackEntities = tracks
                    .Select((t, i) => ToTrack(t, playlist.Id, i))
                    .Where(t => t != null)
                    .Select(t => t!)
                    .ToList();

                var firstImageOrNull = playlist.Images?.FirstOrDefault();

                var playlistImage =
                    firstImageOrNull == null
                        ? null
                        : new Image(
                            firstImageOrNull.Url,
                            firstImageOrNull.Width,
                            firstImageOrNull.Height
                        );

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

                var shouldSaveUsingBatchingStrategy = index % 5 == 0;

                if (requiresProgressUpdates && shouldSaveUsingBatchingStrategy)
                {
                    await dataContext.SaveChangesAsync();
                }
            }

            if (requiresProgressUpdates)
            {
                user.SyncState.Status = SyncStatus.Completed;
            }
            user.SyncState.CompletedAt = DateTime.UtcNow.ToInstant();
            await dataContext.SaveChangesAsync();
        }
        catch (Exception e)
        {
            if (requiresProgressUpdates)
            {
                user.SyncState.Status = SyncStatus.Failed;
                user.SyncState.ErrorMessage = $"There was an error syncing playlists - {e.Message}";
                await dataContext.SaveChangesAsync();
            }
            Console.WriteLine(e.Message);
            throw e;
        }
    }

    private static Track? ToTrack(
        PlaylistTrack<IPlayableItem> playlistTrack,
        string playlistId,
        int index
    )
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
                "",
                playlistId
            );
        }

        return null;
    }
}
