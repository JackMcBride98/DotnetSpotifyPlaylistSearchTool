using SpotifyPlaylistSearchTool.Api.Services;
using TickerQ.Utilities.Base;
using TickerQ.Utilities.Interfaces;

namespace SpotifyPlaylistSearchTool.Api.Jobs;

public class WeeklyPlaylistSyncJob(ISyncSpotifyPlaylistService syncSpotifyPlaylistService)
    : ITickerFunction
{
    //TODO once we have tested that it works, then we can switch it to weekly, but want to test locally and on deployed environment
    public const string DailyAt12PmNoonCronSchedule = "0 12 * * *";

    [TickerFunction("DailyPlaylistSync", cronExpression: DailyAt12PmNoonCronSchedule)]
    public async Task ExecuteAsync(
        TickerFunctionContext context,
        CancellationToken cancellationToken
    )
    {
        Console.WriteLine("Running daily playlist sync job at 12 PM");
        await syncSpotifyPlaylistService.SyncActiveUsersAsync(cancellationToken);
    }
}
