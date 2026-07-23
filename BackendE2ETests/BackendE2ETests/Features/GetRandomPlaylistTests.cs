using Builders;
using Microsoft.AspNetCore.Http;
using NSubstitute;
using SpotifyAPI.Web;
using GetRandomPlaylist = SpotifyPlaylistSearchTool.Api.Features.Playlists.GetRandomPlaylist;

namespace Tests.Features;

public class GetRandomPlaylistTests(App app) : TestBase(app)
{
    private const string DefaultSpotifyUserId = "spotify_user_123";
    private const string DefaultSpotifyDisplayName = "Test User";

    [Fact]
    public async Task GetRandomPlaylist_NoPlaylists_ThrowsError()
    {
        // Arrange
        ArrangeMockSpotifyUser(DefaultSpotifyUserId);

        var user = new UserBuilder { UserId = DefaultSpotifyUserId }.Build();
        Db.Users.Add(user);
        await Db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var request = new GetRandomPlaylist.Request(OnlyOwnPlaylists: false);

        // Act
        var (response, problemDetails) = await App.Client.GETAsync<
            GetRandomPlaylist.Endpoint,
            GetRandomPlaylist.Request,
            ProblemDetails
        >(request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        problemDetails.ShouldNotBeNull();

        var error = problemDetails.Errors.FirstOrDefault();
        error.ShouldNotBeNull();
        error.Reason.ShouldBe("User has no playlists or random playlist not found");
    }

    [Fact]
    public async Task GetRandomPlaylist_UserHasOnePlaylist_ReturnsThatPlaylist()
    {
        // Arrange
        ArrangeMockSpotifyUser(DefaultSpotifyUserId);

        var user = new UserBuilder { UserId = DefaultSpotifyUserId }.Build();
        Db.Users.Add(user);

        var playlist = new PlaylistBuilder
        {
            PlaylistId = "only-playlist-id",
            Name = "My Only Playlist",
            Description = "Chill tunes",
            OwnerName = "Test Owner",
            Users = [user],
        }
            .WithTracks([
                new TrackBuilder()
                {
                    Name = "Track A",
                    ArtistName = "Artist A",
                    PlaylistId = "only-playlist-id",
                    Index = 1,
                },
            ])
            .Build();
        Db.Playlists.Add(playlist);
        await Db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var request = new GetRandomPlaylist.Request(OnlyOwnPlaylists: false);

        // Act
        var (response, result) = await App.Client.GETAsync<
            GetRandomPlaylist.Endpoint,
            GetRandomPlaylist.Request,
            GetRandomPlaylist.Response
        >(request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        result.ShouldNotBeNull();

        var returnedPlaylist = result.RandomPlaylist;
        returnedPlaylist.Id.ShouldBe("only-playlist-id");
        returnedPlaylist.Name.ShouldBe("My Only Playlist");
        returnedPlaylist.Description.ShouldBe("Chill tunes");
        returnedPlaylist.OwnerName.ShouldBe("Test Owner");
        returnedPlaylist.Tracks.Count.ShouldBe(1);
        returnedPlaylist.Tracks.Any(t => t.Name == "Track A").ShouldBeTrue();
    }

    [Fact]
    public async Task GetRandomPlaylist_UserHasMultiplePlaylists_ReturnsOneOfThem()
    {
        // Arrange
        ArrangeMockSpotifyUser(DefaultSpotifyUserId);

        var user = new UserBuilder { UserId = DefaultSpotifyUserId }.Build();
        Db.Users.Add(user);

        var seededIds = new List<string> { "id-1", "id-2", "id-3" };
        var playlists = seededIds.Select(id =>
            new PlaylistBuilder { PlaylistId = id, Users = [user] }.Build()
        );

        Db.Playlists.AddRange(playlists);
        await Db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var request = new GetRandomPlaylist.Request(OnlyOwnPlaylists: false);

        // Act
        var (response, result) = await App.Client.GETAsync<
            GetRandomPlaylist.Endpoint,
            GetRandomPlaylist.Request,
            GetRandomPlaylist.Response
        >(request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        result.ShouldNotBeNull();

        seededIds.ShouldContain(result.RandomPlaylist.Id);
    }

    [Fact]
    public async Task GetRandomPlaylist_MultipleUsers_DoesNotReturnOtherUsersPlaylists()
    {
        // Arrange
        ArrangeMockSpotifyUser(DefaultSpotifyUserId);

        var currentUser = new UserBuilder { UserId = DefaultSpotifyUserId }.Build();
        var otherUser = new UserBuilder { UserId = "other_user_456" }.Build();
        Db.Users.AddRange(currentUser, otherUser);

        var currentUserPlaylist = new PlaylistBuilder
        {
            PlaylistId = "current-user-playlist",
            Users = [currentUser],
        }.Build();
        Db.Playlists.Add(currentUserPlaylist);

        var otherPlaylists = Enumerable
            .Range(0, 5)
            .Select(i =>
                new PlaylistBuilder
                {
                    PlaylistId = $"other-playlist-{i}",
                    Users = [otherUser],
                }.Build()
            );

        Db.Playlists.AddRange(otherPlaylists);
        await Db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var request = new GetRandomPlaylist.Request(OnlyOwnPlaylists: false);

        // Act
        var (response, result) = await App.Client.GETAsync<
            GetRandomPlaylist.Endpoint,
            GetRandomPlaylist.Request,
            GetRandomPlaylist.Response
        >(request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        result.ShouldNotBeNull();

        result.RandomPlaylist.Id.ShouldBe("current-user-playlist");
    }

    [Fact]
    public async Task GetRandomPlaylist_OnlyOwnPlaylistsTrue_FiltersOutOtherOwners()
    {
        // Arrange
        ArrangeMockSpotifyUser(DefaultSpotifyUserId);

        var currentUser = new UserBuilder
        {
            UserId = DefaultSpotifyUserId,
            Username = DefaultSpotifyDisplayName,
        }.Build();
        Db.Users.Add(currentUser);

        var ownPlaylist = new PlaylistBuilder
        {
            PlaylistId = "own-playlist",
            OwnerName = DefaultSpotifyDisplayName,
            Users = [currentUser],
        }.Build();
        var otherPlaylist = new PlaylistBuilder
        {
            PlaylistId = "other-playlist",
            OwnerName = "Someone Else",
            Users = [currentUser],
        }.Build();

        Db.Playlists.AddRange(ownPlaylist, otherPlaylist);
        await Db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var request = new GetRandomPlaylist.Request(OnlyOwnPlaylists: true);

        // Act
        var (response, result) = await App.Client.GETAsync<
            GetRandomPlaylist.Endpoint,
            GetRandomPlaylist.Request,
            GetRandomPlaylist.Response
        >(request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        result.ShouldNotBeNull();
        result.RandomPlaylist.Id.ShouldBe("own-playlist");
    }

    private void ArrangeMockSpotifyUser(string spotifyUserId)
    {
        var mockUserProfile = new PrivateUser
        {
            Id = spotifyUserId,
            DisplayName = DefaultSpotifyDisplayName,
        };

        App.MockSpotifyAuth.GetCurrentUserProfileAsync(
                Arg.Any<HttpContext>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(mockUserProfile);
    }
}
