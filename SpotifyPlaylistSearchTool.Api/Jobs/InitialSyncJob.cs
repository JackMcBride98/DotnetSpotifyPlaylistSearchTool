using SpotifyPlaylistSearchTool.Api.Services;
using TickerQ.Utilities.Base;
using TickerQ.Utilities.Interfaces;

namespace SpotifyPlaylistSearchTool.Api.Jobs;

public record InitialSyncPayload(string UserId);

public class InitialSyncJob(ISyncSpotifyPlaylistService syncSpotifyPlaylistService)
    : ITickerFunction<InitialSyncPayload>
{
    public async Task ExecuteAsync(
        TickerFunctionContext<InitialSyncPayload> context,
        CancellationToken cancellationToken
    )
    {
        Console.WriteLine($"Job {context.Id} executed, for user {context.Request.UserId}");

        await syncSpotifyPlaylistService.SyncSpotifyPlaylistAsync(
            context.Request.UserId,
            requiresProgressUpdates: true
        );
    }
}
