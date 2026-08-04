using Microsoft.EntityFrameworkCore;
using NodaTime;
using NodaTime.Extensions;
using SpotifyAPI.Web;
using SpotifyPlaylistSearchTool.Api.Database;
using Image = SpotifyPlaylistSearchTool.Api.Database.Image;

namespace SpotifyPlaylistSearchTool.Api.Services;

public interface ISyncSpotifyPlaylistService
{
    Task SyncPlaylistsForUserAsync(
        string userId,
        bool requiresProgressUpdates,
        CancellationToken ct
    );
    Task SyncActiveUsersAsync(CancellationToken ct);
}

public class SyncSpotifyPlaylistService(
    DataContext dataContext,
    ISpotifyAuthService spotifyAuthService
) : ISyncSpotifyPlaylistService
{
    private const int InitialSyncPlaylistBatchSize = 5;

    public async Task SyncActiveUsersAsync(CancellationToken ct)
    {
        var aWeekAgo = SystemClock.Instance.GetCurrentInstant() - Duration.FromDays(7);
        var activeUserIds = await dataContext
            .Users.Where(u => u.LastActiveAt.HasValue && u.LastActiveAt.Value >= aWeekAgo)
            .Select(u => u.UserId)
            .ToListAsync(ct);

        Console.WriteLine(
            $"Syncing playlists for {activeUserIds.Count} Users active in the last week."
        );

        foreach (var userId in activeUserIds)
        {
            await SyncPlaylistsForUserAsync(userId, false, ct);
        }
    }

    public virtual async Task SyncPlaylistsForUserAsync(
        string userId,
        bool requiresProgressUpdates,
        CancellationToken ct
    )
    {
        Console.WriteLine($"Syncing Playlists for UserId: {userId}");
        var user = await dataContext
            .Users.Include(u => u.Playlists)
            .SingleOrDefaultAsync(u => u.UserId == userId, cancellationToken: ct);

        if (user == null)
        {
            throw new InvalidOperationException("User not found");
        }

        if (requiresProgressUpdates)
        {
            user.SyncState.Status = SyncStatus.InProgress;
            await dataContext.SaveChangesAsync(ct);
        }

        try
        {
            var spotifyClient = await spotifyAuthService.GetSpotifyClientAsync(
                user.RefreshToken,
                requiresProgressUpdates ? user.AccessToken : null,
                ct
            );

            var allPlaylists = await GetAllCurrentUsersPlaylistsAsync(spotifyClient, ct);

            var playlists = allPlaylists
                .DistinctBy(p => p.Id)
                .Where(p => p.Collaborative == true || p.Owner?.Id == userId)
                .ToList();

            Console.WriteLine($"Returned {playlists.Count} unique playlists for User {userId}");

            if (requiresProgressUpdates)
            {
                user.SyncState.TotalPlaylists = playlists.Count;
                await dataContext.SaveChangesAsync(ct);
            }

            var playlistIds = playlists.Where(p => p.Id != null).Select(p => p.Id!).ToList();
            var existingPlaylists = await dataContext
                .Playlists.Include(p => p.Users)
                .Include(p => p.Tracks)
                .Include(p => p.Image)
                .Where(p => playlistIds.Contains(p.PlaylistId))
                .AsSplitQuery()
                .ToDictionaryAsync(p => p.PlaylistId, ct);

            foreach (var (index, playlist) in playlists.Index())
            {
                if (playlist.Id == null)
                {
                    continue;
                }

                existingPlaylists.TryGetValue(playlist.Id, out var existingPlaylist);

                await SyncPlaylistAsync(spotifyClient, playlist, user, existingPlaylist, ct);

                var shouldSaveUsingBatchingStrategy = index % InitialSyncPlaylistBatchSize == 0;
                if (requiresProgressUpdates && shouldSaveUsingBatchingStrategy)
                {
                    await dataContext.SaveChangesAsync(ct);
                }
            }

            if (requiresProgressUpdates)
            {
                user.SyncState.Status = SyncStatus.Completed;
            }
            user.SyncState.CompletedAt = DateTime.UtcNow.ToInstant();
            Console.WriteLine(
                $"Finished syncing playlists for User {user.UserId} (before dataContext saveChanges)"
            );
            await dataContext.SaveChangesAsync(ct);
            Console.WriteLine(
                $"Finished syncing playlists for User {user.UserId} (after dataContext saveChanges)"
            );
        }
        catch (Exception e)
        {
            var errorMessage = $"There was an error syncing playlists - {e.Message}";
            if (requiresProgressUpdates)
            {
                user.SyncState.Status = SyncStatus.Failed;
                user.SyncState.ErrorMessage = errorMessage;
            }
            await dataContext.SaveChangesAsync(ct);
            Console.WriteLine(errorMessage);
            throw;
        }
    }

    public virtual async Task<IList<FullPlaylist>> GetAllCurrentUsersPlaylistsAsync(
        ISpotifyClient spotifyClient,
        CancellationToken ct
    )
    {
        return await spotifyClient.PaginateAll(
            await spotifyClient.Playlists.CurrentUsers(
                new PlaylistCurrentUsersRequest { Limit = 50 },
                ct
            ),
            cancellationToken: ct
        );
    }

    public virtual async Task<IList<PlaylistTrack<IPlayableItem>>> GetAllPlaylistTracksAsync(
        ISpotifyClient spotifyClient,
        string playlistId,
        CancellationToken ct
    )
    {
        return await spotifyClient.PaginateAll(
            await spotifyClient.Playlists.GetPlaylistItems(
                playlistId,
                new PlaylistGetItemsRequest { Limit = 50 },
                ct
            ),
            cancellationToken: ct
        );
    }

    private async Task SyncPlaylistAsync(
        ISpotifyClient spotifyClient,
        FullPlaylist playlist,
        User user,
        Playlist? existingPlaylist,
        CancellationToken ct
    )
    {
        if (playlist.Id == null)
        {
            return;
        }

        if (existingPlaylist != null && existingPlaylist.SnapshotId == playlist.SnapshotId)
        {
            if (existingPlaylist.Users!.All(u => u.UserId != user.UserId))
            {
                existingPlaylist.Users!.Add(user);
            }
            Console.WriteLine($"Playlist {playlist.Name} is up to date for User {user.UserId}");
            return;
        }
        Console.WriteLine($"Syncing Playlist {playlist.Name} for User {user.UserId}");

        var tracks = await GetAllPlaylistTracksAsync(spotifyClient, playlist.Id, ct);

        Console.WriteLine(
            $"Fetched {tracks.Count} tracks for Playlist {playlist.Name} for User {user.UserId}"
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
                : new Image(firstImageOrNull.Url, firstImageOrNull.Width, firstImageOrNull.Height);

        if (existingPlaylist != null)
        {
            existingPlaylist.Name = playlist.Name ?? "";
            existingPlaylist.Description = playlist.Description ?? "";
            existingPlaylist.OwnerName = playlist.Owner?.DisplayName ?? "";
            existingPlaylist.SnapshotId = playlist.SnapshotId ?? "";

            existingPlaylist.Image = playlistImage;

            if (existingPlaylist.Users!.All(u => u.UserId != user.UserId))
            {
                existingPlaylist.Users!.Add(user);
            }

            dataContext.Tracks.RemoveRange(existingPlaylist.Tracks!);
            existingPlaylist.Tracks = trackEntities;
        }
        else
        {
            var newPlaylist = new Playlist(
                playlist.Id,
                playlist.Name ?? "",
                playlist.Description ?? "",
                playlist.Owner?.DisplayName ?? "",
                playlist.SnapshotId ?? ""
            )
            {
                Tracks = trackEntities,
                Users = [user],
                Image = playlistImage,
            };

            dataContext.Playlists.Add(newPlaylist);
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
