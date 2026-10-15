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
            return CommandHost.Emit(rows, PayloadJsonContext.Default.QueueInfo, filters, truncated);
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
