using Builders;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using SpotifyAPI.Web;
using Callback = SpotifyPlaylistSearchTool.Api.Features.Auth.Callback;

namespace Tests.Features;

public class CallbackTests(App app) : TestBase(app)
{
    private const string DefaultFakeAccessToken = "fake_access_token";
    private const string DefaultFakeRefreshToken = "fake_refresh_token";
    private const string DefaultSpotifyUserId = "spotify_user_123";
    private const string DefaultSpotifyDisplayName = "Test User";

    [Fact]
    public async Task Callback_CodeNotProvided_ReturnsValidationError()
    {
        // Arrange
        var request = new Callback.Request(null!);

        // Act
        var (response, _) = await App.Client.GETAsync<
            Callback.Endpoint,
            Callback.Request,
            EmptyResponse
        >(request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        var content = await response.Content.ReadAsStringAsync(
            TestContext.Current.CancellationToken
        );
        content.ShouldContain("Authorization code is required.");
    }

    [Fact]
    public async Task Callback_OAuthClientTokenRequestNotSuccess_ReturnsError()
    {
        // Arrange
        var request = new Callback.Request("invalid_or_expired_code");

        App.MockSpotifyAuth.RequestTokenAsync(request.Code, Arg.Any<CancellationToken>())
            .ThrowsAsync(
                new APIException("Invalid authorization code", new Exception("Bad Request"))
            );

        // Act
        var (response, errorResult) = await App.Client.GETAsync<
            Callback.Endpoint,
            Callback.Request,
            ErrorResponse
        >(request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        errorResult.ShouldNotBeNull();
        errorResult.Errors.ShouldNotBeEmpty();

        var codeError = errorResult.Errors.FirstOrDefault(e => e.Key == "code").Value;
        codeError.ShouldNotBeNull();
        codeError.ShouldContain("Spotify authentication failed: Invalid authorization code");
    }

    [Fact]
    public async Task Callback_TokenRequestSuccessful_ReturnsRedirectToProfile()
    {
        // Arrange
        var request = new Callback.Request("valid_auth_code");

        ArrangeMockSuccessfulAuthResponse(request.Code);

        var client = App.CreateClient(new() { AllowAutoRedirect = false });

        // Act
        var (response, _) = await client.GETAsync<
            Callback.Endpoint,
            Callback.Request,
            EmptyResponse
        >(request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        response.Headers.Location?.ToString().ShouldBe("/profile");
    }

    [Fact]
    public async Task Callback_TokenRequestSuccessful_SetsHttpContextCookies()
    {
        // Arrange
        var request = new Callback.Request("valid_auth_code");

        ArrangeMockSuccessfulAuthResponse(request.Code);

        var client = App.CreateClient(new() { AllowAutoRedirect = false });

        // Act
        var (response, _) = await client.GETAsync<
            Callback.Endpoint,
            Callback.Request,
            EmptyResponse
        >(request);

        // Assert
        // Extract and assert on cookies from the response "Set-Cookie" headers
        response.Headers.TryGetValues("Set-Cookie", out var cookies).ShouldBeTrue();
        var cookieList = cookies.ToList();

        // Verify AccessToken cookie details
        var accessTokenCookie = cookieList.FirstOrDefault(c => c.Contains("AccessToken"));
        accessTokenCookie.ShouldNotBeNull();
        accessTokenCookie.ShouldContain("AccessToken=fake_access_token");
        accessTokenCookie.ShouldContain("httponly");
        accessTokenCookie.ShouldContain("samesite=strict");
        accessTokenCookie.ShouldContain("secure");

        // Verify RefreshToken cookie details
        var refreshTokenCookie = cookieList.FirstOrDefault(c => c.Contains("RefreshToken"));
        refreshTokenCookie.ShouldNotBeNull();
        refreshTokenCookie.ShouldContain("RefreshToken=fake_refresh_token");
        refreshTokenCookie.ShouldContain("httponly");
        refreshTokenCookie.ShouldContain("samesite=strict");
        refreshTokenCookie.ShouldContain("secure");
    }

    [Fact]
    public async Task Callback_TokenRequestSuccessful_UserDoesNotExist_CreatesNewUser()
    {
        // Arrange
        var request = new Callback.Request("valid_auth_code");

        ArrangeMockSuccessfulAuthResponse(request.Code);

        // Act
        await App.Client.GETAsync<Callback.Endpoint, Callback.Request, EmptyResponse>(request);

        // Assert
        var user = await Db.Users.SingleOrDefaultAsync(TestContext.Current.CancellationToken);
        user.ShouldNotBeNull();
        user.UserId.ShouldBe(DefaultSpotifyUserId);
        user.Username.ShouldBe(DefaultSpotifyDisplayName);
        user.AccessToken.ShouldBe(DefaultFakeAccessToken);
        user.RefreshToken.ShouldBe(DefaultFakeRefreshToken);
        user.UpdatedAt.ShouldBeNull();
    }

    [Fact]
    public async Task Callback_TokenRequestSuccessful_UserAlreadyExists_UpdatesExistingUserTokens()
    {
        // Arrange
        var request = new Callback.Request("valid_auth_code");
        ArrangeMockSuccessfulAuthResponse(request.Code);

        var existingUser = new UserBuilder { UserId = DefaultSpotifyUserId }.Build();
        Db.Users.Add(existingUser);
        await Db.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        await App.Client.GETAsync<Callback.Endpoint, Callback.Request, EmptyResponse>(request);

        // Assert
        Db.ChangeTracker.Clear();

        var users = await Db.Users.ToListAsync(TestContext.Current.CancellationToken);
        users.Count.ShouldBe(1);

        var user = users.Single();
        user.UserId.ShouldBe(DefaultSpotifyUserId);
        user.AccessToken.ShouldBe(DefaultFakeAccessToken);
        user.RefreshToken.ShouldBe(DefaultFakeRefreshToken);
    }

    private void ArrangeMockSuccessfulAuthResponse(
        string authCode,
        string accessToken = DefaultFakeAccessToken,
        string refreshToken = DefaultFakeRefreshToken,
        string spotifyUserId = DefaultSpotifyUserId,
        string displayName = DefaultSpotifyDisplayName
    )
    {
        var mockTokenResponse = new AuthorizationCodeTokenResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresIn = 3600,
        };

        var mockUserProfile = new PrivateUser { Id = spotifyUserId, DisplayName = displayName };

        App.MockSpotifyAuth.RequestTokenAsync(authCode, Arg.Any<CancellationToken>())
            .Returns(mockTokenResponse);

        App.MockSpotifyAuth.GetCurrentUserProfileAsync(
                Arg.Any<HttpContext>(),
                Arg.Any<CancellationToken>(),
                accessToken
            )
            .Returns(mockUserProfile);
    }
}
