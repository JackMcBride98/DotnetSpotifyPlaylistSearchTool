using Builders;
using Microsoft.AspNetCore.Http;
using NodaTime;
using NSubstitute;
using SpotifyAPI.Web;
using SpotifyPlaylistSearchTool.Api.Database;
using SpotifyPlaylistSearchTool.Api.Features.User;
using Image = SpotifyAPI.Web.Image;

namespace Tests.Features;

public class GetProfileEndpointTests(App app) : TestBase(app)
{
    private const string DefaultUserId = "user_123";
    private const string DefaultDisplayName = "Jane Doe";

    [Fact]
    public async Task GetProfile_UserExists_ReturnsProfileAndSyncStatus()
    {
        // Arrange
        const string profileImageUrl = "https://example.com/image.jpg";
        var completedAtInstant = Instant.FromUtc(2026, 7, 29, 10, 0, 0);

        var user = new UserBuilder { UserId = DefaultUserId, Username = DefaultDisplayName }
            .WithSyncState(
                status: SyncStatus.Completed,
                totalPlaylists: 12,
                errorMessage: null,
                completedAt: completedAtInstant
            )
            .WithPlaylists(2)
            .Build();

        Db.Users.Add(user);
        await Db.SaveChangesAsync(TestContext.Current.CancellationToken);

        SetupMockSpotifyUser(
            userId: DefaultUserId,
            displayName: DefaultDisplayName,
            images: [new Image { Url = profileImageUrl }]
        );

        // Act
        var (response, result) = await App.Client.GETAsync<
            GetProfile.Endpoint,
            GetProfile.Response
        >();

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        result.User.Id.ShouldBe(DefaultUserId);
        result.User.DisplayName.ShouldBe(DefaultDisplayName);
        result.User.ProfileImageUrl.ShouldBe(profileImageUrl);
        result.User.TotalPlaylists.ShouldBe(2);

        result.SyncStatus.Status.ShouldBe(SyncStatus.Completed);
        result.SyncStatus.TotalPlaylists.ShouldBe(12);
        result.SyncStatus.ErrorMessage.ShouldBeNull();
        result.SyncStatus.CompletedAt.ShouldBe(completedAtInstant.ToDateTimeOffset());
    }

    [Fact]
    public async Task GetProfile_SyncFailed_ReturnsSyncErrorMessageAndNullCompletedAt()
    {
        // Arrange
        const string errorMessage = "Spotify API Rate Limit Exceeded";

        var user = new UserBuilder { UserId = DefaultUserId, Username = DefaultDisplayName }
            .WithSyncState(
                status: SyncStatus.Failed,
                totalPlaylists: 5,
                errorMessage: errorMessage,
                completedAt: null
            )
            .Build();

        Db.Users.Add(user);
        await Db.SaveChangesAsync(TestContext.Current.CancellationToken);

        SetupMockSpotifyUser(userId: DefaultUserId, displayName: DefaultDisplayName);

        // Act
        var (response, result) = await App.Client.GETAsync<
            GetProfile.Endpoint,
            GetProfile.Response
        >();

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        // User checks
        result.User.Id.ShouldBe(DefaultUserId);
        result.User.ProfileImageUrl.ShouldBeNull();

        // Sync status checks
        result.SyncStatus.Status.ShouldBe(SyncStatus.Failed);
        result.SyncStatus.TotalPlaylists.ShouldBe(5);
        result.SyncStatus.ErrorMessage.ShouldBe(errorMessage);
        result.SyncStatus.CompletedAt.ShouldBeNull();
    }

    [Fact]
    public async Task GetProfile_UserNotInDatabase_ReturnsNotFound()
    {
        // Arrange
        SetupMockSpotifyUser(userId: DefaultUserId, displayName: DefaultDisplayName);

        // Act
        var (response, _) = await App.Client.GETAsync<GetProfile.Endpoint, GetProfile.Response>();

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    private void SetupMockSpotifyUser(
        string userId = DefaultUserId,
        string displayName = DefaultDisplayName,
        List<Image>? images = null
    )
    {
        var spotifyUserProfile = new PrivateUser
        {
            Id = userId,
            DisplayName = displayName,
            Images = images ?? [],
        };

        App.MockSpotifyAuth.GetCurrentUserProfileAsync(
                Arg.Any<HttpContext>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(spotifyUserProfile);
    }
}
