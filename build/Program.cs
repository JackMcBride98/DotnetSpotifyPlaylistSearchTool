using System;
using Cake.Common;
using Cake.Common.Diagnostics;
using Cake.Common.Tools.DotNet;
using Cake.Common.Tools.DotNet.Run;
using Cake.Core;
using Cake.Core.IO;
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
    public string DbDirectoryPath { get; }
    public string DbUpProjectPath { get; }
    public string ApiProjectPath { get;  }
    public string ClientDirectoryPath { get;  }

    public BuildContext(ICakeContext context)
        : base(context)
    {
        Delay = context.Arguments.HasArgument("delay");
        DbUpProjectPath = "../SpotifyPlaylistSearchTool.Database/SpotifyPlaylistSearchTool.Database.csproj";
        DbDirectoryPath = "../SpotifyPlaylistSearchTool.Database";
        DbConnectionString =
            "Host=localhost;Port=5433;Database=SpotifyPlaylistSearchTool;Username=postgres;Password=mysecretpassword";
        ApiProjectPath = "../SpotifyPlaylistSearchTool.Api/SpotifyPlaylistSearchTool.Api.csproj";
        ClientDirectoryPath = "../client";
    }
}

[TaskName("Clean")]
public sealed class CleanTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        context.Information("Cleaning solution artifacts safely...");
        // Clean all other projects in the solution except the build one, couldn't find a better way to exclude this 
        // project from the clean task. Which meant it tries to clean itself, which causes issues.
        context.DotNetClean(context.DbUpProjectPath);
        context.DotNetClean(context.ApiProjectPath);
    }
}

[TaskName("Build")]
[IsDependentOn(typeof(CleanTask))] 
public sealed class BuildTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        context.Information("Building solution...");
        context.DotNetBuild(context.DbUpProjectPath);
        context.DotNetBuild(context.ApiProjectPath);
    }
}

[TaskName("CreateLocalDatabase")]
[IsDependentOn(typeof(BuildTask))]
public sealed class CreateLocalDatabase : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        context.Information("Starting local database container in detached mode...");
        
        // Execute docker compose directly, pointing to your database directory
        context.StartProcess("docker", new ProcessSettings 
        {
            Arguments = "compose up -d",
            WorkingDirectory = context.DbDirectoryPath
        });

        context.Information("Database container initialized successfully! Moving to next step.");
    }
}

[TaskName("DestroyLocalDatabase")]
public sealed class DestroyLocalDatabase : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        context.Warning("Destroying local database container and wiping volumes...");

        context.StartProcess("docker", new ProcessSettings
        {
            Arguments = "compose down -v",
            WorkingDirectory = context.DbDirectoryPath
        });

        context.Information("Database destroyed completely.");
    }
}

[TaskName("MigrateLocalDatabase")]
[IsDependentOn(typeof(CreateLocalDatabase))]
public sealed class MigrateLocalDatabase : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        context.Information("Running DbUp database migrations...");

        context.DotNetRun(context.DbUpProjectPath, new DotNetRunSettings
        {
            ArgumentCustomization = args => args.AppendQuoted(context.DbConnectionString)
        });

        context.Information("Database migrations applied successfully.");
    }
}

[TaskName("ResetLocalDatabase")]
public sealed class ResetLocalDatabase : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        context.Information("Executing full database reset (Drop + Re-migrate) via DbUp...");

        context.DotNetRun(context.DbUpProjectPath, new DotNetRunSettings
        {
            ArgumentCustomization = args => args
                .AppendQuoted(context.DbConnectionString)
                .Append("--reset")
        });

        context.Information("Local database environment has been completely reset and migrated!");
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