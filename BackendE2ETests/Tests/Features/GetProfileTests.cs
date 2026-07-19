using Builders;
using Microsoft.AspNetCore.Http;
using NodaTime.Extensions;
using NSubstitute;
using SpotifyAPI.Web;
using GetProfile = SpotifyPlaylistSearchTool.Api.Features.User.GetProfile;

namespace Tests.Features;

public class GetProfileTests(App app) : TestBase(app)
{
    private const string DefaultSpotifyUserId = "spotify_user_123";
    private const string DefaultSpotifyDisplayName = "Test User";

    [Fact]
    public async Task GetProfile_UserDoesNotExist_ThrowsError()
    {
        // Arrange
        ArrangeMockSpotifyUser(DefaultSpotifyUserId, DefaultSpotifyDisplayName);

        // Act
        var (response, _) = await App.Client.GETAsync<GetProfile.Endpoint, ErrorResponse>();

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync(
            TestContext.Current.CancellationToken
        );
        content.ShouldContain("User not found, try logging in again");
    }

    [Fact]
    public async Task GetProfile_UserExists_ReturnsProfile()
    {
        // Arrange
        ArrangeMockSpotifyUser(DefaultSpotifyUserId, DefaultSpotifyDisplayName);

        var existingUser = new UserBuilder
        {
            UserId = DefaultSpotifyUserId,
            Username = DefaultSpotifyDisplayName,
        }.Build();
        Db.Users.Add(existingUser);
        await Db.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var (response, result) = await App.Client.GETAsync<
            GetProfile.Endpoint,
            GetProfile.Response
        >();

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        result.ShouldNotBeNull();
        result.User.Id.ShouldBe(DefaultSpotifyUserId);
        result.User.DisplayName.ShouldBe(DefaultSpotifyDisplayName);
        result.TotalPlaylists.ShouldBe(0);
        result.LastSyncedAt.ShouldBeNull();
    }

    [Fact]
    public async Task GetProfile_UserExistsWithPlaylists_ReturnsTotalPlaylists()
    {
        // Arrange
        ArrangeMockSpotifyUser(DefaultSpotifyUserId, DefaultSpotifyDisplayName);

        var existingUser = new UserBuilder
        {
            UserId = DefaultSpotifyUserId,
            Username = DefaultSpotifyDisplayName,
        }
            .WithPlaylists(5)
            .Build();

        Db.Users.Add(existingUser);
        await Db.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var (response, result) = await App.Client.GETAsync<
            GetProfile.Endpoint,
            GetProfile.Response
        >();

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        result.ShouldNotBeNull();
        result.TotalPlaylists.ShouldBe(5);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task GetProfile_UserExists_ReturnsUpdatedAtCorrectly(bool userHasNoUpdatedAt)
    {
        // Arrange
        ArrangeMockSpotifyUser(DefaultSpotifyUserId, DefaultSpotifyDisplayName);

        var fixedDate = new DateTime(2026, 7, 16, 12, 0, 0, DateTimeKind.Utc).ToInstant();

        var existingUser = new UserBuilder
        {
            UserId = DefaultSpotifyUserId,
            Username = DefaultSpotifyDisplayName,
            UpdatedAt = userHasNoUpdatedAt ? null : fixedDate,
        }.Build();

        Db.Users.Add(existingUser);
        await Db.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var (response, result) = await App.Client.GETAsync<
            GetProfile.Endpoint,
            GetProfile.Response
        >();

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        if (userHasNoUpdatedAt)
        {
            result.LastSyncedAt.ShouldBeNull();
            return;
        }

        result.ShouldNotBeNull();
        result.LastSyncedAt.ShouldNotBeNull();
        result.LastSyncedAt.ShouldBe(fixedDate.ToString());
    }

    private void ArrangeMockSpotifyUser(string spotifyUserId, string displayName)
    {
        var mockUserProfile = new PrivateUser { Id = spotifyUserId, DisplayName = displayName };

        App.MockSpotifyAuth.GetCurrentUserProfileAsync(
                Arg.Any<HttpContext>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(mockUserProfile);
    }
}
