using Builders;
using Microsoft.AspNetCore.Http;
using NSubstitute;
using SpotifyAPI.Web;
using SpotifyPlaylistSearchTool.Api.Features.Playlists;
using Image = SpotifyPlaylistSearchTool.Api.Database.Image;

namespace Tests.Features;

public class SearchPlaylistsTests(App app) : TestBase(app)
{
    private const string TestUserId = "spotify-user-123";
    private const string TestUserName = "Alex Smith";

    private void ConfigureMockUser(string id, string displayName)
    {
        App.MockSpotifyAuth.GetCurrentUserProfileAsync(
                Arg.Any<HttpContext>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(Task.FromResult(new PrivateUser { Id = id, DisplayName = displayName }));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(5)]
    public async Task Search_Returns_Correct_TotalUserPlaylists_Count(int playlistCount)
    {
        // Arrange
        ConfigureMockUser(TestUserId, TestUserName);

        var user = new UserBuilder { UserId = TestUserId, Username = TestUserName }
            .WithPlaylists(playlistCount)
            .Build();
        Db.Users.Add(user);
        await Db.SaveChangesAsync(TestContext.Current.CancellationToken);

        Db.ChangeTracker.Clear();

        var request = new SearchPlaylists.Request("Track", ShowOnlyOwnPlaylists: false);

        // Act
        var (response, result) = await App.Client.GETAsync<
            SearchPlaylists.Endpoint,
            SearchPlaylists.Request,
            SearchPlaylists.Response
        >(request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        result.TotalPlaylists.ShouldBe(playlistCount);
    }
    
    [Fact]
    public async Task Search_With_ShowOnlyOwnPlaylists_True_Returns_Filtered_TotalUserPlaylists_Count()
    {
        // Arrange
        ConfigureMockUser(TestUserId, TestUserName);

        var user = new UserBuilder { UserId = TestUserId, Username = TestUserName }
            .WithPlaylists([
                new PlaylistBuilder
                {
                    PlaylistId = "own-playlist-1",
                    OwnerName = TestUserName,
                    Image = new Image("https://example.com/img1.jpg", 0, 0),
                }.WithTracks([new TrackBuilder { Name = "Track 1", ArtistName = "Artist 1" }]),
                new PlaylistBuilder
                {
                    PlaylistId = "own-playlist-2",
                    OwnerName = TestUserName,
                    Image = new Image("https://example.com/img2.jpg", 0, 0),
                }.WithTracks([new TrackBuilder { Name = "Track 2", ArtistName = "Artist 2" }]),
                new PlaylistBuilder
                {
                    PlaylistId = "followed-playlist",
                    OwnerName = "Someone Else",
                    Image = new Image("https://example.com/img3.jpg", 0, 0),
                }.WithTracks([new TrackBuilder { Name = "Track 3", ArtistName = "Artist 3" }]),
            ])
            .Build();
        Db.Users.Add(user);
        await Db.SaveChangesAsync(TestContext.Current.CancellationToken);

        Db.ChangeTracker.Clear();

        var request = new SearchPlaylists.Request(SearchTerm: "Track", ShowOnlyOwnPlaylists: true);

        // Act
        var (response, result) = await App.Client.GETAsync<
            SearchPlaylists.Endpoint,
            SearchPlaylists.Request,
            SearchPlaylists.Response
        >(request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        result.TotalPlaylists.ShouldBe(2);
    }

    [Fact]
    public async Task Search_With_ShowOnlyOwnPlaylists_False_Returns_Unfiltered_TotalUserPlaylists_Count()
    {
        // Arrange
        ConfigureMockUser(TestUserId, TestUserName);

        var user = new UserBuilder { UserId = TestUserId, Username = TestUserName }
            .WithPlaylists([
                new PlaylistBuilder
                {
                    PlaylistId = "own-playlist-1",
                    OwnerName = TestUserName,
                    Image = new Image("https://example.com/img1.jpg", 0, 0),
                }.WithTracks([new TrackBuilder { Name = "Track 1", ArtistName = "Artist 1" }]),
                new PlaylistBuilder
                {
                    PlaylistId = "followed-playlist",
                    OwnerName = "Someone Else",
                    Image = new Image("https://example.com/img2.jpg", 0, 0),
                }.WithTracks([new TrackBuilder { Name = "Track 2", ArtistName = "Artist 2" }]),
            ])
            .Build();
        Db.Users.Add(user);
        await Db.SaveChangesAsync(TestContext.Current.CancellationToken);

        Db.ChangeTracker.Clear();

        var request = new SearchPlaylists.Request(SearchTerm: "Track", ShowOnlyOwnPlaylists: false);

        // Act
        var (response, result) = await App.Client.GETAsync<
            SearchPlaylists.Endpoint,
            SearchPlaylists.Request,
            SearchPlaylists.Response
        >(request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        result.TotalPlaylists.ShouldBe(2);
    }

    [Fact]
    public async Task Search_With_Matching_Track_Name_Returns_Expected_Playlist()
    {
        // Arrange
        ConfigureMockUser(TestUserId, TestUserName);

        var user = new UserBuilder { UserId = TestUserId, Username = TestUserName }
            .WithPlaylists([
                new PlaylistBuilder
                {
                    PlaylistId = "playlist-track-name",
                    Image = new Image("https://example.com/img1.jpg", 0, 0),
                }.WithTracks([
                    new TrackBuilder { Name = "November Rain", ArtistName = "Guns N' Roses" },
                ]),
            ])
            .Build();
        Db.Users.Add(user);
        await Db.SaveChangesAsync(TestContext.Current.CancellationToken);

        Db.ChangeTracker.Clear();

        var request = new SearchPlaylists.Request("november", ShowOnlyOwnPlaylists: false);

        // Act
        var (response, result) = await App.Client.GETAsync<
            SearchPlaylists.Endpoint,
            SearchPlaylists.Request,
            SearchPlaylists.Response
        >(request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        result.MatchingPlaylists.Count.ShouldBe(1);
        result.MatchingPlaylists.First().Tracks.First().Name.ShouldBe("November Rain");
        result.MatchingPlaylists.First().Tracks.First().Match.ShouldBeTrue();
    }

    [Fact]
    public async Task Search_With_Matching_Artist_Name_Returns_Expected_Playlist()
    {
        // Arrange
        ConfigureMockUser(TestUserId, TestUserName);

        var user = new UserBuilder { UserId = TestUserId, Username = TestUserName }
            .WithPlaylists([
                new PlaylistBuilder
                {
                    PlaylistId = "playlist-artist-name",
                    Image = new Image("https://example.com/img2.jpg", 0, 0),
                }.WithTracks([
                    new TrackBuilder { Name = "Shake It Off", ArtistName = "Taylor Swift" },
                ]),
            ])
            .Build();
        Db.Users.Add(user);
        await Db.SaveChangesAsync(TestContext.Current.CancellationToken);

        Db.ChangeTracker.Clear();

        var request = new SearchPlaylists.Request("swift", ShowOnlyOwnPlaylists: false);

        // Act
        var (response, result) = await App.Client.GETAsync<
            SearchPlaylists.Endpoint,
            SearchPlaylists.Request,
            SearchPlaylists.Response
        >(request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        result.MatchingPlaylists.Count.ShouldBe(1);
        result.MatchingPlaylists.First().Tracks.First().ArtistName.ShouldBe("Taylor Swift");
        result.MatchingPlaylists.First().Tracks.First().Match.ShouldBeTrue();
    }

    [Fact]
    public async Task Search_Only_Returns_Playlists_That_Contain_At_Least_One_Matching_Track()
    {
        // Arrange
        ConfigureMockUser(TestUserId, TestUserName);

        var user = new UserBuilder { UserId = TestUserId, Username = TestUserName }
            .WithPlaylists([
                new PlaylistBuilder
                {
                    PlaylistId = "matching-playlist",
                    Image = new Image("https://example.com/img1.jpg", 0, 0),
                }.WithTracks([
                    new TrackBuilder { Name = "Fortnight", ArtistName = "Taylor Swift" },
                ]),
                new PlaylistBuilder
                {
                    PlaylistId = "non-matching-playlist",
                    Image = new Image("https://example.com/img2.jpg", 0, 0),
                }.WithTracks([
                    new TrackBuilder { Name = "Take Five", ArtistName = "Dave Brubeck" },
                ]),
            ])
            .Build();
        Db.Users.Add(user);
        await Db.SaveChangesAsync(TestContext.Current.CancellationToken);

        Db.ChangeTracker.Clear();

        var request = new SearchPlaylists.Request("Fortnight", ShowOnlyOwnPlaylists: false);

        // Act
        var (response, result) = await App.Client.GETAsync<
            SearchPlaylists.Endpoint,
            SearchPlaylists.Request,
            SearchPlaylists.Response
        >(request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        result.MatchingPlaylists.Count.ShouldBe(1);
        result.MatchingPlaylists.ShouldNotContain(p => p.Id == "non-matching-playlist");
        result.MatchingPlaylists.ShouldContain(p => p.Id == "matching-playlist");
    }

    [Fact]
    public async Task Search_With_ShowOnlyOwnPlaylists_True_Returns_Only_Playlists_Owned_By_User()
    {
        // Arrange
        ConfigureMockUser(TestUserId, TestUserName);

        var user = new UserBuilder { UserId = TestUserId, Username = TestUserName }
            .WithPlaylists([
                new PlaylistBuilder
                {
                    PlaylistId = "own-playlist",
                    OwnerName = TestUserName, // Matches logged-in user DisplayName
                    Image = new Image("https://example.com/img1.jpg", 0, 0),
                }.WithTracks([new TrackBuilder { Name = "Style", ArtistName = "Taylor Swift" }]),
                new PlaylistBuilder
                {
                    PlaylistId = "someone-elses-playlist",
                    OwnerName = "John Doe", // Different owner
                    Image = new Image("https://example.com/img2.jpg", 0, 0),
                }.WithTracks([
                    new TrackBuilder { Name = "Blank Space", ArtistName = "Taylor Swift" },
                ]),
            ])
            .Build();
        Db.Users.Add(user);
        await Db.SaveChangesAsync(TestContext.Current.CancellationToken);

        Db.ChangeTracker.Clear();

        var request = new SearchPlaylists.Request("Swift", ShowOnlyOwnPlaylists: true);

        // Act
        var (response, result) = await App.Client.GETAsync<
            SearchPlaylists.Endpoint,
            SearchPlaylists.Request,
            SearchPlaylists.Response
        >(request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        result.MatchingPlaylists.Count.ShouldBe(1);
        result.MatchingPlaylists.ShouldContain(p => p.Id == "own-playlist");
        result.MatchingPlaylists.ShouldNotContain(p => p.Id == "someone-elses-playlist");
    }

    [Fact]
    public async Task Search_With_ShowOnlyOwnPlaylists_False_Returns_All_User_Playlists()
    {
        // Arrange
        ConfigureMockUser(TestUserId, TestUserName);

        var user = new UserBuilder { UserId = TestUserId, Username = TestUserName }
            .WithPlaylists([
                new PlaylistBuilder
                {
                    PlaylistId = "own-playlist",
                    OwnerName = TestUserName,
                    Image = new Image("https://example.com/img1.jpg", 0, 0),
                }.WithTracks([new TrackBuilder { Name = "Style", ArtistName = "Taylor Swift" }]),
                new PlaylistBuilder
                {
                    PlaylistId = "someone-elses-playlist",
                    OwnerName = "John Doe",
                    Image = new Image("https://example.com/img2.jpg", 0, 0),
                }.WithTracks([
                    new TrackBuilder { Name = "Blank Space", ArtistName = "Taylor Swift" },
                ]),
            ])
            .Build();
        Db.Users.Add(user);
        await Db.SaveChangesAsync(TestContext.Current.CancellationToken);

        Db.ChangeTracker.Clear();

        var request = new SearchPlaylists.Request("Swift", ShowOnlyOwnPlaylists: false);

        // Act
        var (response, result) = await App.Client.GETAsync<
            SearchPlaylists.Endpoint,
            SearchPlaylists.Request,
            SearchPlaylists.Response
        >(request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        result.MatchingPlaylists.Count.ShouldBe(2);
        result.MatchingPlaylists.ShouldContain(p => p.Id == "own-playlist");
        result.MatchingPlaylists.ShouldContain(p => p.Id == "someone-elses-playlist");
    }

    [Fact]
    public async Task Search_Flags_Match_True_Only_For_Tracks_Containing_SearchTerm_Within_Matched_Playlist()
    {
        // Arrange
        ConfigureMockUser(TestUserId, TestUserName);

        var user = new UserBuilder { UserId = TestUserId, Username = TestUserName }
            .WithPlaylists([
                new PlaylistBuilder
                {
                    PlaylistId = "mixed-playlist",
                    Image = new Image("https://example.com/img1.jpg", 0, 0),
                }.WithTracks([
                    new TrackBuilder { Name = "Bohemian Rhapsody", ArtistName = "Queen" },
                    new TrackBuilder { Name = "Stairway to Heaven", ArtistName = "Led Zeppelin" },
                    new TrackBuilder { Name = "Killer Queen", ArtistName = "Various Artists" },
                ]),
            ])
            .Build();
        Db.Users.Add(user);
        await Db.SaveChangesAsync(TestContext.Current.CancellationToken);

        Db.ChangeTracker.Clear();

        // Searching for "Queen" should match via Artist Name (track 1) or Track Name (track 3)
        var request = new SearchPlaylists.Request("Queen", ShowOnlyOwnPlaylists: false);

        // Act
        var (response, result) = await App.Client.GETAsync<
            SearchPlaylists.Endpoint,
            SearchPlaylists.Request,
            SearchPlaylists.Response
        >(request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        result.MatchingPlaylists.Count.ShouldBe(1);

        var matchedPlaylist = result.MatchingPlaylists.First();

        var artistMatchTrack = matchedPlaylist
            .Tracks.Where(t => t.ArtistName == "Queen")
            .ShouldHaveSingleItem();
        artistMatchTrack.Match.ShouldBeTrue();

        var nonMatchTrack = matchedPlaylist
            .Tracks.Where(t => t.Name == "Stairway to Heaven")
            .ShouldHaveSingleItem();
        nonMatchTrack.Match.ShouldBeFalse();

        var nameMatchTrack = matchedPlaylist
            .Tracks.Where(t => t.Name == "Killer Queen")
            .ShouldHaveSingleItem();
        nameMatchTrack.Match.ShouldBeTrue();
    }
}
