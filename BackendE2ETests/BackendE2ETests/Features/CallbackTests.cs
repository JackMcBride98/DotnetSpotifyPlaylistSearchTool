using Microsoft.AspNetCore.Http;

using NSubstitute;
using NSubstitute.ExceptionExtensions;
using SpotifyAPI.Web;
using Callback = SpotifyPlaylistSearchTool.Api.Features.Auth.Callback;

namespace Tests.Features;

public class CallbackTests(App app) : TestBase(app)
{
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
    public async Task Callback_ServiceThrowsAPIException_ReturnsError()
    {
        // Arrange
        var request = new Callback.Request("invalid_or_expired_code");

        App.MockSpotifyAuth
            .HandleCallbackAndUpsertUserAsync(
                request.Code,
                Arg.Any<HttpContext>(),
                Arg.Any<CancellationToken>()
            )
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
    public async Task Callback_Success_CallsAuthServiceAndRedirectsToProfile()
    {
        // Arrange
        var request = new Callback.Request("valid_auth_code");
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

        await App.MockSpotifyAuth
            .Received(1)
            .HandleCallbackAndUpsertUserAsync(
                request.Code,
                Arg.Any<HttpContext>(),
                Arg.Any<CancellationToken>()
            );
    }
}
