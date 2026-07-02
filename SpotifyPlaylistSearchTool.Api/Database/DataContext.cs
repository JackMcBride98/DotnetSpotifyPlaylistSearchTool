using Microsoft.EntityFrameworkCore;

namespace DotnetSpotifyPlaylistSearchTool.Database;

public class DataContext(DbContextOptions<DataContext> options) : DbContext(options)
{
    public DbSet<User> Users { get; set; }
    public DbSet<Playlist> Playlists { get; set; }
    public DbSet<Track> Tracks { get; set; }
}
