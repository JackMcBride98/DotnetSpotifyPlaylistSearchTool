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
    public Instant? UpdatedAt { get; set; } = null;
    public int? FirstSyncTotalPlaylists { get; set; } = null;
    public List<Playlist> Playlists { get; set; } = [];

    public UserBuilder WithPlaylists(int count)
    {
        for (int i = 0; i < count; i++)
        {
            var playlist = new PlaylistBuilder().Build();

            Playlists.Add(playlist);
        }
        return this;
    }

    public override User Build()
    {
        var user = new User(UserId, Username, AccessToken, RefreshToken)
        {
            UpdatedAt = UpdatedAt,
            Playlists = Playlists,
            FirstSyncTotalPlaylists = FirstSyncTotalPlaylists,
        };

        Playlists.ForEach(p => p.Users!.Add(user));

        return user;
    }
}
