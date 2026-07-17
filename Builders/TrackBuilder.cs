using Bogus;
using SpotifyPlaylistSearchTool.Api.Database;

namespace Builders;

public class TrackBuilder : Builder<Track>
{
    private static readonly Faker Faker = new();

    public int TrackId { get; set; } = 0; // Default EF core identity placeholder
    public int Index { get; set; } = Faker.Random.Number(1, 20);
    public string Name { get; set; } =
        string.Join(" ", Faker.Lorem.Words(Faker.Random.Number(1, 4)));
    public string ArtistName { get; set; } = Faker.Name.FullName();
    public string PlaylistId { get; set; } = Faker.Random.Guid().ToString();

    public override Track Build()
    {
        return new Track(Index, Name, ArtistName, PlaylistId) { TrackId = TrackId };
    }
}
