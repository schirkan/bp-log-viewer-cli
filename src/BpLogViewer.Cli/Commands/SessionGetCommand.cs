using BpLogViewer.Cli.Configuration;
using BpLogViewer.Cli.Data;
using BpLogViewer.Cli.Filters;
using BpLogViewer.Cli.Output;
using FilterSet = BpLogViewer.Cli.Filters.Filters;

namespace BpLogViewer.Cli.Commands;

/// <summary>
/// <c>bp-log-viewer session get</c> - Detailansicht einer einzelnen Session.
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

        if (!CommandHost.TryLoadSettings(filters, out var settings))
        {
            return 3;
        }

        if (!CommandHost.TryLoadStatusCache(settings, out var statusCache))
        {
            return 5;
        }

        var query = parsedQuery!;
        try
        {
            using var db = new BpDbContext(settings);
            var repo = new SessionRepository(db);

            // ParseArgs garantiert genau eines von beiden gesetzt (SessionNumber ODER SessionId).
            var details = query.SessionNumber.HasValue
                ? repo.GetBySessionNumber(query.SessionNumber.Value, query.IncludeStateXml)
                : repo.GetBySessionId(query.SessionId!.Value, query.IncludeStateXml);

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

            return CommandHost.Emit(new[] { details }, PayloadJsonContext.Readable.SessionDetails, filters, wasTruncated: false);
        }
        catch (Exception ex)
        {
            return CommandHost.WriteDbError(ex);
        }
    }

    private static bool TryParseArgs(string[] args, out SessionGetQuery? query, out string? error)
    {
        int? sessionNumber = null;
        Guid? sessionId = null;

        var parsed = CommandArgs.Parse(args, "session", "get");
        if (parsed.Error is not null)
        {
            query = default;
            error = parsed.Error;
            return false;
        }

        var includeStateXml = parsed.HasFlag("--include-state-xml");

        var sessionIdValue = parsed.Value("--session-id");
        if (sessionIdValue is not null)
        {
            if (!Guid.TryParse(sessionIdValue, out var parsedId))
            {
                query = default;
                error = $"invalid --session-id: '{sessionIdValue}'";
                return false;
            }

            sessionId = parsedId;
        }

        foreach (var token in parsed.Positional)
        {
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
