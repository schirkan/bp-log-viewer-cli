using System.Data;
using BpLogViewer.Cli.Configuration;
using BpLogViewer.Cli.Data;
using BpLogViewer.Cli.Output;
using Microsoft.Data.SqlClient;
using FilterSet = BpLogViewer.Cli.Filters.Filters;

namespace BpLogViewer.Cli.Commands;

/// <summary>
/// <c>bp-log-viewer session log</c> (F-004).
/// </summary>
public static class SessionLogCommand
{
    private static readonly IReadOnlyDictionary<int, string> FallbackResultTypes = new Dictionary<int, string>
    {
        [0] = "Information",
        [1] = "Warning",
        [2] = "Error",
        [3] = "Debug",
        [4] = "Exception"
    };

    public static int Execute(string[] args, FilterSet filters)
    {
        if (!TryParseArgs(args, out var parsed, out var parseError))
        {
            ErrorWriter.WriteErrorAsync("command_invalid", parseError!, 11).GetAwaiter().GetResult();
            return 11;
        }

        var query = parsed!;

        var settings = ConfigurationLoader.Load(cliConnectionString: filters.ConnectionString);
        if (string.IsNullOrEmpty(settings.Database.ConnectionString))
        {
            ErrorWriter.WriteErrorAsync("config_invalid", "appsettings.json nicht gefunden oder Database.ConnectionString leer. Siehe self-test für Diagnose.", 3).GetAwaiter().GetResult();
            return 3;
        }

        if (filters.Text is { Length: > 0 })
        {
            ErrorWriter.WriteWarningAsync("text_filter_perf_warning", "full table scan, consider narrowing filters").GetAwaiter().GetResult();
        }

        var levelMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            ["info"] = 0,
            ["warn"] = 1,
            ["error"] = 2,
            ["debug"] = 3
        };

        int? resultType = null;
        if (!string.IsNullOrWhiteSpace(filters.Level))
        {
            if (!levelMap.TryGetValue(filters.Level, out var mapped))
            {
                ErrorWriter.WriteErrorAsync("filter_invalid", $"unknown --level '{filters.Level}'", 11).GetAwaiter().GetResult();
                return 11;
            }
            resultType = mapped;
        }

        try
        {
            using var db = new BpDbContext(settings);
            var repo = new SessionLogRepository(db);

            var activeTable = repo.ResolveActiveLogTable();
            if (string.IsNullOrEmpty(activeTable))
            {
                ErrorWriter.WriteWarningAsync("no_log_entries", query.SessionNumber.ToString(System.Globalization.CultureInfo.InvariantCulture)).GetAwaiter().GetResult();
                return 0;
            }

            var resultTypeNames = LoadResultTypeNames(settings);

            var effectiveMax = (filters.Output is { Length: > 0 } && !filters.MaxRows.HasValue)
                ? FilterSet.MaxRowsAbsoluteCeiling
                : filters.EffectiveMaxRows;
            var rows = repo.List(
                tableName: activeTable,
                sessionNumber: query.SessionNumber,
                resultType: resultType,
                text: filters.Text,
                stage: query.Stage,
                fullResult: query.FullResult,
                maxRowsPlusOne: effectiveMax + 1,
                resultTypeNames: resultTypeNames);

            var truncated = rows.Count > effectiveMax;
            if (truncated)
            {
                rows.RemoveAt(rows.Count - 1);
                ErrorWriter.WriteWarningAsync("output_truncated", $"{effectiveMax} of {effectiveMax}+ rows. Use --output to write full result.").GetAwaiter().GetResult();
            }

            if (!string.IsNullOrWhiteSpace(filters.Output))
            {
                if (!OutputFileWriter.TryWriteJsonLines(rows, PayloadJsonContext.Default.SessionLogEntry, filters.Output, out var code, out var message))
                {
                    ErrorWriter.WriteErrorAsync(code ?? "output_write_failed", message ?? "failed to write output file", 6).GetAwaiter().GetResult();
                    return 6;
                }
                return truncated ? 2 : 0;
            }

            foreach (var row in rows)
            {
                Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(row, PayloadJsonContext.Default.SessionLogEntry));
            }

            return truncated ? 2 : 0;
        }
        catch (Exception ex)
        {
            ErrorWriter.WriteErrorAsync("db_error", ex.Message, 5).GetAwaiter().GetResult();
            return 5;
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
        catch
        {
            // Fallback bleibt aktiv.
        }

        return result;
    }

    private static bool TryParseArgs(string[] args, out SessionLogQuery? query, out string? error)
    {
        int? sessionNumber = null;
        string? stage = null;
        var fullResult = false;

        var optionsWithValue = new HashSet<string>(StringComparer.Ordinal)
        {
            "--since", "--until", "--process", "--resource", "--status", "--level", "--text",
            "--max-rows", "--format", "--output", "--out-file", "--connection-string", "--stage"
        };

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

            if (optionsWithValue.Contains(token))
            {
                i++; // global option value
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
