using Microsoft.EntityFrameworkCore;

namespace SpotifyPlaylistSearchTool.Api.Database;

public class DataContext(DbContextOptions<DataContext> options) : DbContext(options)
{
    public DbSet<User> Users { get; set; }
    public DbSet<Playlist> Playlists { get; set; }
    public DbSet<Track> Tracks { get; set; }
    public DbSet<Image> Images { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(builder =>
        {
            builder.OwnsOne(
                u => u.SyncState,
                sync =>
                {
                    sync.Property(s => s.Status)
                        .HasColumnName("SyncStatus")
                        .HasColumnType("sync_status");

                    sync.Property(s => s.TotalPlaylists).HasColumnName("SyncTotalPlaylists");

                    sync.Property(s => s.ErrorMessage)
                        .HasColumnName("SyncErrorMessage")
                        .HasMaxLength(500);

                    sync.Property(s => s.CompletedAt).HasColumnName("SyncCompletedAt");
                }
            );
        });
    }
}
