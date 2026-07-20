using System;
using Cake.Common;
using Cake.Common.Diagnostics;
using Cake.Common.Tools.DotNet;
using Cake.Common.Tools.DotNet.Format;
using Cake.Common.Tools.DotNet.Run;
using Cake.Common.Tools.DotNet.Test;
using Cake.Common.Tools.DotNet.Tool;
using Cake.Core;
using Cake.Core.IO;
using Cake.Frosting;
using Cake.Npm;

namespace Build;

public static class Program
{
    public static int Main(string[] args)
    {
        return new CakeHost().UseContext<BuildContext>().Run(args);
    }
}

public class BuildContext : FrostingContext
{
    public bool Delay { get; set; }
    public string LocalDbConnectionString { get; }
    public string LocalTestDbConnectionString { get; }
    public string DbDirectoryPath { get; }
    public string DbUpProjectPath { get; }
    public string ApiProjectPath { get; }
    public string ClientDirectoryPath { get; }
    public string BackendE2ETestsProjectPath { get; }
    public string BuildersProjectPath { get; }

    public BuildContext(ICakeContext context)
        : base(context)
    {
        Delay = context.Arguments.HasArgument("delay");
        DbUpProjectPath =
            "../SpotifyPlaylistSearchTool.Database/SpotifyPlaylistSearchTool.Database.csproj";
        DbDirectoryPath = "../SpotifyPlaylistSearchTool.Database";
        LocalDbConnectionString =
            "Host=localhost;Port=5433;Database=SpotifyPlaylistSearchTool;Username=postgres;Password=mysecretpassword";
        LocalTestDbConnectionString =
            "Host=localhost;Port=5434;Database=SpotifyPlaylistSearchToolTest;Username=postgres;Password=mysecretpassword";
        ApiProjectPath = "../SpotifyPlaylistSearchTool.Api/SpotifyPlaylistSearchTool.Api.csproj";
        ClientDirectoryPath = "../client";
        BackendE2ETestsProjectPath = "../BackendE2ETests/Tests/Tests.csproj";
        BuildersProjectPath = "../Builders/Builders.csproj";
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
        context.DotNetClean(context.BackendE2ETestsProjectPath);
        context.DotNetClean(context.BuildersProjectPath);
    }
}

[TaskName("Format")]
public sealed class FormatTask : FrostingTask<ICakeContext>
{
    public override void Run(ICakeContext context)
    {
        context.DotNetTool(
            "csharpier",
            new DotNetToolSettings { ArgumentCustomization = args => args.Append("format ../") }
        );

        context.DotNetFormat(
            "../SpotifyPlaylistSearchTool.slnx",
            new DotNetFormatSettings
            {
                VerifyNoChanges = false,
                Diagnostics = new[] { "style", "analyzers" },
            }
        );
    }
}

[TaskName("LintBackend")]
public sealed class LintBackendTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        context.DotNetTool(
            "csharpier",
            new DotNetToolSettings { ArgumentCustomization = args => args.Append("check ../") }
        );

        context.DotNetFormat(
            "../SpotifyPlaylistSearchTool.slnx",
            new DotNetFormatSettings
            {
                VerifyNoChanges = true,
                Diagnostics = new[] { "style", "analyzers" },
            }
        );
        Console.WriteLine("dotnet format passed");

        var exitCode = context.StartProcess(
            "yamllint",
            new ProcessSettings { Arguments = ".", WorkingDirectory = "../" }
        );

        if (exitCode != 0)
        {
            throw new CakeException($"yamllint failed with exit code {exitCode}");
        }
        Console.WriteLine("yamllint passed");
    }
}

[TaskName("LintFrontend")]
public sealed class LintFrontendTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        context.Information("Checking frontend formatting with Prettier...");

        context.NpmRunScript(
            "format:check",
            settings => settings.FromPath(context.ClientDirectoryPath)
        );
        context.Information("Frontend formatting check passed.");
        context.Information("Linting frontend files with ESLint...");

        context.NpmRunScript("lint", settings => settings.FromPath(context.ClientDirectoryPath));

        context.Information("ESLint check passed successfully.");
    }
}

[TaskName("TypecheckFrontend")]
public sealed class TypecheckFrontendTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        context.Information("Typechecking frontend files via TypeScript compiler (tsc)...");

        context.NpmRunScript(
            "typecheck",
            settings => settings.FromPath(context.ClientDirectoryPath)
        );

        context.Information("TypeScript typecheck passed successfully.");
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
        context.DotNetBuild(context.BackendE2ETestsProjectPath);
        context.DotNetBuild(context.BuildersProjectPath);
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
        context.StartProcess(
            "docker",
            new ProcessSettings
            {
                Arguments = "compose up -d",
                WorkingDirectory = context.DbDirectoryPath,
            }
        );

        context.Information("Database container initialized successfully! Moving to next step.");
    }
}

