using BpLogViewer.Cli.Configuration;
using BpLogViewer.Cli.Data;
using BpLogViewer.Cli.Models;
using BpLogViewer.Cli.Output;
using FilterSet = BpLogViewer.Cli.Filters.Filters;

namespace BpLogViewer.Cli.Commands;

public static class ResourceListCommand
{
    public static int Execute(string[] args, FilterSet filters)
    {
        ParseArgs(args, out var name, out var nameContains);

        var settings = ConfigurationLoader.Load(cliConnectionString: filters.ConnectionString);
        if (string.IsNullOrEmpty(settings.Database.ConnectionString))
        {
            ErrorWriter.WriteErrorAsync("config_invalid", "appsettings.json nicht gefunden oder Database.ConnectionString leer. Siehe self-test für Diagnose.", 3).GetAwaiter().GetResult();
            return 3;
        }

        int[]? statusIds = null;
        string[]? statusNames = null;
        if (filters.Status is { Length: > 0 })
        {
            var cache = new StatusCache();
            try { cache.Load(settings); }
            catch (Exception ex)
            {
                ErrorWriter.WriteErrorAsync("status_cache_load_failed", ex.Message, 5).GetAwaiter().GetResult();
                return 5;
            }

            var ids = new List<int>();
            var names = new List<string>();
            foreach (var s in filters.Status)
            {
                if (cache.TryGetId(s, out var id)) ids.Add(id);
                else names.Add(s);
            }

            statusIds = ids.Count > 0 ? ids.ToArray() : null;
            statusNames = names.Count > 0 ? names.ToArray() : null;
        }

        try
        {
            using var db = new BpDbContext(settings);
            var repo = new ResourceRepository(db);

            var effectiveMax = (filters.Output is { Length: > 0 } && !filters.MaxRows.HasValue)
                ? FilterSet.MaxRowsAbsoluteCeiling
                : filters.EffectiveMaxRows;

            var rows = repo.List(name, nameContains, statusIds, statusNames, effectiveMax + 1);

            var truncated = rows.Count > effectiveMax;
            if (truncated)
            {
                rows.RemoveAt(rows.Count - 1);
                ErrorWriter.WriteWarningAsync("output_truncated", $"{effectiveMax} of {effectiveMax}+ rows. Use --output to write full result.").GetAwaiter().GetResult();
            }

            if (!string.IsNullOrWhiteSpace(filters.Output))
            {
                if (!OutputFileWriter.TryWriteJsonLines(rows, PayloadJsonContext.Default.ResourceInfo, filters.Output, out var code, out var msg))
                {
                    ErrorWriter.WriteErrorAsync(code ?? "output_write_failed", msg ?? "failed to write output file", 6).GetAwaiter().GetResult();
                    return 6;
                }
                return truncated ? 2 : 0;
            }

            foreach (var row in rows)
            {
                Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(row, PayloadJsonContext.Default.ResourceInfo));
            }

            return truncated ? 2 : 0;
        }
        catch (Exception ex)
        {
            ErrorWriter.WriteErrorAsync("db_error", ex.Message, 5).GetAwaiter().GetResult();
            return 5;
        }
    }

    private static void ParseArgs(string[] args, out string? name, out bool nameContains)
    {
        name = null;
        nameContains = false;

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
            }
        }
    }
}
