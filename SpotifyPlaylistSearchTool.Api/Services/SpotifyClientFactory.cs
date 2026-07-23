using SpotifyAPI.Web;

namespace SpotifyPlaylistSearchTool.Api.Services;

public interface ISpotifyClientFactory
{
    ISpotifyClient CreateClient(string accessToken);
    ISpotifyClient CreateClient(SpotifyClientConfig config);
}

public class SpotifyClientFactory : ISpotifyClientFactory
{
    public ISpotifyClient CreateClient(string accessToken)
    {
        return new SpotifyClient(accessToken);
    }

    public ISpotifyClient CreateClient(SpotifyClientConfig config)
    {
        return new SpotifyClient(config);
    }
}
