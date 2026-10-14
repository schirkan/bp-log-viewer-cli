using BpLogViewer.Cli.Configuration;
using BpLogViewer.Cli.Data;
using BpLogViewer.Cli.Models;
using BpLogViewer.Cli.Output;
// Alias, weil der Namespace `BpLogViewer.Cli.Filters` und der Typ `Filters`
// denselben Namen tragen — ohne Alias interpretiert der Compiler `Filters`
// als Namespace statt als Typ (CS0118).
using FilterSet = BpLogViewer.Cli.Filters.Filters;

namespace BpLogViewer.Cli.Commands;

/// <summary>
/// <c>bp-log-viewer session list</c> — listet Sessions aus <c>BPVSessionInfo</c> (F-002).
/// </summary>
public static class SessionListCommand
{
    public static int Execute(FilterSet filters, string? subCommandConnectionString = null)
    {
        // 1. Config laden
        var settings = ConfigurationLoader.Load(cliConnectionString: subCommandConnectionString);
        if (string.IsNullOrEmpty(settings.Database.ConnectionString))
        {
            ErrorWriter.WriteErrorAsync(
                "config_invalid",
                "appsettings.json nicht gefunden oder Database.ConnectionString leer. " +
                "Siehe self-test für Diagnose.",
                3).GetAwaiter().GetResult();
            return 3;
        }

        // 2. StatusCache laden (für --status-Filter + status_name im Output)
        var statusCache = new StatusCache();
        try
        {
            statusCache.Load(settings);
        }
        catch (Exception ex)
        {
            ErrorWriter.WriteErrorAsync("status_cache_load_failed", ex.Message, 5).GetAwaiter().GetResult();
            return 5;
        }

        // 3. --status (Code oder Name) → numerische IDs
        int[]? statusIds = null;
        if (filters.Status is { Length: > 0 })
        {
            var resolved = new List<int>(filters.Status.Length);
            foreach (var s in filters.Status)
            {
                if (statusCache.TryGetId(s, out var id))
                {
                    resolved.Add(id);
                }
                else
                {
                    ErrorWriter.WriteErrorAsync("unknown_status_value", $"unknown status '{s}'", 11).GetAwaiter().GetResult();
                    return 11;
                }
            }

            statusIds = resolved.ToArray();
        }

        // 4. Query ausführen (LIMIT max+1 für Overflow-Detection)
        var effectiveMax = (filters.Output is { Length: > 0 } && !filters.MaxRows.HasValue)
            ? FilterSet.MaxRowsAbsoluteCeiling
            : filters.EffectiveMaxRows;
        const int overflowProbe = 1;
        List<SessionInfo> rows;
        try
        {
            using var db = new BpDbContext(settings);
            var repo = new SessionRepository(db);
            var since = filters.Since ?? DateTimeOffset.UtcNow.AddHours(-24);
            var until = filters.Until ?? DateTimeOffset.UtcNow;

            rows = repo.List(
                since: since,
                until: until,
                process: filters.Process,
                resource: filters.Resource,
                statusIds: statusIds,
                hasException: filters.HasException,
                maxRowsPlusOne: effectiveMax + overflowProbe);
        }
        catch (Exception ex)
        {
            ErrorWriter.WriteErrorAsync("db_error", ex.Message, 5).GetAwaiter().GetResult();
            return 5;
        }

        // 5. Statusnamen ergänzen + Output
        for (var i = 0; i < rows.Count; i++)
        {
            var r = rows[i];
            rows[i] = r with { StatusName = statusCache.GetName(r.Status) };
        }

        var truncated = rows.Count > effectiveMax;
        if (truncated)
        {
            rows.RemoveAt(rows.Count - 1); // letzten (overflow-) Datensatz verwerfen
            ErrorWriter.WriteWarningAsync(
                "output_truncated",
                $"{effectiveMax} of {effectiveMax}+ rows. Use --output to write full result.").GetAwaiter().GetResult();
        }

        if (filters.Output is { Length: > 0 } outputPath)
        {
            if (!OutputFileWriter.TryWriteJsonLines(rows, PayloadJsonContext.Default.SessionInfo, outputPath, out var code, out var message))
            {
                ErrorWriter.WriteErrorAsync(code ?? "output_write_failed", message ?? "failed to write output file", 6).GetAwaiter().GetResult();
                return 6;
            }
            return truncated ? 2 : 0;
        }

        foreach (var row in rows)
        {
            var json = System.Text.Json.JsonSerializer.Serialize(row, PayloadJsonContext.Default.SessionInfo);
            Console.WriteLine(json);
        }

        return truncated ? 2 : 0;
    }
}
