using Bogus;
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

    public PlaylistBuilder WithTracks(int count)
    {
        for (int i = 0; i < count; i++)
        {
            var track = new TrackBuilder { PlaylistId = PlaylistId, Index = i + 1 }.Build();
            Tracks.Add(track);
        }

        return this;
    }

    public PlaylistBuilder WithTracks(List<TrackBuilder> trackBuilders)
    {
        trackBuilders.ForEach(tb => tb.PlaylistId = PlaylistId);
        Tracks.AddRange(trackBuilders.Select(tb => tb.Build()));
        return this;
    }

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
