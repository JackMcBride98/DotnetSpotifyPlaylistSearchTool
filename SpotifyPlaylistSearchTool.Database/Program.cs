using System.Reflection;
using DbUp;

namespace SpotifyPlaylistSearchTool.Database;

class Program
{
    static int Main(string[] args)
    {
        var connectionString =
            args.FirstOrDefault()
            ?? "Host=localhost;Port=5433;Database=SpotifyPlaylistSearchTool;Username=postgres;Password=mysecretpassword";

        bool shouldDrop = args.Contains("--drop") || args.Contains("--reset");

        if (shouldDrop)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Dropping database using DbUp utility...");
            Console.ResetColor();

            DropDatabase.For.PostgresqlDatabase(connectionString);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Database dropped successfully.");
            Console.ResetColor();

            if (args.Contains("--drop") && !args.Contains("--reset"))
            {
                return 0;
            }
        }

        Console.WriteLine("Starting DbUp database migration...");

        EnsureDatabase.For.PostgresqlDatabase(connectionString);

        var upgrader = DeployChanges
            .To.PostgresqlDatabase(connectionString)
            .WithScriptsEmbeddedInAssembly(Assembly.GetExecutingAssembly())
            .LogToConsole()
            .Build();

        var result = upgrader.PerformUpgrade();

        if (!result.Successful)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(result.Error);
            Console.ResetColor();
#if DEBUG
            Console.ReadLine();
#endif
            return -1;
        }

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("Success!");
        Console.ResetColor();
        return 0;
    }
}
