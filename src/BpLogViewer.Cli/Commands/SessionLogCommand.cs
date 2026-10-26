using System.Data;
using BpLogViewer.Cli.Configuration;
using BpLogViewer.Cli.Data;
using BpLogViewer.Cli.Filters;
using BpLogViewer.Cli.Output;
using Microsoft.Data.SqlClient;
using FilterSet = BpLogViewer.Cli.Filters.Filters;

namespace BpLogViewer.Cli.Commands;

/// <summary>
/// <c>bp-log-viewer session log</c>.
/// </summary>
public static class SessionLogCommand
{
    private static readonly IReadOnlyDictionary<int, string> FallbackResultTypes = new Dictionary<int, string>
    {
        [0] = "Information",
        [1] = "Warning",
        [2] = "Error",
        [3] = "Debug",
        [4] = "Exception",
        // BP 7.5 NonUnicode-Log auf diesem System nutzt 64 für Error-Zeilen.
        [64] = "Error"
    };

    public static int Execute(string[] args, FilterSet filters)
    {
        if (!TryParseArgs(args, out var parsed, out var parseError))
        {
            ErrorWriter.WriteErrorAsync("command_invalid", parseError!, 11).GetAwaiter().GetResult();
            return 11;
        }

        var query = parsed!;

        if (!CommandHost.TryLoadSettings(filters, out var settings))
        {
            return 3;
        }

        if (filters.Text is { Length: > 0 })
        {
            ErrorWriter.WriteWarningAsync("text_filter_perf_warning", "full table scan, consider narrowing filters").GetAwaiter().GetResult();
        }

        var levelMap = new Dictionary<string, int[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["info"] = new[] { 0 },
            ["warn"] = new[] { 1 },
            ["error"] = new[] { 2, 64 },
            ["debug"] = new[] { 3 }
        };

        int[]? resultTypes = null;
        if (!string.IsNullOrWhiteSpace(filters.Level))
        {
            if (!levelMap.TryGetValue(filters.Level, out var mapped))
            {
                ErrorWriter.WriteErrorAsync("filter_invalid", $"unknown --level '{filters.Level}'", 11).GetAwaiter().GetResult();
                return 11;
            }
            resultTypes = mapped;
        }

        try
        {
            using var db = new BpDbContext(settings);
            var repo = new SessionLogRepository(db);

            var activeTable = repo.ResolveActiveLogTable();
            if (string.IsNullOrEmpty(activeTable))
            {
                ErrorWriter.WriteWarningAsync("no_log_entries", query.SessionNumber.ToString(System.Globalization.CultureInfo.InvariantCulture)).GetAwaiter().GetResult();
                return CommandHost.Emit(Array.Empty<BpLogViewer.Cli.Models.SessionLogEntry>(),
                    PayloadJsonContext.Readable.SessionLogEntry, filters, wasTruncated: false,
                    headerJson: SessionLogEntryJsonConverter.HeaderJson);
            }

            var resultTypeNames = LoadResultTypeNames(settings);

            var effectiveMax = CommandHost.ResolveEffectiveMax(filters);
            var rows = repo.List(
                tableName: activeTable,
                sessionNumber: query.SessionNumber,
                resultTypes: resultTypes,
                text: filters.Text,
                stage: query.Stage,
                fullResult: query.FullResult,
                maxRowsPlusOne: effectiveMax + 1,
                resultTypeNames: resultTypeNames);

            var truncated = CommandHost.Truncate(rows, effectiveMax);
            return CommandHost.Emit(rows, PayloadJsonContext.Readable.SessionLogEntry, filters, truncated,
                headerJson: SessionLogEntryJsonConverter.HeaderJson);
        }
        catch (Exception ex)
        {
            return CommandHost.WriteDbError(ex);
        }
    }

    private static IReadOnlyDictionary<int, string> LoadResultTypeNames(AppSettings settings)
    {
        var result = new Dictionary<int, string>(FallbackResultTypes);

        try
        {
            using var conn = new SqlConnection(settings.Database.ConnectionString);
            conn.Open();

            // Optionale Runtime-Quelle (wenn vorhanden) überschreibt Fallback.
            const string tableProbe = "SELECT COUNT(1) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'BPALogResultType'";
            using var probe = new SqlCommand(tableProbe, conn);
            var exists = Convert.ToInt32(probe.ExecuteScalar() ?? 0, System.Globalization.CultureInfo.InvariantCulture) > 0;
            if (!exists) return result;

            using var cmd = new SqlCommand("SELECT resulttype, name FROM BPALogResultType", conn);
            using var reader = cmd.ExecuteReader(CommandBehavior.SequentialAccess);
            while (reader.Read())
            {
                if (reader.IsDBNull(0) || reader.IsDBNull(1)) continue;
                result[reader.GetInt32(0)] = reader.GetString(1);
            }
        }
        catch (Exception ex)
        {
            // Fallback bleibt aktiv, aber wir warnen, damit der Konsument
            // den Diagnose-Pfad nachvollziehen kann.
            ErrorWriter.WriteWarningAsync("result_type_table_unavailable", ex.Message).GetAwaiter().GetResult();
        }

        return result;
    }

    private static bool TryParseArgs(string[] args, out SessionLogQuery? query, out string? error)
    {
        int? sessionNumber = null;
        string? stage = null;
        var fullResult = false;

        for (var i = 0; i < args.Length; i++)
        {
            var token = args[i];

            if (token == "--full-result")
            {
                fullResult = true;
                continue;
            }

            if (token == "--stage")
            {
                if (i + 1 >= args.Length)
                {
                    query = null;
                    error = "--stage requires a value";
                    return false;
                }

                stage = args[++i];
                continue;
            }

            if (FilterParser.OptionsWithValue.Contains(token))
            {
                i++; // globaler Option-Wert
                continue;
            }

            if (token.StartsWith("-", StringComparison.Ordinal))
            {
                continue;
            }

            if (sessionNumber.HasValue)
            {
                query = null;
                error = "multiple session numbers provided";
                return false;
            }

            if (!int.TryParse(token, out var parsed) || parsed <= 0)
            {
                query = null;
                error = $"invalid session number: '{token}'";
                return false;
            }

            sessionNumber = parsed;
        }

        if (!sessionNumber.HasValue)
        {
            query = null;
            error = "usage: session log <session-number> [--stage <name>] [--full-result]";
            return false;
        }

        query = new SessionLogQuery(sessionNumber.Value, stage, fullResult);
        error = null;
        return true;
    }

    private sealed record SessionLogQuery(int SessionNumber, string? Stage, bool FullResult);
}
