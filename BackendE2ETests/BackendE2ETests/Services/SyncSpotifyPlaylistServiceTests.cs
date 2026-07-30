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

        var syncService = CreateSyncServiceSubstituteForSyncActiveUsers();

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

        var syncService = CreateSyncServiceSubstituteForSyncActiveUsers();

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

        var syncService = CreateSyncServiceWithAuthServiceError(apiErrorMessage);

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

        var syncService = CreateSyncServiceWithAuthServiceError(apiErrorMessage);

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

        var syncService = CreateSyncServiceWithMockedPlaylists(mockPlaylists);

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
    public async Task SyncPlaylistsForUser_PlaylistsReturned_OnlySyncsOwnedOrCollaborativePlaylists()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        const string targetUserId = "target_user_id";
        const string otherUserId = "other_user_id";

        var user = new UserBuilder { UserId = targetUserId }.Build();

        Db.Users.Add(user);
        await Db.SaveChangesAsync(ct);

        var mockPlaylists = new List<FullPlaylist>
        {
            new()
            {
                Id = "owned_playlist_1",
                Owner = new PublicUser { Id = targetUserId },
                Collaborative = false,
            },
            new()
            {
                Id = "owned_playlist_2",
                Owner = new PublicUser { Id = targetUserId },
                Collaborative = false,
            },
            new()
            {
                Id = "collaborative_playlist",
                Owner = new PublicUser { Id = otherUserId },
                Collaborative = true,
            },
            new()
            {
                Id = "ignored_other_user_playlist",
                Owner = new PublicUser { Id = otherUserId },
                Collaborative = false,
            },
        };

        var syncService = CreateSyncServiceWithMockedPlaylists(mockPlaylists);

        // Act
        await syncService.SyncPlaylistsForUserAsync(
            targetUserId,
            requiresProgressUpdates: false,
            ct
        );

        // Assert
        Db.ChangeTracker.Clear();

        var savedPlaylists = await Db.Playlists.ToListAsync(ct);

        savedPlaylists.Count.ShouldBe(3);
        savedPlaylists
            .Select(p => p.PlaylistId)
            .ShouldBe(
                new[] { "owned_playlist_1", "owned_playlist_2", "collaborative_playlist" },
                ignoreOrder: true
            );
    }

    [Fact]
    public async Task SyncPlaylistsForUser_RequiresProgressUpdates_SuccessfulSync_SyncStatusSetToCompleted()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        const string userId = "completed_status_user_id";

        var user = new UserBuilder { UserId = userId }
            .WithSyncState(SyncStatus.NotStarted)
            .Build();

        Db.Users.Add(user);
        await Db.SaveChangesAsync(ct);

        var mockPlaylists = new List<FullPlaylist>
        {
            new()
            {
                Id = "user_playlist_1",
                Owner = new PublicUser { Id = userId },
            },
        };

        var syncService = CreateSyncServiceWithMockedPlaylists(mockPlaylists);

        // Act
        await syncService.SyncPlaylistsForUserAsync(userId, requiresProgressUpdates: true, ct);

        // Assert
        Db.ChangeTracker.Clear();
        var userInDb = await Db.Users.SingleAsync(u => u.UserId == userId, ct);

        userInDb.SyncState.Status.ShouldBe(SyncStatus.Completed);
        userInDb.SyncState.CompletedAt.ShouldNotBeNull();
    }

    [Fact]
    public async Task SyncPlaylistsForUser_SavesPlaylistWithTracksAndImages_PlaylistContainsBothEpisodesAndTracks_AsExpected()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        const string userId = "world_class_dev_user_id";
        const string playlistId = "poly_playlist_99";
        const string snapshotId = "snapshot_v1_abc";

        var user = new UserBuilder { UserId = userId }.Build();
        Db.Users.Add(user);
        await Db.SaveChangesAsync(ct);

        var mockTrack = new FullTrack
        {
            Type = ItemType.Track,
            Name = "Midnight City",
            Artists = new List<SimpleArtist>
            {
                new() { Name = "M83" },
                new() { Name = "Anthony Gonzalez" },
            },
        };

        var mockEpisode = new FullEpisode
        {
            Type = ItemType.Episode,
            Name = "Episode 42: Deep Dive into EF Core",
            Show = new SimpleShow { Name = "The C# Tech Podcast" },
        };

        var mockPlaylistTracks = new Paging<PlaylistTrack<IPlayableItem>>
        {
            Items = new List<PlaylistTrack<IPlayableItem>>
            {
                new() { Item = mockTrack },
                new() { Item = mockEpisode },
            },
        };

        var mockPlaylist = new FullPlaylist
        {
            Id = playlistId,
            Name = "Synthwave & C# Podcasts",
            Description = "The ultimate dev playlist",
            Owner = new PublicUser { Id = userId, DisplayName = "Lead Architect" },
            SnapshotId = snapshotId,
            Collaborative = false,
            Images = new List<SpotifyAPI.Web.Image>
            {
                new()
                {
                    Url = "https://cdn.spotify.com/images/playlist_cover.jpg",
                    Width = 640,
                    Height = 640,
                },
            },
            Items = mockPlaylistTracks,
        };

        var syncService = CreateSyncServiceWithMockedPlaylists(
            new List<FullPlaylist> { mockPlaylist }
        );

        // Act
        await syncService.SyncPlaylistsForUserAsync(userId, requiresProgressUpdates: true, ct);

        // Assert
        Db.ChangeTracker.Clear();

        var savedPlaylist = await Db
            .Playlists.Include(p => p.Image)
            .Include(p => p.Tracks)
            .Include(p => p.Users)
            .SingleOrDefaultAsync(p => p.PlaylistId == playlistId, ct);

        savedPlaylist.ShouldNotBeNull();
        savedPlaylist.Name.ShouldBe("Synthwave & C# Podcasts");
        savedPlaylist.Description.ShouldBe("The ultimate dev playlist");
        savedPlaylist.OwnerName.ShouldBe("Lead Architect");
        savedPlaylist.SnapshotId.ShouldBe(snapshotId);

        savedPlaylist.Image.ShouldNotBeNull();
        savedPlaylist.Image.Url.ShouldBe("https://cdn.spotify.com/images/playlist_cover.jpg");
        savedPlaylist.Image.Width.ShouldBe(640);
        savedPlaylist.Image.Height.ShouldBe(640);

        savedPlaylist.Users.ShouldHaveSingleItem();
        savedPlaylist.Users!.First().UserId.ShouldBe(userId);

        savedPlaylist.Tracks!.Count.ShouldBe(2);

        var musicTrackEntity = savedPlaylist.Tracks.Single(t => t.Index == 0);
        musicTrackEntity.Name.ShouldBe("Midnight City");
        musicTrackEntity.ArtistName.ShouldBe("M83, Anthony Gonzalez");
        musicTrackEntity.PlaylistId.ShouldBe(playlistId);

        var podcastEpisodeEntity = savedPlaylist.Tracks.Single(t => t.Index == 1);
        podcastEpisodeEntity.Name.ShouldBe(
            "The C# Tech Podcast - Episode 42: Deep Dive into EF Core"
        );
        podcastEpisodeEntity.ArtistName.ShouldBe(string.Empty);
        podcastEpisodeEntity.PlaylistId.ShouldBe(playlistId);
    }

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

    private SyncSpotifyPlaylistService CreateSyncServiceSubstituteForSyncActiveUsers()
    {
        var authServiceMock = Substitute.For<ISpotifyAuthService>();
        var syncService = Substitute.ForPartsOf<SyncSpotifyPlaylistService>(Db, authServiceMock);

        syncService
            .SyncPlaylistsForUserAsync(
                Arg.Any<string>(),
                Arg.Any<bool>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(Task.CompletedTask);

        return syncService;
    }

    private SyncSpotifyPlaylistService CreateSyncServiceWithAuthServiceError(string errorMessage)
    {
        var authServiceMock = Substitute.For<ISpotifyAuthService>();
        authServiceMock
            .GetSpotifyClientAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>()
            )
            .ThrowsAsync(new Exception(errorMessage));

        return new SyncSpotifyPlaylistService(Db, authServiceMock);
    }

    private SyncSpotifyPlaylistService CreateSyncServiceWithMockedPlaylists(
        IList<FullPlaylist> mockPlaylists
    )
    {
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
            .Returns(Task.FromResult(mockPlaylists));

        syncService
            .GetAllPlaylistTracksAsync(
                Arg.Any<ISpotifyClient>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(callInfo =>
            {
                var playlistId = callInfo.ArgAt<string>(1);
                var matchingPlaylist = mockPlaylists.FirstOrDefault(p => p.Id == playlistId);

                IList<PlaylistTrack<IPlayableItem>> tracks = matchingPlaylist?.Items?.Items ?? [];

                return Task.FromResult(tracks);
            });

        return syncService;
    }
}
