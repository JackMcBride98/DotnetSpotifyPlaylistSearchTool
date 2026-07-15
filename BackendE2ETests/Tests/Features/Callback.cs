using SpotifyPlaylistSearchTool.Api.Features;

namespace Tests.Features;

public class CallbackTests(App app) : TestBase(app)
{
    [Fact]
    public async Task Callback_CodeNotProvided_ReturnsValidationError()
    {
        // Arrange
        var request = new Callback.Request(null);

        // Act
        // Fetch only the raw HttpResponseMessage
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

        // Verify your custom validation message is present in the response string
        content.ShouldContain("Authorization code is required.");
    }

    [Fact]
    public async Task Callback_OAuthClientTokenRequestNotSuccess_ReturnsError() { }

    [Fact]
    public async Task Callback_TokenRequestSuccesful_SetsHttpContextCookies() { }

    [Fact]
    public async Task Callback_TokenRequestSuccesful_UserDoesNotExist_CreatesNewUser() { }

    [Fact]
    public async Task Callback_TokenRequestSuccesful_UserAlreadyExists_UpdatesExistingUserTokens() { }

    [Fact]
    public async Task Callback_TokenRequestSuccesful_ReturnsRedirectToProfile() { }
}
