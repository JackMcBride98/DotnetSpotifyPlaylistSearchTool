namespace Tests.Services;

public class SyncSpotifyPlaylistServiceTests(App app) : TestBase(app)
{
    [Fact]
    public async Task SyncActiveUsers_CallsSyncSpotifyPlaylists_ForActiveUser() { }

    [Fact]
    public async Task SyncActiveUsers_DoesNotCallSyncSpotifyPlaylists_ForInactiveUsers() { }

    [Fact]
    public async Task SyncPlaylistsForUser_UserNotFound_ThrowsError() { }

    [Fact]
    public async Task SyncPlaylistsForUser_ApiThrowsException_RequiresProgressUpdates_SetsStatusToFailedAndErrorMessage() { }

    [Fact]
    public async Task SyncPlaylistsForUser_ApiThrowsException_RethrowsException() { }

    [Fact]
    public async Task SyncPlaylistsForUser_RequiresProgressUpdates_UserFound_InitiallySetsStatusToInProgress() { }

    [Fact]
    public async Task SyncPlaylistsForUser_RequiresProgressUpdates_PlaylistsReturnedFromApiCall_SavesTotalPlaylists() { }

    [Fact]
    public async Task SyncPlaylistsForUser_RequiresProgressUpdates_SuccessfulSync_SyncStatusSetToCompleted() { }

    [Fact]
    public async Task SyncPlaylistsForUser_SuccessfulSync_SyncCompletedAtGetsSet() { }

    [Fact]
    public async Task SyncPlaylistsForUser_SavesPlaylistWithTracksAndImages_PlaylistContainsBothEpisodesAndTracks_AsExpected() { }

    [Fact]
    public async Task SyncPlaylistsForUser_PlaylistsReturned_OnlySyncsOwnedOrCollaborativePlaylists() { }

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
