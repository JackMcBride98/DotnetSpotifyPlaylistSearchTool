using System;
using System.Linq;
using System.Threading.Tasks;
using Cake.Common.Diagnostics;
using Cake.Common.IO;
using Cake.Common.Tools.DotNet;
using Cake.Core;
using Cake.Core.Diagnostics;
using Cake.Frosting;

namespace Build;

public static class Program
{
    public static int Main(string[] args)
    {
        return new CakeHost()
            .UseContext<BuildContext>()
            .Run(args);
    }
}

public class BuildContext : FrostingContext
{
    public bool Delay { get; set; }
    public string DbConnectionString { get;  }
    public string DbUpProjectPath { get; }

    public BuildContext(ICakeContext context)
        : base(context)
    {
        Delay = context.Arguments.HasArgument("delay");
        DbUpProjectPath = "../SpotifyPlaylistSearchTool.Database/SpotifyPlaylistSearchTool.Database.csproj";
        DbConnectionString =
            "Host=localhost;Port=5433;Database=SpotifyPlaylistSearchTool;Username=postgres;Password=mysecretpassword";
    }
}

[TaskName("Clean")]
public sealed class CleanTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        context.Information("Cleaning solution artifacts safely...");
        context.DotNetClean("../SpotifyPlaylistSearchTool.slnx");
    }
}

[TaskName("Build")]
[IsDependentOn(typeof(CleanTask))] 
public sealed class BuildTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        context.Information("Building solution...");
        context.DotNetBuild("../SpotifyPlaylistSearchTool.slnx");
    }
}

[TaskName("Default")]
public sealed class DefaultTask : FrostingTask<BuildContext>
{
    private readonly ICakeEngine _engine;

    // Cake Frosting automatically injects the ICakeEngine via Dependency Injection
    public DefaultTask(ICakeEngine engine)
    {
        _engine = engine;
    }

    public override void Run(BuildContext context)
    {
        context.Warning("No target task specified. Please use '--target=<task-name>'");
        context.Information("--------------------------------------------------");
        context.Information("Available Tasks:");
        context.Information("--------------------------------------------------");

        foreach (var task in _engine.Tasks)
        {
            // Skip showing the Default task itself to keep the output clean
            if (task.Name.Equals("Default", StringComparison.OrdinalIgnoreCase)) 
                continue;

            context.Information($"  -> {task.Name}");
        }
        
        context.Information("--------------------------------------------------");
        context.Information("Example usage: dotnet cake --target=MigrateLocalDatabase");
    }
}