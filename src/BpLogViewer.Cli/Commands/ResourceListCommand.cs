using BpLogViewer.Cli.Configuration;
using BpLogViewer.Cli.Data;
using BpLogViewer.Cli.Filters;
using BpLogViewer.Cli.Output;
using FilterSet = BpLogViewer.Cli.Filters.Filters;

namespace BpLogViewer.Cli.Commands;

/// <summary>
/// <c>bp-log-viewer resource list</c>.
/// </summary>
public static class ResourceListCommand
{
    public static int Execute(string[] args, FilterSet filters)
    {
        ParseArgs(args, out var name, out var nameContains);

        if (!CommandHost.TryLoadSettings(filters, out var settings))
        {
            return 3;
        }

        if (!TryResolveStatus(filters, settings, out var statusIds, out var statusNames, out var statusExit, out var statusError))
        {
            ErrorWriter.WriteErrorAsync(statusError!, statusError!, statusExit).GetAwaiter().GetResult();
            return statusExit;
        }

        try
        {
            using var db = new BpDbContext(settings);
            var repo = new ResourceRepository(db);

            var effectiveMax = CommandHost.ResolveEffectiveMax(filters);
            var rows = repo.List(name, nameContains, statusIds, statusNames, effectiveMax + 1);
            var truncated = CommandHost.Truncate(rows, effectiveMax);
            return CommandHost.Emit(rows, PayloadJsonContext.Readable.ResourceInfo, filters, truncated);
        }
        catch (Exception ex)
        {
            return CommandHost.WriteDbError(ex);
        }
    }

    private static bool TryResolveStatus(
        FilterSet filters,
        AppSettings settings,
        out int[]? statusIds,
        out string[]? statusNames,
        out int exitCode,
        out string? error)
    {
        statusIds = null;
        statusNames = null;
        exitCode = 0;
        error = null;

        if (filters.Status is not { Count: > 0 })
        {
            return true;
        }

        if (!CommandHost.TryLoadStatusCache(settings, out var cache))
        {
            exitCode = 5;
            error = "status_cache_load_failed";
            return false;
        }

        var ids = new List<int>();
        var names = new List<string>();
        foreach (var s in filters.Status)
        {
            if (cache.TryGetId(s, out var id))
            {
                ids.Add(id);
            }
            else
            {
                names.Add(s);
            }
        }

        statusIds = ids.Count > 0 ? ids.ToArray() : null;
        statusNames = names.Count > 0 ? names.ToArray() : null;
        return true;
    }

    private static void ParseArgs(string[] args, out string? name, out bool nameContains)
    {
        name = null;
        nameContains = false;

        var parsed = CommandArgs.Parse(args, "resource", "list");
        if (parsed.Error is not null)
        {
            return;
        }

        // --name und --name-contains teilen sich den Namensfilter; das zuletzt
        // angegebene gewinnt.
        name = parsed.LastValue("--name", "--name-contains");
        nameContains = parsed.Values("--name-contains").Count > 0;
    }
}
