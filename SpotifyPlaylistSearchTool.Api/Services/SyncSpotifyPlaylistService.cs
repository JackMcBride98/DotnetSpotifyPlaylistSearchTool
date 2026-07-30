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
    public async Task SyncActiveUsersAsync(CancellationToken ct)
    {
        var aWeekAgo = SystemClock.Instance.GetCurrentInstant() - Duration.FromDays(7);
        var activeUserIds = await dataContext
            .Users.Where(u => u.LastActiveAt.HasValue && u.LastActiveAt.Value >= aWeekAgo)
            .Select(u => u.UserId)
            .ToListAsync(ct);

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
        Console.WriteLine($"Running sync job for UserId: {userId}");
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

            if (requiresProgressUpdates)
            {
                user.SyncState.TotalPlaylists = playlists.Count;
                await dataContext.SaveChangesAsync(ct);
            }

            foreach (var (index, playlist) in playlists.Index())
            {
                await SyncPlaylistAsync(spotifyClient, playlist, user, ct);

                var shouldSaveUsingBatchingStrategy = index % 5 == 0;
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
            await dataContext.SaveChangesAsync(ct);
        }
        catch (Exception e)
        {
            if (requiresProgressUpdates)
            {
                user.SyncState.Status = SyncStatus.Failed;
                user.SyncState.ErrorMessage = $"There was an error syncing playlists - {e.Message}";
            }
            await dataContext.SaveChangesAsync(ct);
            Console.WriteLine(e.Message);
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
        CancellationToken ct
    )
    {
        if (playlist.Id == null)
        {
            return;
        }

        var existingPlaylist = await dataContext
            .Playlists.Include(p => p.Users)
            .Include(p => p.Tracks)
            .SingleOrDefaultAsync(p => p.PlaylistId == playlist.Id, ct);

        if (existingPlaylist != null && existingPlaylist.SnapshotId == playlist.SnapshotId)
        {
            if (existingPlaylist.Users!.All(u => u.UserId != user.UserId))
            {
                existingPlaylist.Users!.Add(user);
            }

            return;
        }

        var tracks = await GetAllPlaylistTracksAsync(spotifyClient, playlist.Id, ct);

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

        var existingPlaylistUsers = new List<User> { user };
        if (existingPlaylist is not null)
        {
            existingPlaylistUsers = existingPlaylist.Users!.ToList();
            if (existingPlaylistUsers.All(u => u.UserId != user.UserId))
            {
                existingPlaylistUsers.Add(user);
            }
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

        dataContext.Playlists.Add(newPlaylist);
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
