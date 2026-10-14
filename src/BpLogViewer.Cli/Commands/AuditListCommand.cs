using BpLogViewer.Cli.Configuration;
using BpLogViewer.Cli.Data;
using BpLogViewer.Cli.Output;
using FilterSet = BpLogViewer.Cli.Filters.Filters;

namespace BpLogViewer.Cli.Commands;

public static class AuditListCommand
{
    public static int Execute(string[] args, FilterSet filters)
    {
        ParseArgs(args, out var codes, out var user, out var targetProcess, out var targetResource, out var includeXml, out var fullComments);

        var settings = ConfigurationLoader.Load(cliConnectionString: filters.ConnectionString);
        if (string.IsNullOrEmpty(settings.Database.ConnectionString))
        {
            ErrorWriter.WriteErrorAsync("config_invalid", "appsettings.json nicht gefunden oder Database.ConnectionString leer. Siehe self-test für Diagnose.", 3).GetAwaiter().GetResult();
            return 3;
        }

        try
        {
            using var db = new BpDbContext(settings);
            var repo = new AuditRepository(db);

            var since = filters.Since ?? DateTimeOffset.UtcNow.AddDays(-7);
            var until = filters.Until;

            var effectiveMax = (filters.Output is { Length: > 0 } && !filters.MaxRows.HasValue)
                ? FilterSet.MaxRowsAbsoluteCeiling
                : (filters.MaxRows ?? 500);

            var rows = repo.List(since, until, codes, user, targetProcess, targetResource, includeXml, fullComments, effectiveMax + 1);
            var truncated = rows.Count > effectiveMax;
            if (truncated)
            {
                rows.RemoveAt(rows.Count - 1);
                ErrorWriter.WriteWarningAsync("output_truncated", $"{effectiveMax} of {effectiveMax}+ rows. Use --output to write full result.").GetAwaiter().GetResult();
            }

            if (!string.IsNullOrWhiteSpace(filters.Output))
            {
                if (!OutputFileWriter.TryWriteJsonLines(rows, PayloadJsonContext.Default.AuditEventInfo, filters.Output, out var code, out var msg))
                {
                    ErrorWriter.WriteErrorAsync(code ?? "output_write_failed", msg ?? "failed to write output file", 6).GetAwaiter().GetResult();
                    return 6;
                }
                return truncated ? 2 : 0;
            }

            foreach (var row in rows)
            {
                Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(row, PayloadJsonContext.Default.AuditEventInfo));
            }

            return truncated ? 2 : 0;
        }
        catch (Exception ex)
        {
            ErrorWriter.WriteErrorAsync("db_error", ex.Message, 5).GetAwaiter().GetResult();
            return 5;
        }
    }

    private static void ParseArgs(string[] args, out string[]? codes, out string? user, out string? targetProcess, out string? targetResource, out bool includeXml, out bool fullComments)
    {
        var codeList = new List<string>();
        user = null;
        targetProcess = null;
        targetResource = null;
        includeXml = false;
        fullComments = false;

        for (var i = 0; i < args.Length; i++)
        {
            if (args[i] == "--code" && i + 1 < args.Length)
            {
                codeList.Add(args[++i]);
                continue;
            }
            if (args[i] == "--user" && i + 1 < args.Length)
            {
                user = args[++i];
                continue;
            }
            if (args[i] == "--target-process" && i + 1 < args.Length)
            {
                targetProcess = args[++i];
                continue;
            }
            if (args[i] == "--target-resource" && i + 1 < args.Length)
            {
                targetResource = args[++i];
                continue;
            }
            if (args[i] == "--include-xml") { includeXml = true; continue; }
            if (args[i] == "--full-comments") { fullComments = true; }
        }

        codes = codeList.Count > 0 ? codeList.ToArray() : null;
    }
}
