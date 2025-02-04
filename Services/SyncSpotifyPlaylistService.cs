using DotnetSpotifyPlaylistSearchTool.Database;

namespace DotnetSpotifyPlaylistSearchTool.Services;

public interface ISyncSpotifyPlaylistService
{
    Task<ICollection<Playlist>> SyncSpotifyPlaylistAsync(string userId);
}

public class SyncSpotifyPlaylistService
{
    
}
