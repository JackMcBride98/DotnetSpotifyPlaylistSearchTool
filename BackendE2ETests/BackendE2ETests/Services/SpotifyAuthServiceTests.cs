using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using SpotifyAPI.Web;
using SpotifyPlaylistSearchTool.Api.Configuration;
using SpotifyPlaylistSearchTool.Api.Database;
using SpotifyPlaylistSearchTool.Api.Services;

namespace Tests.Services;

public class SpotifyAuthServiceTests(App app) : TestBase(app)
{
    private const string DefaultClientId = "test_client_id";
    private const string DefaultClientSecret = "test_client_secret";
    private const string DefaultRedirectUri = "http://localhost:5000/callback";

    [Fact]
    public async Task HandleCallbackAndUpsertUserAsync_UserDoesNotExist_CreatesNewUserInDatabase()
    {
        // Arrange
        const string authCode = "valid_auth_code";
        const string spotifyUserId = "spotify_user_999";
        const string spotifyDisplayName = "New User";
        const string accessToken = "access_token_123";
        const string refreshToken = "refresh_token_123";

        var authService = SetupAuthServiceForHandleCallback(
            authCode,
            accessToken,
            refreshToken,
            spotifyUserId,
            spotifyDisplayName
        );

        var httpContext = new DefaultHttpContext();

        // Act
        await authService.HandleCallbackAndUpsertUserAsync(
            authCode,
            httpContext,
            TestContext.Current.CancellationToken
        );

        // Assert
        Db.ChangeTracker.Clear();

        var users = await Db.Users.ToListAsync(TestContext.Current.CancellationToken);
        users.Count.ShouldBe(1);

        var createdUser = users.Single();
        createdUser.UserId.ShouldBe(spotifyUserId);
        createdUser.Username.ShouldBe(spotifyDisplayName);
        createdUser.AccessToken.ShouldBe(accessToken);
        createdUser.RefreshToken.ShouldBe(refreshToken);
    }

    [Fact]
    public async Task HandleCallbackAndUpsertUserAsync_UserAlreadyExists_UpdatesExistingUserTokens()
    {
        // Arrange
        const string authCode = "valid_auth_code";
        const string existingUserId = "spotify_user_456";
        const string newAccessToken = "new_access_token_789";
        const string newRefreshToken = "new_refresh_token_789";
        const string newDisplayName = "Updated User Name";

        var existingUser = new User(existingUserId, "Old Name", "old_access", "old_refresh");
        Db.Users.Add(existingUser);
        await Db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var authService = SetupAuthServiceForHandleCallback(
            authCode,
            newAccessToken,
            newRefreshToken,
            existingUserId,
            newDisplayName
        );

        var httpContext = new DefaultHttpContext();

        // Act
        await authService.HandleCallbackAndUpsertUserAsync(
            authCode,
            httpContext,
            TestContext.Current.CancellationToken
        );

        // Assert
        Db.ChangeTracker.Clear();

        var users = await Db.Users.ToListAsync(TestContext.Current.CancellationToken);
        users.Count.ShouldBe(1);

        var updatedUser = users.Single();
        updatedUser.UserId.ShouldBe(existingUserId);
        updatedUser.Username.ShouldBe(newDisplayName);
        updatedUser.AccessToken.ShouldBe(newAccessToken);
        updatedUser.RefreshToken.ShouldBe(newRefreshToken);
    }

    [Fact]
    public async Task HandleCallbackAndUpsertUserAsync_Success_SetsTokensInHttpResponseCookies()
    {
        // Arrange
        const string authCode = "valid_auth_code";
        const string accessToken = "cookie_access_token";
        const string refreshToken = "cookie_refresh_token";

        var authService = SetupAuthServiceForHandleCallback(
            authCode,
            accessToken,
            refreshToken,
            "user_123",
            "Test User"
        );

        var httpContext = new DefaultHttpContext();

        // Act
        await authService.HandleCallbackAndUpsertUserAsync(
            authCode,
            httpContext,
            TestContext.Current.CancellationToken
        );

        // Assert
        var cookieHeader = httpContext.Response.Headers["Set-Cookie"].ToString();
        cookieHeader.ShouldContain($"AccessToken={accessToken}");
        cookieHeader.ShouldContain($"RefreshToken={refreshToken}");
        cookieHeader.ShouldContain("httponly");
        cookieHeader.ShouldContain("samesite=strict");
        cookieHeader.ShouldContain("secure");
    }

    [Fact]
    public async Task GetSpotifyClientAsync_MissingTokensInCookiesAndParams_ThrowsInvalidOperationException()
    {
        // Arrange
        var (authService, _) = MockAuthServiceForGetClient("user_1", "Test User");
        var httpContext = new DefaultHttpContext();

        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(async () =>
        {
            await authService.GetSpotifyClientAsync(
                httpContext,
                TestContext.Current.CancellationToken
            );
        });

        exception.Message.ShouldBe("Refresh token not found in HttpContext or parameters.");
    }

    [Fact]
    public async Task GetSpotifyClientAsync_AccessTokenPresent_ReturnsClientImmediately()
    {
        // Arrange
        const string accessToken = "cookie_access_token";
        const string refreshToken = "cookie_refresh_token";
        const string userId = "spotify_user_1";

        var (authService, clientFactoryMock) = MockAuthServiceForGetClient(userId, "Display Name");

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["Cookie"] =
            $"AccessToken={accessToken}; RefreshToken={refreshToken}";

        // Act
        var client = await authService.GetSpotifyClientAsync(
            httpContext,
            TestContext.Current.CancellationToken
        );

        // Assert
        client.ShouldNotBeNull();
        clientFactoryMock.Received(1).CreateClient(accessToken);
    }

