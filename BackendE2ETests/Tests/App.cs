using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using SpotifyPlaylistSearchTool.Api.Services;

namespace Tests;

public class App : AppFixture<Program>
{
    public ISpotifyAuthService MockSpotifyAuth { get; private set; } = Substitute.For<ISpotifyAuthService>();
    
    public string DatabaseConnectionString => 
        Services.GetRequiredService<IConfiguration>().GetRequiredSection("Database")["ConnectionString"]
        ?? throw new InvalidOperationException("Database connection string is missing in configuration");
    
    public App()
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");
    }
    
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