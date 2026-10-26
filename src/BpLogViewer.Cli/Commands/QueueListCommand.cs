using BpLogViewer.Cli.Configuration;
using BpLogViewer.Cli.Data;
using BpLogViewer.Cli.Filters;
using BpLogViewer.Cli.Output;
using FilterSet = BpLogViewer.Cli.Filters.Filters;

namespace BpLogViewer.Cli.Commands;

/// <summary>
/// <c>bp-log-viewer queue list</c>.
/// </summary>
public static class QueueListCommand
{
    public static int Execute(string[] args, FilterSet filters)
    {
        ParseArgs(args, out var name, out var nameContains, out var onlyExceptions, out var onlyRunning);

        if (!CommandHost.TryLoadSettings(filters, out var settings))
        {
            return 3;
        }

        try
        {
            using var db = new BpDbContext(settings);
            var repo = new QueueRepository(db);

            var effectiveMax = CommandHost.ResolveEffectiveMax(filters);
            var rows = repo.ListQueues(name, nameContains, onlyExceptions, onlyRunning, effectiveMax + 1);
            var truncated = CommandHost.Truncate(rows, effectiveMax);
            return CommandHost.Emit(rows, PayloadJsonContext.Readable.QueueInfo, filters, truncated);
        }
        catch (Exception ex)
        {
            return CommandHost.WriteDbError(ex);
        }
    }

    private static void ParseArgs(string[] args, out string? name, out bool nameContains, out bool onlyExceptions, out bool onlyRunning)
    {
        name = null;
        nameContains = false;
        onlyExceptions = false;
        onlyRunning = false;

        var parsed = CommandArgs.Parse(args, "queue", "list");
        if (parsed.Error is not null)
        {
            return;
        }

        // --name und --name-contains teilen sich den Namensfilter; das zuletzt
        // angegebene gewinnt.
        name = parsed.LastValue("--name", "--name-contains");
        nameContains = parsed.Values("--name-contains").Count > 0;
        onlyExceptions = parsed.HasFlag("--only-exceptions");
        onlyRunning = parsed.HasFlag("--only-running");
    }
}
