using BpLogViewer.Cli.Configuration;
using BpLogViewer.Cli.Data;
using BpLogViewer.Cli.Filters;
using BpLogViewer.Cli.Output;
using FilterSet = BpLogViewer.Cli.Filters.Filters;

namespace BpLogViewer.Cli.Commands;

/// <summary>
/// <c>bp-log-viewer audit list</c>.
/// </summary>
public static class AuditListCommand
{
    public static int Execute(string[] args, FilterSet filters)
    {
        ParseArgs(args, out var codes, out var user, out var targetProcess, out var targetResource, out var includeXml, out var fullComments);

        if (!CommandHost.TryLoadSettings(filters, out var settings))
        {
            return 3;
        }

        try
        {
            using var db = new BpDbContext(settings);
            var repo = new AuditRepository(db);

            var since = filters.Since ?? DateTimeOffset.UtcNow.AddDays(-7);
            var until = filters.Until;

            var effectiveMax = CommandHost.ResolveEffectiveMax(filters, fallbackDefault: 500);
            var rows = repo.List(since, until, codes, user, targetProcess, targetResource, includeXml, fullComments, effectiveMax + 1);
            var truncated = CommandHost.Truncate(rows, effectiveMax);
            return CommandHost.Emit(rows, PayloadJsonContext.Readable.AuditEventInfo, filters, truncated);
        }
        catch (Exception ex)
        {
            return CommandHost.WriteDbError(ex);
        }
    }

    private static void ParseArgs(
        string[] args,
        out string[]? codes,
        out string? user,
        out string? targetProcess,
        out string? targetResource,
        out bool includeXml,
        out bool fullComments)
    {
        var parsed = CommandArgs.Parse(args, "audit", "list");

        var codeList = parsed.Values("--code");
        codes = codeList.Count > 0 ? codeList.ToArray() : null;
        user = parsed.Value("--user");
        targetProcess = parsed.Value("--target-process");
        targetResource = parsed.Value("--target-resource");
        includeXml = parsed.HasFlag("--include-xml");
        fullComments = parsed.HasFlag("--full-comments");
    }
}
