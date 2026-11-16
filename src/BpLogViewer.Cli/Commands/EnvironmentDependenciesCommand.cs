using BpLogViewer.Cli.Configuration;
using BpLogViewer.Cli.Data;
using BpLogViewer.Cli.Filters;
using BpLogViewer.Cli.Output;
using FilterSet = BpLogViewer.Cli.Filters.Filters;

namespace BpLogViewer.Cli.Commands;

/// <summary>
/// <c>bp-log-viewer environment dependencies</c> - Zuordnung von Prozessen zu
/// Environment Variables aus <c>BPAProcessEnvironmentVarDependency</c>.
/// </summary>
public static class EnvironmentDependenciesCommand
{
    public static int Execute(string[] args, FilterSet filters)
    {
        if (!TryParseArgs(args, out var processName, out var variableName, out var parseError))
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
            var rows = repo.ListVarDependencies(processName, variableName, effectiveMax + 1);
            var truncated = CommandHost.Truncate(rows, effectiveMax);
            return CommandHost.Emit(rows, PayloadJsonContext.Readable.EnvironmentVarDependencyInfo, filters, truncated);
        }
        catch (Exception ex)
        {
            return CommandHost.WriteDbError(ex);
        }
    }

    private static bool TryParseArgs(string[] args, out string? processName, out string? variableName, out string? error)
    {
        processName = null;
        variableName = null;
        error = null;

        var parsed = CommandArgs.Parse(args, "environment", "dependencies");
        if (parsed.Error is not null)
        {
            error = parsed.Error;
            return false;
        }

        processName = parsed.Value("--process-name");
        variableName = parsed.Value("--variable");
        return true;
    }
}
