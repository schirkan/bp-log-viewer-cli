using BpLogViewer.Cli.Configuration;
using BpLogViewer.Cli.Data;
using BpLogViewer.Cli.Output;
using FilterSet = BpLogViewer.Cli.Filters.Filters;

namespace BpLogViewer.Cli.Commands;

public static class ProcessListCommand
{
    public static int Execute(string[] args, FilterSet filters)
    {
        if (!ParseArgs(args, out var name, out var nameContains, out var type, out var hasErrorsSince, out var parseError))
        {
            ErrorWriter.WriteErrorAsync("command_invalid", parseError!, 11).GetAwaiter().GetResult();
            return 11;
        }

        var settings = ConfigurationLoader.Load(cliConnectionString: filters.ConnectionString);
        if (string.IsNullOrEmpty(settings.Database.ConnectionString))
        {
            ErrorWriter.WriteErrorAsync("config_invalid", "appsettings.json nicht gefunden oder Database.ConnectionString leer. Siehe self-test für Diagnose.", 3).GetAwaiter().GetResult();
            return 3;
        }

        try
        {
            using var db = new BpDbContext(settings);
            var repo = new ProcessRepository(db);

            var effectiveMax = (filters.Output is { Length: > 0 } && !filters.MaxRows.HasValue)
                ? FilterSet.MaxRowsAbsoluteCeiling
                : filters.EffectiveMaxRows;

            var rows = repo.List(name, nameContains, type, hasErrorsSince, effectiveMax + 1);

            var truncated = rows.Count > effectiveMax;
            if (truncated)
            {
                rows.RemoveAt(rows.Count - 1);
                ErrorWriter.WriteWarningAsync("output_truncated", $"{effectiveMax} of {effectiveMax}+ rows. Use --output to write full result.").GetAwaiter().GetResult();
            }

            if (!string.IsNullOrWhiteSpace(filters.Output))
            {
                if (!OutputFileWriter.TryWriteJsonLines(rows, PayloadJsonContext.Default.ProcessInfo, filters.Output, out var code, out var msg))
                {
                    ErrorWriter.WriteErrorAsync(code ?? "output_write_failed", msg ?? "failed to write output file", 6).GetAwaiter().GetResult();
                    return 6;
                }
                return truncated ? 2 : 0;
            }

            foreach (var row in rows)
            {
                Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(row, PayloadJsonContext.Default.ProcessInfo));
            }

            return truncated ? 2 : 0;
        }
        catch (Exception ex)
        {
            ErrorWriter.WriteErrorAsync("db_error", ex.Message, 5).GetAwaiter().GetResult();
            return 5;
        }
    }

    private static bool ParseArgs(string[] args, out string? name, out bool nameContains, out string? type, out DateTimeOffset? hasErrorsSince, out string? error)
    {
        name = null;
        nameContains = false;
        type = null;
        hasErrorsSince = null;
        error = null;

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

            if (args[i] == "--type" && i + 1 < args.Length)
            {
                type = args[++i];
                if (!type.Equals("process", StringComparison.OrdinalIgnoreCase) &&
                    !type.Equals("object", StringComparison.OrdinalIgnoreCase))
                {
                    error = "--type supports only process|object";
                    return false;
                }
                continue;
            }

            if (args[i] == "--has-errors-since" && i + 1 < args.Length)
            {
                if (!DateTimeOffset.TryParse(args[++i], System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.AssumeUniversal, out var dt))
                {
                    error = "invalid --has-errors-since datetime";
                    return false;
                }
                hasErrorsSince = dt.ToUniversalTime();
                continue;
            }
        }

        return true;
    }
}
