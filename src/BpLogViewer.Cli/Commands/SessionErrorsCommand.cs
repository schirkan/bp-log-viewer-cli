using BpLogViewer.Cli.Configuration;
using BpLogViewer.Cli.Data;
using BpLogViewer.Cli.Filters;
using BpLogViewer.Cli.Output;
using FilterSet = BpLogViewer.Cli.Filters.Filters;

namespace BpLogViewer.Cli.Commands;

/// <summary>
/// <c>bp-log-viewer session errors</c> (F-005). Aggregate-Modus wird in der
/// Datenbank per SQL GROUP BY berechnet, um OOM bei großen Datenmengen zu vermeiden.
/// </summary>
public static class SessionErrorsCommand
{
    public static int Execute(string[] args, FilterSet filters)
    {
        ParseArgs(args, out var aggregate, out var exceptionTypes);

        if (!CommandHost.TryLoadSettings(filters, out var settings))
        {
            return 3;
        }

        if (filters.Text is { Length: > 0 })
        {
            ErrorWriter.WriteWarningAsync("text_filter_perf_warning", "full table scan, consider narrowing filters").GetAwaiter().GetResult();
        }

        if (!CommandHost.TryLoadStatusCache(settings, out var statusCache))
        {
            return 5;
        }

        try
        {
            using var db = new BpDbContext(settings);
            var repo = new SessionErrorRepository(db);

            var effectiveMax = CommandHost.ResolveEffectiveMax(filters);
            var since = filters.Since ?? DateTimeOffset.UtcNow.AddHours(-24);
            var until = filters.Until ?? DateTimeOffset.UtcNow;

            if (!aggregate)
            {
                var rows = repo.List(since, until, filters.Process, filters.Resource, filters.Text, exceptionTypes, effectiveMax + 1);
                var truncated = CommandHost.Truncate(rows, effectiveMax);
                for (var i = 0; i < rows.Count; i++)
                {
                    rows[i] = rows[i] with { StatusName = statusCache.GetName(rows[i].Status) };
                }
                return CommandHost.Emit(rows, PayloadJsonContext.Default.SessionInfo, filters, truncated);
            }

            // Aggregate-Modus: GROUP BY in SQL (kein LINQ auf max+1 Rows).
            var aggregates = repo.Aggregate(since, until, filters.Process, filters.Resource, filters.Text, exceptionTypes);
            return CommandHost.Emit(aggregates, PayloadJsonContext.Default.SessionErrorAggregate, filters, wasTruncated: false);
        }
        catch (Exception ex)
        {
            return CommandHost.WriteDbError(ex);
        }
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
