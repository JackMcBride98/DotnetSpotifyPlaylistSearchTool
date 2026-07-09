using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;
using SpotifyPlaylistSearchTool.Api.Services;

namespace Tests;

public class App : AppFixture<Program>
{
    public ISpotifyAuthService MockSpotifyAuth { get; private set; } = Substitute.For<ISpotifyAuthService>();
    
    protected override async ValueTask PreSetupAsync()
    {

    }

    protected override void ConfigureApp(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
    }

    protected override void ConfigureServices(IServiceCollection services)
    {
        services.RemoveAll<ISpotifyAuthService>();
        services.AddSingleton(MockSpotifyAuth);
    }
}