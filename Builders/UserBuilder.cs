using Bogus;
using NodaTime;
using SpotifyPlaylistSearchTool.Api.Database;

namespace Builders;

public class UserBuilder : Builder<User>
{
    private static readonly Faker Faker = new();

    public string UserId { get; set; } = Faker.Random.Guid().ToString();
    public string Username { get; set; } = Faker.Internet.UserName();
    public string AccessToken { get; set; } = Faker.Random.AlphaNumeric(32);
    public string RefreshToken { get; set; } = Faker.Random.AlphaNumeric(32);
    public Instant? LastActiveAt { get; set; } = null;
    public UserSyncState SyncState { get; set; } = new UserSyncState();
    public List<Playlist> Playlists { get; set; } = [];

    public UserBuilder WithSyncState(
        SyncStatus status,
        int? totalPlaylists = null,
        string? errorMessage = null,
        Instant? completedAt = null
    )
    {
        SyncState = new UserSyncState
        {
            Status = status,
            TotalPlaylists = totalPlaylists,
            ErrorMessage = errorMessage,
            CompletedAt = completedAt,
        };
        return this;
    }

    public UserBuilder WithPlaylists(int count)
    {
        for (int i = 0; i < count; i++)
        {
            var playlist = new PlaylistBuilder().Build();
            Playlists.Add(playlist);
        }
        return this;
    }

    public UserBuilder WithPlaylists(List<PlaylistBuilder> playlistBuilders)
    {
        Playlists.AddRange(playlistBuilders.Select(pb => pb.Build()));
        return this;
    }

    public UserBuilder WithPlaylists(List<Playlist> playlists)
    {
        Playlists.AddRange(playlists);
        return this;
    }

    public override User Build()
    {
        var user = new User(UserId, Username, AccessToken, RefreshToken)
        {
            LastActiveAt = LastActiveAt,
            Playlists = Playlists,
            SyncState = SyncState,
        };

        Playlists.ForEach(p => p.Users!.Add(user));

        return user;
    }
}
