using BpLogViewer.Cli.Configuration;
using BpLogViewer.Cli.Data;
using BpLogViewer.Cli.Filters;
using BpLogViewer.Cli.Output;
using FilterSet = BpLogViewer.Cli.Filters.Filters;

namespace BpLogViewer.Cli.Commands;

/// <summary>
/// <c>bp-log-viewer process list</c>.
/// </summary>
public static class ProcessListCommand
{
    public static int Execute(string[] args, FilterSet filters)
    {
        if (!ParseArgs(args, out var name, out var nameContains, out var type, out var hasErrorsSince, out var parseError))
        {
            ErrorWriter.WriteErrorAsync("command_invalid", parseError!, 11).GetAwaiter().GetResult();
            return 11;
        }

        if (!CommandHost.TryLoadSettings(filters, out var settings))
        {
            return 3;
        }

        try
        {
            using var db = new BpDbContext(settings);
            var repo = new ProcessRepository(db);

            var effectiveMax = CommandHost.ResolveEffectiveMax(filters);
            var rows = repo.List(name, nameContains, type, hasErrorsSince, effectiveMax + 1);
            var truncated = CommandHost.Truncate(rows, effectiveMax);
            return CommandHost.Emit(rows, PayloadJsonContext.Default.ProcessInfo, filters, truncated);
        }
        catch (Exception ex)
        {
            return CommandHost.WriteDbError(ex);
        }
    }

    private static bool ParseArgs(
        string[] args,
        out string? name,
        out bool nameContains,
        out string? type,
        out DateTimeOffset? hasErrorsSince,
        out string? error)
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
