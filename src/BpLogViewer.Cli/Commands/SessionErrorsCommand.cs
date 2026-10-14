using BpLogViewer.Cli.Configuration;
using BpLogViewer.Cli.Data;
using BpLogViewer.Cli.Models;
using BpLogViewer.Cli.Output;
using FilterSet = BpLogViewer.Cli.Filters.Filters;

namespace BpLogViewer.Cli.Commands;

/// <summary>
/// <c>bp-log-viewer session errors</c> (F-005).
/// </summary>
public static class SessionErrorsCommand
{
    public static int Execute(string[] args, FilterSet filters)
    {
        ParseArgs(args, out var aggregate, out var exceptionTypes);

        var settings = ConfigurationLoader.Load(cliConnectionString: filters.ConnectionString);
        if (string.IsNullOrEmpty(settings.Database.ConnectionString))
        {
            ErrorWriter.WriteErrorAsync("config_invalid", "appsettings.json nicht gefunden oder Database.ConnectionString leer. Siehe self-test für Diagnose.", 3).GetAwaiter().GetResult();
            return 3;
        }

        if (filters.Text is { Length: > 0 })
        {
            ErrorWriter.WriteWarningAsync("text_filter_perf_warning", "full table scan, consider narrowing filters").GetAwaiter().GetResult();
        }

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

        try
        {
            using var db = new BpDbContext(settings);
            var repo = new SessionErrorRepository(db);

            var effectiveMax = filters.EffectiveMaxRows;
            var rows = repo.List(
                since: filters.Since,
                until: filters.Until,
                process: filters.Process,
                resource: filters.Resource,
                text: filters.Text,
                exceptionTypes: exceptionTypes,
                maxRowsPlusOne: effectiveMax + 1);

            var truncated = rows.Count > effectiveMax;
            if (truncated)
            {
                rows.RemoveAt(rows.Count - 1);
                ErrorWriter.WriteWarningAsync("output_truncated", $"{effectiveMax} of {effectiveMax}+ rows. Use --output to write full result.").GetAwaiter().GetResult();
            }

            if (!string.IsNullOrWhiteSpace(filters.Output))
            {
                ErrorWriter.WriteErrorAsync("not_implemented", "--output noch nicht implementiert (F-012 folgt)", 0).GetAwaiter().GetResult();
                return 0;
            }

            if (!aggregate)
            {
                for (var i = 0; i < rows.Count; i++)
                {
                    var r = rows[i];
                    rows[i] = r with { StatusName = statusCache.GetName(r.Status) };
                    Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(rows[i], PayloadJsonContext.Default.SessionInfo));
                }
            }
            else
            {
                foreach (var item in BuildAggregate(rows))
                {
                    Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(item, PayloadJsonContext.Default.SessionErrorAggregate));
                }
            }

            return truncated ? 2 : 0;
        }
        catch (Exception ex)
        {
            ErrorWriter.WriteErrorAsync("db_error", ex.Message, 5).GetAwaiter().GetResult();
            return 5;
        }
    }

    private static IReadOnlyList<SessionErrorAggregate> BuildAggregate(IReadOnlyList<SessionInfo> rows)
    {
        return rows
            .Where(r => !string.IsNullOrWhiteSpace(r.ExceptionType))
            .GroupBy(r => r.ExceptionType!, StringComparer.Ordinal)
            .Select(g =>
            {
                var ordered = g.OrderByDescending(x => x.StartedAt).ToList();
                var firstSeen = g.Min(x => x.StartedAt);
                var lastSeen = g.Max(x => x.StartedAt);
                var processes = g.Select(x => x.Process).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.Ordinal).Take(20).Cast<string>().ToArray();
                var resources = g.Select(x => x.Resource).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.Ordinal).Take(20).Cast<string>().ToArray();
                var samples = ordered.Select(x => x.SessionNumber).Distinct().Take(5).ToArray();
                var sampleMsg = ordered.Select(x => x.ExceptionMessage).FirstOrDefault(x => !string.IsNullOrWhiteSpace(x));
                if (!string.IsNullOrEmpty(sampleMsg) && sampleMsg.Length > 200)
                {
                    sampleMsg = sampleMsg[..200];
                }

                return new SessionErrorAggregate(
                    ExceptionType: g.Key,
                    Count: g.Count(),
                    FirstSeen: firstSeen,
                    LastSeen: lastSeen,
                    Processes: processes,
                    Resources: resources,
                    SampleSessionNumbers: samples,
                    SampleMessagePreview: sampleMsg);
            })
            .OrderByDescending(x => x.Count)
            .ToList();
    }

    private static void ParseArgs(string[] args, out bool aggregate, out string[]? exceptionTypes)
    {
        aggregate = false;
        var list = new List<string>();

        for (var i = 0; i < args.Length; i++)
        {
            var token = args[i];
            if (token == "--aggregate")
            {
                aggregate = true;
                continue;
            }

            if (token == "--exception-type" && i + 1 < args.Length)
            {
                list.Add(args[++i]);
            }
        }

        exceptionTypes = list.Count > 0 ? list.ToArray() : null;
    }
}
