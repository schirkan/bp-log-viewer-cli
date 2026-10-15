using BpLogViewer.Cli.Configuration;
using BpLogViewer.Cli.Data;
using BpLogViewer.Cli.Filters;
using BpLogViewer.Cli.Models;
using BpLogViewer.Cli.Output;
using FilterSet = BpLogViewer.Cli.Filters.Filters;

namespace BpLogViewer.Cli.Commands;

/// <summary>
/// <c>bp-log-viewer session list</c> — listet Sessions aus <c>BPVSessionInfo</c>.
/// </summary>
public static class SessionListCommand
{
    public static int Execute(FilterSet filters)
    {
        if (!CommandHost.TryLoadSettings(filters, out var settings))
        {
            return 3;
        }

        if (!CommandHost.TryLoadStatusCache(settings, out var statusCache))
        {
            return 5;
        }

        if (!TryResolveStatusIds(filters, statusCache, out var statusIds, out var statusError))
        {
            ErrorWriter.WriteErrorAsync("unknown_status_value", statusError!, 11).GetAwaiter().GetResult();
            return 11;
        }

        var effectiveMax = CommandHost.ResolveEffectiveMax(filters);
        var since = filters.Since ?? DateTimeOffset.UtcNow.AddHours(-24);
        var until = filters.Until ?? DateTimeOffset.UtcNow;

        List<SessionInfo> rows;
        try
        {
            using var db = new BpDbContext(settings);
            var repo = new SessionRepository(db);
            rows = repo.List(since, until, filters.Process, filters.Resource, statusIds, filters.HasException, effectiveMax + 1);
        }
        catch (Exception ex)
        {
            return CommandHost.WriteDbError(ex);
        }

        // Pre-compute: einmal Dictionary bauen, dann O(1)-Lookups statt
        // O(n) Cache-GetName-Calls in der Schleife.
        var allStatuses = new HashSet<int>();
        for (var i = 0; i < rows.Count; i++)
        {
            allStatuses.Add(rows[i].Status);
        }
        var statusNames = new Dictionary<int, string?>(allStatuses.Count);
        foreach (var id in allStatuses)
        {
            statusNames[id] = statusCache.GetName(id);
        }
        for (var i = 0; i < rows.Count; i++)
        {
            rows[i] = rows[i] with { StatusName = statusNames[rows[i].Status] };
        }

        var truncated = CommandHost.Truncate(rows, effectiveMax);
        return CommandHost.Emit(rows, PayloadJsonContext.Readable.SessionInfo, filters, truncated);
    }

    private static bool TryResolveStatusIds(FilterSet filters, StatusCache cache, out int[]? ids, out string? error)
    {
        ids = null;
        error = null;
        if (filters.Status is not { Count: > 0 })
        {
            return true;
        }

        var list = new List<int>(filters.Status.Count);
        foreach (var s in filters.Status)
        {
            if (cache.TryGetId(s, out var id))
            {
                list.Add(id);
            }
            else
            {
                error = $"unknown status '{s}'";
                return false;
            }
        }

        ids = list.ToArray();
        return true;
    }
}
