using Bogus;
using Builders;
using SpotifyPlaylistSearchTool.Api.Database;

namespace Builders;

public class PlaylistBuilder : Builder<Playlist>
{
    private static readonly Faker Faker = new();

    public string PlaylistId { get; set; } = Faker.Random.Guid().ToString();
    public string Name { get; set; } = Faker.Music.Genre() + " Hits";
    public string Description { get; set; } = Faker.Lorem.Sentence();
    public string OwnerName { get; set; } = Faker.Name.FullName();
    public string SnapshotId { get; set; } = Faker.Random.AlphaNumeric(10);
    public Image? Image { get; set; } = null;
    public List<User> Users { get; set; } = [];
    public List<Track> Tracks { get; set; } = [];

    public override Playlist Build()
    {
        return new Playlist(PlaylistId, Name, Description, OwnerName, SnapshotId)
        {
            Image = Image,
            Users = Users,
            Tracks = Tracks,
        };
    }
}
