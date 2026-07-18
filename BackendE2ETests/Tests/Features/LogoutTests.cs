using Builders;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using SpotifyAPI.Web;
using LogoutFeature = SpotifyPlaylistSearchTool.Api.Features.Logout;
using Void = FastEndpoints.Void;

namespace Tests.Features;

public class LogoutTests(App app) : TestBase(app)
{
    private const string DefaultSpotifyUserId = "spotify_user_999";

    [Fact]
    public async Task Logout_UserDoesNotExist_ThrowsError()
    {
        // Arrange
        ArrangeMockSpotifyUser(DefaultSpotifyUserId);

        // Act
        var response = await App.Client.POSTAsync<LogoutFeature.Endpoint, ErrorResponse>();

        // Assert
        response.Response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        response.Result.ShouldNotBeNull();

        var content = await response.Response.Content.ReadAsStringAsync(
            TestContext.Current.CancellationToken
        );
        content.ShouldContain("User not found");
    }

    [Fact]
    public async Task Logout_UserExists_UnsetsAccessTokenAndClearsCookies()
    {
        // Arrange
        ArrangeMockSpotifyUser(DefaultSpotifyUserId);

        var existingUser = new UserBuilder
        {
            UserId = DefaultSpotifyUserId,
            AccessToken = "active-access-token",
            RefreshToken = "active-refresh-token",
        }.Build();

        Db.Users.Add(existingUser);
        await Db.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var (response, _) = await App.Client.POSTAsync<LogoutFeature.Endpoint, Void>();

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        Db.ChangeTracker.Clear();

        var updatedUser = await Db.Users.SingleAsync(
            u => u.UserId == DefaultSpotifyUserId,
            TestContext.Current.CancellationToken
        );
        updatedUser.AccessToken.ShouldBeNull();

        if (response.Headers.TryGetValues("Set-Cookie", out var cookieHeaders))
        {
            var cookieStrings = cookieHeaders.ToList();
            cookieStrings.Any(c => c.Contains("AccessToken=;")).ShouldBeTrue();
            cookieStrings.Any(c => c.Contains("RefreshToken=;")).ShouldBeTrue();
        }
    }

    private void ArrangeMockSpotifyUser(string spotifyUserId)
    {
        var mockUserProfile = new PrivateUser
        {
            Id = spotifyUserId,
            DisplayName = "Logout Test User",
        };

        App.MockSpotifyAuth.GetCurrentUserProfileAsync(
                Arg.Any<HttpContext>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(mockUserProfile);
    }
}
