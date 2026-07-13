using NSubstitute;
using Respawn;

namespace Tests;

public abstract class TestBase(App app) : IClassFixture<App>, IAsyncLifetime
{
    protected readonly App App = app;
    protected readonly HttpClient Client = app.Client;

    public async ValueTask InitializeAsync()
    {
        App.MockSpotifyAuth.ClearReceivedCalls();

        await ResetDatabaseAsync();
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    private async Task ResetDatabaseAsync()
    {
        await using var conn = new Npgsql.NpgsqlConnection(App.DatabaseConnectionString);
        await conn.OpenAsync();

        var respawner = await Respawner.CreateAsync(
            conn,
            new RespawnerOptions
            {
                DbAdapter = DbAdapter.Postgres,
                TablesToIgnore = ["schemaversions"],
                SchemasToInclude = ["public"],
            }
        );

        await respawner.ResetAsync(conn);
    }
}
