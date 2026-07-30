using Builders;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using SpotifyAPI.Web;
using SpotifyPlaylistSearchTool.Api.Database;
using SpotifyPlaylistSearchTool.Api.Services;

namespace Tests.Services;

public class SyncSpotifyPlaylistServiceTests(App app) : TestBase(app)
{
    [Fact]
    public async Task SyncActiveUsers_CallsSyncSpotifyPlaylists_ForActiveUsers()
    {
        // Arrange
        var now = SystemClock.Instance.GetCurrentInstant();
        var ct = TestContext.Current.CancellationToken;

        var activeUser1 = new UserBuilder
        {
            UserId = "active_user_1",
            LastActiveAt = now - Duration.FromDays(2),
        }.Build();

        var activeUser2 = new UserBuilder
        {
            UserId = "active_user_2",
            LastActiveAt = now - Duration.FromDays(6),
        }.Build();

        Db.Users.AddRange(activeUser1, activeUser2);
        await Db.SaveChangesAsync(ct);

        var authServiceMock = Substitute.For<ISpotifyAuthService>();

        var syncService = Substitute.ForPartsOf<SyncSpotifyPlaylistService>(Db, authServiceMock);

        syncService
            .SyncPlaylistsForUserAsync(
                Arg.Any<string>(),
                Arg.Any<bool>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(Task.CompletedTask);

        // Act
        await syncService.SyncActiveUsersAsync(ct);

        // Assert
        await syncService.Received(1).SyncPlaylistsForUserAsync("active_user_1", false, ct);
        await syncService.Received(1).SyncPlaylistsForUserAsync("active_user_2", false, ct);
    }

    [Fact]
    public async Task SyncActiveUsers_DoesNotCallSyncSpotifyPlaylists_ForInactiveUsers()
    {
        // Arrange
        var now = SystemClock.Instance.GetCurrentInstant();
        var ct = TestContext.Current.CancellationToken;

        var nullActiveUser = new UserBuilder
        {
            UserId = "null_active_user",
            LastActiveAt = null,
        }.Build();

        var oldUser1 = new UserBuilder
        {
            UserId = "old_user_1",
            LastActiveAt = now - Duration.FromDays(8),
        }.Build();

        var oldUser2 = new UserBuilder
        {
            UserId = "old_user_2",
            LastActiveAt = now - Duration.FromDays(30),
        }.Build();

        Db.Users.AddRange(nullActiveUser, oldUser1, oldUser2);
        await Db.SaveChangesAsync(ct);

        var authServiceMock = Substitute.For<ISpotifyAuthService>();

        var syncService = Substitute.ForPartsOf<SyncSpotifyPlaylistService>(Db, authServiceMock);

        syncService
            .SyncPlaylistsForUserAsync(
                Arg.Any<string>(),
                Arg.Any<bool>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(Task.CompletedTask);

        // Act
        await syncService.SyncActiveUsersAsync(ct);

        // Assert
        // Verify SyncPlaylistsForUserAsync was never called for any user
        await syncService
            .DidNotReceive()
            .SyncPlaylistsForUserAsync(
                Arg.Any<string>(),
                Arg.Any<bool>(),
                Arg.Any<CancellationToken>()
            );
    }

    [Fact]
    public async Task SyncPlaylistsForUser_UserNotFound_ThrowsError()
    {
        // Arrange
        const string existingUserId = "existing_user_id";
        const string nonExistentUserId = "ghost_user_999";
        var ct = TestContext.Current.CancellationToken;

        var existingUser = new UserBuilder { UserId = existingUserId }.Build();

        Db.Users.Add(existingUser);
        await Db.SaveChangesAsync(ct);

        var authServiceMock = Substitute.For<ISpotifyAuthService>();
        var syncService = new SyncSpotifyPlaylistService(Db, authServiceMock);

        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(async () =>
        {
            await syncService.SyncPlaylistsForUserAsync(
                nonExistentUserId,
                requiresProgressUpdates: false,
                ct
            );
        });

        exception.Message.ShouldBe("User not found");
    }

    [Fact]
    public async Task SyncPlaylistsForUser_ApiThrowsException_RequiresProgressUpdates_SetsStatusToFailedAndErrorMessage()
    {
        // Arrange
        const string userId = "error_test_user";
        const string apiErrorMessage = "Spotify API rate limit exceeded";
        var ct = TestContext.Current.CancellationToken;

        var user = new UserBuilder { UserId = userId }.Build();

        Db.Users.Add(user);
        await Db.SaveChangesAsync(ct);

        var authServiceMock = Substitute.For<ISpotifyAuthService>();
        authServiceMock
            .GetSpotifyClientAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>()
            )
            .ThrowsAsync(new Exception(apiErrorMessage));

        var syncService = new SyncSpotifyPlaylistService(Db, authServiceMock);

        // Act & Assert
        var exception = await Should.ThrowAsync<Exception>(async () =>
        {
            await syncService.SyncPlaylistsForUserAsync(userId, requiresProgressUpdates: true, ct);
        });

        exception.Message.ShouldBe(apiErrorMessage);

        // Verify the database state was updated to Failed with the correct message
        Db.ChangeTracker.Clear();
        var updatedUser = await Db.Users.SingleAsync(
            u => u.UserId == userId,
            TestContext.Current.CancellationToken
        );

        updatedUser.SyncState.Status.ShouldBe(SyncStatus.Failed);
        updatedUser.SyncState.ErrorMessage.ShouldBe(
            $"There was an error syncing playlists - {apiErrorMessage}"
        );
    }

    [Fact]
    public async Task SyncPlaylistsForUser_RequiresProgressUpdatesFalse_ApiThrowsException_RethrowsException_UserStateUnchanged()
    {
        // Arrange
        const string userId = "error_test_user";
        const string apiErrorMessage = "Spotify API rate limit exceeded";
        var ct = TestContext.Current.CancellationToken;

        var user = new UserBuilder { UserId = userId }
            .WithSyncState(SyncStatus.NotStarted)
            .Build();

        Db.Users.Add(user);
        await Db.SaveChangesAsync(ct);

        var authServiceMock = Substitute.For<ISpotifyAuthService>();
        authServiceMock
            .GetSpotifyClientAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>()
            )
            .ThrowsAsync(new Exception(apiErrorMessage));

        var syncService = new SyncSpotifyPlaylistService(Db, authServiceMock);

        // Act & Assert
        var exception = await Should.ThrowAsync<Exception>(async () =>
        {
            await syncService.SyncPlaylistsForUserAsync(userId, requiresProgressUpdates: false, ct);
        });

        exception.Message.ShouldBe(apiErrorMessage);

        // Verify the database state was updated to Failed with the correct message
        Db.ChangeTracker.Clear();
        var existingUser = await Db.Users.SingleAsync(
            u => u.UserId == userId,
            TestContext.Current.CancellationToken
        );

        existingUser.SyncState.Status.ShouldBe(SyncStatus.NotStarted);
        existingUser.SyncState.ErrorMessage.ShouldBeNull();
    }

