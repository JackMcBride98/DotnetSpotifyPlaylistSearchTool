using Microsoft.AspNetCore.Http;
using NSubstitute;
using SpotifyAPI.Web;
using SpotifyPlaylistSearchTool.Api.Database;
using SpotifyPlaylistSearchTool.Api.Features;
using Image = SpotifyPlaylistSearchTool.Api.Database.Image;

namespace Tests.Features;

public class SearchPlaylistsTests(App app) : TestBase(app)
{
    private const string TestUserId = "spotify-user-123";
    private const string TestUserName = "Alex Smith";

    private async Task SeedDatabaseAsync(List<Playlist> playlists)
    {
        using var scope = App.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DataContext>();
        
        // Ensure database is clean between test runs
        await db.Database.EnsureDeletedAsync();
        await db.Database.EnsureCreatedAsync();

        db.Playlists.AddRange(playlists);
        await db.SaveChangesAsync();
    }
    
    private void ConfigureMockUser(string id, string displayName)
    {
        App.MockSpotifyAuth
           .GetCurrentUserProfileAsync(Arg.Any<HttpContext>(), Arg.Any<CancellationToken>())
           .Returns(Task.FromResult(new PrivateUser 
           { 
               Id = id, 
               DisplayName = displayName 
           }));
    }
    
    [Fact]
    public async Task Search_Returns_Matching_Playlists_And_Flags_Matching_Tracks()
    {
        // Arrange
        ConfigureMockUser(TestUserId, TestUserName);

        var user = new User(TestUserId, TestUserName, "access-token", "refresh-token");
        var samplePlaylists = new List<Playlist>
        {
            new Playlist("playlist-1", "Rock Classics", "Greatest rock tracks", TestUserName, "snapshot-1")
            {
                Users = [user],
                Image = new Image("https://example.com/img1.jpg", 200, 200),
                Tracks = [
                    new Track(1, "Bohemian Rhapsody", "Queen", "playlist-1"),
                    new Track(2, "Stairway to Heaven", "Led Zeppelin", "playlist-1")
                ]
            },
            new Playlist("playlist-2", "Pop Hits", "Top pop music", "Someone Else", "snapshot-2")
            {
                Users = [user],
                Image = new Image("https://example.com/img2.jpg", 0, 0),
                Tracks = [
                    new Track(1, "Blank Space", "Taylor Swift", "playlist-2")
                ]
            }
        };

        await SeedDatabaseAsync(samplePlaylists);
        
        var request = new SearchPlaylists.Request("queen", ShowOnlyOwnPlaylists: false);

        // Act
        var (response, result) = await App.Client
            .GETAsync<SearchPlaylists.Endpoint, SearchPlaylists.Request, SearchPlaylists.Response>(request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        result.TotalPlaylists.ShouldBe(2); // The total count before filtering tracks
        result.MatchingPlaylists.Count.ShouldBe(1);
        
        var matchedPlaylist = result.MatchingPlaylists.First();
        matchedPlaylist.Id.ShouldBe("playlist-1");
        matchedPlaylist.Image.Url.ShouldBe("https://example.com/img1.jpg");
        
        // Check that individual tracks flag their match state correctly
        var queenTrack = matchedPlaylist.Tracks.First(t => t.ArtistName == "Queen");
        queenTrack.Match.ShouldBeTrue();

        var zeppelinTrack = matchedPlaylist.Tracks.First(t => t.ArtistName == "Led Zeppelin");
        zeppelinTrack.Match.ShouldBeFalse();
    }
}