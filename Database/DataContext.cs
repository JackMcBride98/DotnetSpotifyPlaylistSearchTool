using Microsoft.EntityFrameworkCore;

namespace DotnetSpotifyPlaylistSearchTool.Database;

public class DataContext(DbContextOptions<DataContext> options) : DbContext(options)
{
    public DbSet<User> Users { get; set; }
}
