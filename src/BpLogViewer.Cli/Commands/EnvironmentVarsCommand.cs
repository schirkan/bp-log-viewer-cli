using BpLogViewer.Cli.Configuration;
using BpLogViewer.Cli.Data;
using BpLogViewer.Cli.Filters;
using BpLogViewer.Cli.Output;
using FilterSet = BpLogViewer.Cli.Filters.Filters;

namespace BpLogViewer.Cli.Commands;

/// <summary>
/// <c>bp-log-viewer environment vars</c> - Environment Variables aus
/// <c>BPAEnvironmentVar</c>.
/// </summary>
public static class EnvironmentVarsCommand
{
    public static int Execute(string[] args, FilterSet filters)
    {
        if (!TryParseArgs(args, out var name, out var nameContains, out var parseError))
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
            var repo = new EnvironmentRepository(db);

            var effectiveMax = CommandHost.ResolveEffectiveMax(filters);
            var rows = repo.ListVars(name, nameContains, effectiveMax + 1);
            var truncated = CommandHost.Truncate(rows, effectiveMax);
            return CommandHost.Emit(rows, PayloadJsonContext.Readable.EnvironmentVarInfo, filters, truncated);
        }
        catch (Exception ex)
        {
            return CommandHost.WriteDbError(ex);
        }
    }

    private static bool TryParseArgs(string[] args, out string? name, out bool nameContains, out string? error)
    {
        name = null;
        nameContains = false;
        error = null;

        var parsed = CommandArgs.Parse(args, "environment", "vars");
        if (parsed.Error is not null)
        {
            error = parsed.Error;
            return false;
        }

        // --name und --name-contains teilen sich den Namensfilter; das zuletzt
        // angegebene gewinnt.
        name = parsed.LastValue("--name", "--name-contains");
        nameContains = parsed.LastOption("--name", "--name-contains") == "--name-contains";
        return true;
    }
}
