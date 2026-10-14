using BpLogViewer.Cli.Configuration;
using BpLogViewer.Cli.Data;
using BpLogViewer.Cli.Output;
using FilterSet = BpLogViewer.Cli.Filters.Filters;

namespace BpLogViewer.Cli.Commands;

/// <summary>
/// <c>bp-log-viewer session get</c> — Detailansicht einer einzelnen Session (F-003).
/// </summary>
public static class SessionGetCommand
{
    public static int Execute(string[] args, FilterSet filters)
    {
        if (!TryParseArgs(args, out var parsedQuery, out var parseError))
        {
            ErrorWriter.WriteErrorAsync("command_invalid", parseError!, 11).GetAwaiter().GetResult();
            return 11;
        }

        var query = parsedQuery!;
        var settings = ConfigurationLoader.Load(cliConnectionString: filters.ConnectionString);
        if (string.IsNullOrEmpty(settings.Database.ConnectionString))
        {
            ErrorWriter.WriteErrorAsync(
                "config_invalid",
                "appsettings.json nicht gefunden oder Database.ConnectionString leer. Siehe self-test für Diagnose.",
                3).GetAwaiter().GetResult();
            return 3;
        }

        var statusCache = new StatusCache();
        try
        {
            statusCache.Load(settings);
        }
        catch (Exception ex)
        {
            ErrorWriter.WriteErrorAsync("status_cache_load_failed", ex.Message, 5).GetAwaiter().GetResult();
            return 5;
        }

        try
        {
            using var db = new BpDbContext(settings);
            var repo = new SessionRepository(db);

            var details = query.SessionNumber.HasValue
                ? repo.GetBySessionNumber(query.SessionNumber.Value, query.IncludeStateXml)
                : repo.GetBySessionId(query.SessionId ?? Guid.Empty, query.IncludeStateXml);

            if (details is null)
            {
                var ident = query.SessionNumber?.ToString(System.Globalization.CultureInfo.InvariantCulture)
                    ?? query.SessionId!.Value.ToString("D");
                ErrorWriter.WriteWarningAsync("session_not_found", ident).GetAwaiter().GetResult();
                return 10;
            }

            details = details with { StatusName = statusCache.GetName(details.Status) };

            if (query.IncludeStateXml)
            {
                var startLen = details.StartParamsXml?.Length ?? 0;
                var stateLen = details.SessionStateXml?.Length ?? 0;
                ErrorWriter.WriteWarningAsync("state_xml_included", $"start_params_xml={startLen} chars, session_state_xml={stateLen} chars").GetAwaiter().GetResult();
            }

            if (details.Truncated)
            {
                ErrorWriter.WriteWarningAsync("output_truncated", $"work_queue_items limited to {SessionRepository.WorkQueueItemsLimit}").GetAwaiter().GetResult();
            }

            if (!string.IsNullOrWhiteSpace(filters.Output))
            {
                var rows = new[] { details };
                if (!OutputFileWriter.TryWriteJsonLines(rows, PayloadJsonContext.Default.SessionDetails, filters.Output, out var code, out var message))
                {
                    ErrorWriter.WriteErrorAsync(code ?? "output_write_failed", message ?? "failed to write output file", 6).GetAwaiter().GetResult();
                    return 6;
                }
                return 0;
            }

            var json = System.Text.Json.JsonSerializer.Serialize(details, PayloadJsonContext.Default.SessionDetails);
            Console.WriteLine(json);
            return 0;
        }
        catch (Exception ex)
        {
            ErrorWriter.WriteErrorAsync("db_error", ex.Message, 5).GetAwaiter().GetResult();
            return 5;
        }
    }

    private static bool TryParseArgs(string[] args, out SessionGetQuery? query, out string? error)
    {
        int? sessionNumber = null;
        Guid? sessionId = null;
        var includeStateXml = false;

        var optionsWithValue = new HashSet<string>(StringComparer.Ordinal)
        {
            "--since", "--until", "--process", "--resource", "--status", "--level", "--text",
            "--max-rows", "--format", "--output", "--out-file", "--connection-string", "--session-id"
        };

        for (var i = 0; i < args.Length; i++)
        {
            var token = args[i];

            if (token == "--include-state-xml")
            {
                includeStateXml = true;
                continue;
            }

            if (token == "--session-id")
            {
                if (i + 1 >= args.Length)
                {
                    query = default;
                    error = "--session-id requires a value";
                    return false;
                }

                var value = args[++i];
                if (!Guid.TryParse(value, out var parsedId))
                {
                    query = default;
                    error = $"invalid --session-id: '{value}'";
                    return false;
                }
                sessionId = parsedId;
                continue;
            }

            if (optionsWithValue.Contains(token))
            {
                i++; // global option value
                continue;
            }

            if (token.StartsWith("-", StringComparison.Ordinal))
            {
                continue; // globale Optionen wurden bereits in FilterParser gecheckt
            }

            if (sessionNumber.HasValue)
            {
                query = default;
                error = "multiple session numbers provided";
                return false;
            }

            if (!int.TryParse(token, out var parsedNumber) || parsedNumber <= 0)
            {
                query = default;
                error = $"invalid session number: '{token}'";
                return false;
            }

            sessionNumber = parsedNumber;
        }

        if (sessionNumber.HasValue && sessionId.HasValue)
        {
            query = default;
            error = "use either <session-number> OR --session-id, not both";
            return false;
        }

        if (!sessionNumber.HasValue && !sessionId.HasValue)
        {
            query = default;
            error = "usage: session get <session-number> OR session get --session-id <uuid>";
            return false;
        }

        query = new SessionGetQuery(sessionNumber, sessionId, includeStateXml);
        error = null;
        return true;
    }

    private sealed record SessionGetQuery(int? SessionNumber, Guid? SessionId, bool IncludeStateXml);
}