    [Fact]
    public async Task SyncPlaylistsForUser_RequiresProgressUpdates_UserFound_InitiallySetsStatusToInProgress()
    {
        // Arrange
        const string userId = "inprogress_user_id";
        var ct = TestContext.Current.CancellationToken;

        var user = new UserBuilder { UserId = userId }.Build();

        Db.Users.Add(user);
        await Db.SaveChangesAsync(ct);

        SyncStatus? capturedStatusMidFlight = null;

        var spotifyClientMock = Substitute.For<ISpotifyClient>();
        spotifyClientMock
            .Playlists.CurrentUsers(
                Arg.Any<PlaylistCurrentUsersRequest>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(Task.FromResult(new Paging<FullPlaylist> { Items = [] }));

        var authServiceMock = Substitute.For<ISpotifyAuthService>();
        authServiceMock
            .GetSpotifyClientAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(async _ =>
            {
                // Capture DB state right after GetSpotifyClientAsync is invoked inside the service
                Db.ChangeTracker.Clear();
                var userInDb = await Db.Users.SingleAsync(u => u.UserId == userId, ct);
                capturedStatusMidFlight = userInDb.SyncState.Status;

                return spotifyClientMock;
            });

        var syncService = new SyncSpotifyPlaylistService(Db, authServiceMock);

        // Act
        await syncService.SyncPlaylistsForUserAsync(userId, requiresProgressUpdates: true, ct);

        // Assert
        capturedStatusMidFlight.ShouldBe(SyncStatus.InProgress);
    }

    [Fact]
    public async Task SyncPlaylistsForUser_RequiresProgressUpdates_PlaylistsReturnedFromApiCall_SavesTotalPlaylists()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        const string userId = "total_playlists_user_id";

        var user = new UserBuilder { UserId = userId }.Build();

        Db.Users.Add(user);
        await Db.SaveChangesAsync(cancellationToken);

        var mockPlaylists = new List<FullPlaylist>
        {
            new()
            {
                Id = "playlist_1",
                Owner = new PublicUser { Id = userId },
            },
            new()
            {
                Id = "playlist_2",
                Owner = new PublicUser { Id = userId },
            },
            new() { Id = "playlist_3", Collaborative = true },
        };

        var spotifyClientMock = Substitute.For<ISpotifyClient>();
        var authServiceMock = Substitute.For<ISpotifyAuthService>();

        authServiceMock
            .GetSpotifyClientAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(Task.FromResult(spotifyClientMock));

        var syncService = Substitute.ForPartsOf<SyncSpotifyPlaylistService>(Db, authServiceMock);

        syncService
            .GetAllCurrentUsersPlaylistsAsync(
                Arg.Any<ISpotifyClient>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(Task.FromResult<IList<FullPlaylist>>(mockPlaylists));

        syncService
            .GetAllPlaylistTracksAsync(
                Arg.Any<ISpotifyClient>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(
                Task.FromResult<IList<PlaylistTrack<IPlayableItem>>>(
                    new List<PlaylistTrack<IPlayableItem>>()
                )
            );

        // Act
        await syncService.SyncPlaylistsForUserAsync(
            userId,
            requiresProgressUpdates: true,
            cancellationToken
        );

        // Assert
        Db.ChangeTracker.Clear();
        var userInDb = await Db.Users.SingleAsync(u => u.UserId == userId, cancellationToken);

        userInDb.SyncState.TotalPlaylists.ShouldBe(3);
    }

    [Fact]
    public async Task SyncPlaylistsForUser_PlaylistsReturned_OnlySyncsOwnedOrCollaborativePlaylists() { }

    [Fact]
    public async Task SyncPlaylistsForUser_RequiresProgressUpdates_SuccessfulSync_SyncStatusSetToCompleted() { }

    [Fact]
    public async Task SyncPlaylistsForUser_SuccessfulSync_SyncCompletedAtGetsSet() { }

    [Fact]
    public async Task SyncPlaylistsForUser_SavesPlaylistWithTracksAndImages_PlaylistContainsBothEpisodesAndTracks_AsExpected() { }

    [Fact]
    public async Task SyncPlaylistsForUser_PlaylistWithNullId_SkipsPlaylistWithoutCrashing() { }

    [Fact]
    public async Task SyncPlaylistsForUser_ExistingPlaylistSnapshotIdEqual_DoesNotCallForTracks_PlaylistAndTrackUnchanged() { }

    [Fact]
    public async Task SyncPlaylistsForUser_ExistingPlaylistSnapshotIdEqual_ButUsersDoesNotContainCurrentUser_AddsUserToPlaylistUsers() { }

    [Fact]
    public async Task SyncPlaylistsForUser_ExistingPlaylistSnapshotIdsNotEqual_UserNotInPlaylistUsers_RetainsExistingUsersAndAddsUser() { }

    [Fact]
    public async Task SyncPlaylistsForUser_ExistingPlaylistSnapshotIdsNotEqual_UserInPlaylistUsers_RetainsExistingUsers() { }

    [Fact]
    public async Task SyncPlaylistsForUser_ExistingPlaylistSnapshotIdsNotEqual_UpdatesPlaylistDetailsAndOverwritesTracks() { }
}
