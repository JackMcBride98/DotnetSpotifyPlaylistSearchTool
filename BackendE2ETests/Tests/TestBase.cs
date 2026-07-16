using NSubstitute.ClearExtensions;
using Respawn;
using Respawn.Graph;
using SpotifyPlaylistSearchTool.Api.Database;

namespace Tests;

public abstract class TestBase(App app) : IAsyncLifetime
{
    protected readonly App App = app;
    protected readonly HttpClient Client = app.Client;

    private IServiceScope? _testScope;
    protected DataContext Db { get; private set; } = null!;

    public async ValueTask InitializeAsync()
    {
        App.MockSpotifyAuth.ClearSubstitute();

        await ResetDatabaseAsync();

        _testScope = App.Services.CreateScope();

        Db = _testScope.ServiceProvider.GetRequiredService<DataContext>();
    }

    public ValueTask DisposeAsync()
    {
        _testScope?.Dispose();
        return ValueTask.CompletedTask;
    }

    private async Task ResetDatabaseAsync()
    {
        await using var conn = new Npgsql.NpgsqlConnection(App.DatabaseConnectionString);
        await conn.OpenAsync();

        var respawner = await Respawner.CreateAsync(
            conn,
            new RespawnerOptions
            {
                DbAdapter = DbAdapter.Postgres,
                TablesToIgnore = new Table[] { new Table("public", "schemaversions") },
                SchemasToInclude = ["public"],
            }
        );

        await respawner.ResetAsync(conn);
    }
}