[TaskName("DestroyLocalDatabase")]
public sealed class DestroyLocalDatabase : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        context.Warning("Destroying local database container and wiping volumes...");

        context.StartProcess(
            "docker",
            new ProcessSettings
            {
                Arguments = "compose down -v",
                WorkingDirectory = context.DbDirectoryPath,
            }
        );

        context.Information("Database destroyed completely.");
    }
}

[TaskName("MigrateLocalDatabase")]
public sealed class MigrateLocalDatabase : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        context.Information("Running DbUp database migrations...");

        context.DotNetRun(
            context.DbUpProjectPath,
            new DotNetRunSettings
            {
                ArgumentCustomization = args => args.AppendQuoted(context.LocalDbConnectionString),
            }
        );

        context.Information("Database migrations applied successfully.");
    }
}

[TaskName("SetupLocalDatabase")]
[IsDependentOn(typeof(CreateLocalDatabase))]
[IsDependentOn(typeof(MigrateLocalDatabase))]
public sealed class SetupLocalDatabase : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        context.Information("Local Development Database Setup Complete!");
    }
}

[TaskName("ResetLocalDatabase")]
public sealed class ResetLocalDatabase : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        context.Information("Executing full database reset (Drop + Re-migrate) via DbUp...");

        context.DotNetRun(
            context.DbUpProjectPath,
            new DotNetRunSettings
            {
                ArgumentCustomization = args =>
                    args.AppendQuoted(context.LocalDbConnectionString).Append("--reset"),
            }
        );

        context.Information("Local database environment has been completely reset and migrated!");
    }
}

[TaskName("CreateTestDatabase")]
[IsDependentOn(typeof(BuildTask))]
public sealed class CreateTestDatabase : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        context.Information("Starting local testing database container...");

        context.StartProcess(
            "docker",
            new ProcessSettings
            {
                Arguments = "compose -f docker-compose.test.yml up -d",
                WorkingDirectory = context.DbDirectoryPath,
            }
        );

        context.Information("Testing database container initialized.");
    }
}

[TaskName("DestroyTestDatabase")]
public sealed class DestroyTestDatabase : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        context.Warning("Destroying test database container and wiping volumes...");

        context.StartProcess(
            "docker",
            new ProcessSettings
            {
                Arguments = "compose -f docker-compose.test.yml down -v",
                WorkingDirectory = context.DbDirectoryPath,
            }
        );

        context.Information("Test database destroyed completely.");
    }
}

[TaskName("MigrateTestDatabase")]
public sealed class MigrateTestDatabase : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        context.Information("Running DbUp database migrations against the Test environment...");

        context.DotNetRun(
            context.DbUpProjectPath,
            new DotNetRunSettings
            {
                ArgumentCustomization = args =>
                    args.AppendQuoted(context.LocalTestDbConnectionString),
            }
        );

        context.Information("Test database migrations applied successfully.");
    }
}

[TaskName("ResetTestDatabase")]
public sealed class ResetTestDatabase : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        context.Information("Executing full test database reset (Drop + Re-migrate) via DbUp...");

        context.DotNetRun(
            context.DbUpProjectPath,
            new DotNetRunSettings
            {
                ArgumentCustomization = args =>
                    args.AppendQuoted(context.LocalTestDbConnectionString).Append("--reset"),
            }
        );

        context.Information("Test database environment has been completely reset and migrated!");
    }
}

[TaskName("SetupTestDatabase")]
[IsDependentOn(typeof(CreateTestDatabase))]
[IsDependentOn(typeof(MigrateTestDatabase))]
public sealed class SetupTestDatabase : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        context.Information("Local Test Database Environment Setup Complete!");
    }
}

[TaskName("RunBackendE2ETests")]
[IsDependentOn(typeof(BuildTask))]
[IsDependentOn(typeof(ResetTestDatabase))]
public sealed class RunBackendE2ETests : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        context.Information("Executing Backend E2E/Integration tests...");

        var testSettings = new DotNetTestSettings
        {
            NoBuild = true,
            NoRestore = true,
            Configuration = "Debug",
        };

        context.DotNetTest(context.BackendE2ETestsProjectPath, testSettings);

        context.Information("All Backend E2E tests completed successfully!");
    }
}

// Make this task run what is run in the CI pipeline so developers can run it locally
[TaskName("CI")]
[IsDependentOn(typeof(LintFrontendTask))]
[IsDependentOn(typeof(LintBackendTask))]
[IsDependentOn(typeof(TypecheckFrontendTask))]
[IsDependentOn(typeof(SetupTestDatabase))]
[IsDependentOn(typeof(RunBackendE2ETests))]
public sealed class CITask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context) { }
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