    [Fact]
    public async Task GetSpotifyClientAsync_PassedAccessTokenOverridesCookie_UsesPassedToken()
    {
        // Arrange
        const string cookieAccessToken = "cookie_access";
        const string cookieRefreshToken = "cookie_refresh";
        const string explicitAccessToken = "explicit_access";
        const string explicitRefreshToken = "explicit_refresh";

        var (authService, clientFactoryMock) = MockAuthServiceForGetClient(
            "spotify_user_2",
            "User 2"
        );

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["Cookie"] =
            $"AccessToken={cookieAccessToken}; RefreshToken={cookieRefreshToken}";

        // Act
        var client = await authService.GetSpotifyClientAsync(
            httpContext,
            TestContext.Current.CancellationToken,
            passedAccessToken: explicitAccessToken,
            passedRefreshToken: explicitRefreshToken
        );

        // Assert
        client.ShouldNotBeNull();
        clientFactoryMock.Received(1).CreateClient(explicitAccessToken);
    }

    [Fact]
    public async Task GetSpotifyClientAsync_AccessTokenMissingButRefreshTokenPresent_RefreshesAndReturnsClient()
    {
        // Arrange
        const string userId = "refreshed_user_id";
        const string refreshToken = "valid_refresh_token";
        const string newAccessToken = "new_refreshed_access";

        var user = new User(userId, "Original Name", "old_access", refreshToken);
        Db.Users.Add(user);
        await Db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var (authService, clientFactoryMock) = MockAuthServiceForGetClient(
            userId,
            "Original Name",
            refreshedAccessToken: newAccessToken,
            refreshedRefreshToken: refreshToken
        );

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["Cookie"] = $"RefreshToken={refreshToken}"; // AccessToken missing

        // Act
        var client = await authService.GetSpotifyClientAsync(
            httpContext,
            TestContext.Current.CancellationToken
        );

        // Assert
        client.ShouldNotBeNull();

        // Verify fresh client created
        clientFactoryMock.Received(1).CreateClient(newAccessToken);

        // Verify cookies updated
        var setCookieHeader = httpContext.Response.Headers["Set-Cookie"].ToString();
        setCookieHeader.ShouldContain($"AccessToken={newAccessToken}");

        // Verify DB updated
        Db.ChangeTracker.Clear();
        var updatedUser = await Db.Users.SingleAsync(
            u => u.UserId == userId,
            TestContext.Current.CancellationToken
        );
        updatedUser.AccessToken.ShouldBe(newAccessToken);
    }

    private (
        SpotifyAuthService AuthService,
        ISpotifyClientFactory ClientFactoryMock,
        ISpotifyClient ClientMock
    ) SetupBaseAuthService(string spotifyUserId, string spotifyDisplayName)
    {
        var options = Options.Create(
            new SpotifyOptions
            {
                ClientId = DefaultClientId,
                ClientSecret = DefaultClientSecret,
                RedirectUri = DefaultRedirectUri,
            }
        );

        var spotifyClientMock = Substitute.For<ISpotifyClient>();
        var spotifyClientFactoryMock = Substitute.For<ISpotifyClientFactory>();

        spotifyClientFactoryMock.CreateClient(Arg.Any<string>()).Returns(spotifyClientMock);

        spotifyClientMock
            .UserProfile.Current(Arg.Any<CancellationToken>())
            .Returns(new PrivateUser { Id = spotifyUserId, DisplayName = spotifyDisplayName });

        var authServiceMock = Substitute.ForPartsOf<SpotifyAuthService>(
            options,
            Db,
            spotifyClientFactoryMock
        );

        return (authServiceMock, spotifyClientFactoryMock, spotifyClientMock);
    }

    private (
        SpotifyAuthService AuthService,
        ISpotifyClientFactory ClientFactoryMock
    ) MockAuthServiceForGetClient(
        string spotifyUserId,
        string spotifyDisplayName,
        string refreshedAccessToken = "default_new_access",
        string refreshedRefreshToken = "default_new_refresh"
    )
    {
        var (authServiceMock, clientFactoryMock, _) = SetupBaseAuthService(
            spotifyUserId,
            spotifyDisplayName
        );

        authServiceMock
            .RefreshTokenAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(
                new AuthorizationCodeRefreshResponse
                {
                    AccessToken = refreshedAccessToken,
                    RefreshToken = refreshedRefreshToken,
                    ExpiresIn = 3600,
                }
            );

        return (authServiceMock, clientFactoryMock);
    }

    private SpotifyAuthService SetupAuthServiceForHandleCallback(
        string authCode,
        string accessToken,
        string refreshToken,
        string spotifyUserId,
        string spotifyDisplayName
    )
    {
        var (authServiceMock, _, _) = SetupBaseAuthService(spotifyUserId, spotifyDisplayName);

        authServiceMock
            .RequestTokenAsync(authCode, Arg.Any<CancellationToken>())
            .Returns(
                new AuthorizationCodeTokenResponse
                {
                    AccessToken = accessToken,
                    RefreshToken = refreshToken,
                    ExpiresIn = 3600,
                }
            );

        return authServiceMock;
    }
}
