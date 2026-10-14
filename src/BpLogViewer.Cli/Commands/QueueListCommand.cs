using BpLogViewer.Cli.Configuration;
using BpLogViewer.Cli.Data;
using BpLogViewer.Cli.Output;
using FilterSet = BpLogViewer.Cli.Filters.Filters;

namespace BpLogViewer.Cli.Commands;

public static class QueueListCommand
{
    public static int Execute(string[] args, FilterSet filters)
    {
        ParseArgs(args, out var name, out var nameContains, out var onlyExceptions, out var onlyRunning);

        var settings = ConfigurationLoader.Load(cliConnectionString: filters.ConnectionString);
        if (string.IsNullOrEmpty(settings.Database.ConnectionString))
        {
            ErrorWriter.WriteErrorAsync("config_invalid", "appsettings.json nicht gefunden oder Database.ConnectionString leer. Siehe self-test für Diagnose.", 3).GetAwaiter().GetResult();
            return 3;
        }

        try
        {
            using var db = new BpDbContext(settings);
            var repo = new QueueRepository(db);

            var effectiveMax = (filters.Output is { Length: > 0 } && !filters.MaxRows.HasValue)
                ? FilterSet.MaxRowsAbsoluteCeiling
                : filters.EffectiveMaxRows;

            var rows = repo.ListQueues(name, nameContains, onlyExceptions, onlyRunning, effectiveMax + 1);
            var truncated = rows.Count > effectiveMax;
            if (truncated)
            {
                rows.RemoveAt(rows.Count - 1);
                ErrorWriter.WriteWarningAsync("output_truncated", $"{effectiveMax} of {effectiveMax}+ rows. Use --output to write full result.").GetAwaiter().GetResult();
            }

            if (!string.IsNullOrWhiteSpace(filters.Output))
            {
                if (!OutputFileWriter.TryWriteJsonLines(rows, PayloadJsonContext.Default.QueueInfo, filters.Output, out var code, out var msg))
                {
                    ErrorWriter.WriteErrorAsync(code ?? "output_write_failed", msg ?? "failed to write output file", 6).GetAwaiter().GetResult();
                    return 6;
                }
                return truncated ? 2 : 0;
            }

            foreach (var row in rows)
            {
                Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(row, PayloadJsonContext.Default.QueueInfo));
            }

            return truncated ? 2 : 0;
        }
        catch (Exception ex)
        {
            ErrorWriter.WriteErrorAsync("db_error", ex.Message, 5).GetAwaiter().GetResult();
            return 5;
        }
    }

    private static void ParseArgs(string[] args, out string? name, out bool nameContains, out bool onlyExceptions, out bool onlyRunning)
    {
        name = null;
        nameContains = false;
        onlyExceptions = false;
        onlyRunning = false;

        for (var i = 0; i < args.Length; i++)
        {
            if (args[i] == "--name" && i + 1 < args.Length)
            {
                name = args[++i];
                continue;
            }
            if (args[i] == "--name-contains" && i + 1 < args.Length)
            {
                name = args[++i];
                nameContains = true;
                continue;
            }
            if (args[i] == "--only-exceptions")
            {
                onlyExceptions = true;
                continue;
            }
            if (args[i] == "--only-running")
            {
                onlyRunning = true;
            }
        }
    }
}
