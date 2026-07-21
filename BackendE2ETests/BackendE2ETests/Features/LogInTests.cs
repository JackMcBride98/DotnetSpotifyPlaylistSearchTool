using Microsoft.Extensions.Options;
using SpotifyPlaylistSearchTool.Api.Configuration;
using SpotifyPlaylistSearchTool.Api.Features.Auth;

namespace Tests.Features;

public class LogInTests(App app) : TestBase(app)
{
    [Fact]
    public async Task LogIn_WhenCalled_ReturnsExpectedLoginUri()
    {
        var spotifyOptions = App.Services.GetRequiredService<IOptions<SpotifyOptions>>().Value;

        // Act
        var (response, result) = await App.Client.POSTAsync<LogIn.Endpoint, LogIn.Response>();

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        result.ShouldNotBeNull();
        result.LoginUri.ShouldNotBeNull();

        var queryParams = new Dictionary<string, string>
        {
            { "client_id", spotifyOptions.ClientId },
            { "response_type", "code" },
            { "redirect_uri", spotifyOptions.RedirectUri },
            {
                "scope",
                "playlist-read-private playlist-read-collaborative user-read-private user-read-email"
            },
        };

        using var content = new FormUrlEncodedContent(queryParams);
        var expectedQueryString = await content.ReadAsStringAsync(
            TestContext.Current.CancellationToken
        );

        var expectedUri =
            $"https://accounts.spotify.com/authorize?{expectedQueryString.ToLowerInvariant()}";

        result.LoginUri.ToString().ShouldBe(expectedUri);
    }
}
