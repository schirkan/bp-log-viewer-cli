using System.Collections.Generic;
using System.Globalization;
using BpLogViewer.Cli.Output;

namespace BpLogViewer.Cli.Filters;

/// <summary>
/// Parses CLI args into a <see cref="Filters"/> object. Validates against
/// architecture § 4.4 (Timezone), § 5 (Global Options) and § 8 (Exit codes).
/// Throws <see cref="FilterValidationException"/> on invalid input.
/// </summary>
public static class FilterParser
{
    /// <summary>Globale Optionen mit Value (für Single-Pass-Parsing im Dispatcher).</summary>
    public static readonly IReadOnlySet<string> OptionsWithValue = new HashSet<string>(StringComparer.Ordinal)
    {
        "--since", "--until", "--process", "--resource", "--status", "--level", "--text",
        "--max-rows", "--format", "--output", "--out-file", "--connection-string"
    };

    /// <summary>Parst <paramref name="args"/> in <see cref="Filters"/>. Wirft <see cref="FilterValidationException"/>.</summary>
    public static Filters Parse(string[] args, Filters? defaults = null)
    {
        defaults ??= Filters.Empty;
        DateTimeOffset? since = defaults.Since;
        DateTimeOffset? until = defaults.Until;
        string? process = defaults.Process;
        string? resource = defaults.Resource;
        var status = defaults.Status is null ? null : new List<string>(defaults.Status);
        bool hasException = defaults.HasException;
        string? level = defaults.Level;
        string? text = defaults.Text;
        int? maxRows = defaults.MaxRows;
        bool verbose = defaults.Verbose;
        string? format = defaults.Format;
        string? output = defaults.Output;
        string? connectionString = defaults.ConnectionString;

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--since":
                    since = ParseIsoDate(RequireValue(args, ref i, "--since"));
                    continue;
                case "--until":
                    until = ParseIsoDate(RequireValue(args, ref i, "--until"));
                    continue;
                case "--process":
                    process = RequireValue(args, ref i, "--process");
                    continue;
                case "--resource":
                    resource = RequireValue(args, ref i, "--resource");
                    continue;
                case "--status":
                    (status ??= new List<string>()).Add(RequireValue(args, ref i, "--status"));
                    continue;
                case "--has-exception":
                    hasException = true;
                    continue;
                case "--level":
                    level = RequireValue(args, ref i, "--level");
                    if (level is not ("info" or "warn" or "error" or "debug"))
                    {
                        throw new FilterValidationException(
                            "filter_invalid",
                            $"--level: unknown value '{level}' (allowed: info|warn|error|debug)",
                            11);
                    }
                    continue;
                case "--text":
                    text = RequireValue(args, ref i, "--text");
                    continue;
                case "--max-rows":
                {
                    var maxRowsStr = RequireValue(args, ref i, "--max-rows");
                    if (!int.TryParse(maxRowsStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) || parsed < 0)
                    {
                        throw new FilterValidationException(
                            "filter_invalid",
                            $"--max-rows: invalid integer '{maxRowsStr}'",
                            11);
                    }

                    maxRows = parsed;
                    if (maxRows > Filters.MaxRowsAbsoluteCeiling)
                    {
                        throw new FilterValidationException(
                            "max_rows_exceeds_ceiling",
                            $"{maxRows} > {Filters.MaxRowsAbsoluteCeiling}",
                            8);
                    }
                    continue;
                }
                case "--verbose":
                    verbose = true;
                    continue;
                case "--format":
                    format = RequireValue(args, ref i, "--format");
                    if (format is not ("jsonl" or "table"))
                    {
                        throw new FilterValidationException(
                            "filter_invalid",
                            $"--format: unknown value '{format}' (allowed: jsonl|table)",
                            11);
                    }
                    continue;
                case "--output":
                case "--out-file":
                    output = RequireValue(args, ref i, args[i]);
                    continue;
                case "--connection-string":
                    connectionString = RequireValue(args, ref i, "--connection-string");
                    continue;
                default:
                    if (args[i].StartsWith("-", StringComparison.Ordinal))
                    {
                        // Unbekannte globale Option: Warning statt silent (fängt Tippfehler ab).
                        ErrorWriter.WriteWarningAsync(
                            "filter_unknown_option",
                            $"unrecognized global option '{args[i]}' (ignored; check spelling)").GetAwaiter().GetResult();
                        if (OptionsWithValue.Contains(args[i]))
                        {
                            i++;
                        }
                    }
                    continue;
            }
        }

        if (since.HasValue && until.HasValue && since.Value > until.Value)
        {
            throw new FilterValidationException(
                "filter_invalid",
                $"since ({since:O}) > until ({until:O})",
                11);
        }

        return new Filters(
            Since: since,
            Until: until,
            Process: process,
            Resource: resource,
            Status: status is null || status.Count == 0 ? null : status,
            HasException: hasException,
            Level: level,
            Text: text,
            MaxRows: maxRows,
            Verbose: verbose,
            Format: format,
            Output: output,
            ConnectionString: connectionString);
    }

    /// <summary>ISO-8601 mit/ohne Offset. Ohne Offset → UTC (architecture § 4.4).</summary>
    private static DateTimeOffset ParseIsoDate(string value)
    {
        if (DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var dto))
        {
            return dto.ToUniversalTime();
        }

        throw new FilterValidationException(
            "filter_invalid",
            $"invalid ISO-8601 datetime '{value}' (expected e.g. 2026-01-15T08:00:00Z)",
            11);
    }

    private static string RequireValue(string[] args, ref int i, string opt)
    {
        if (i + 1 >= args.Length)
        {
            throw new FilterValidationException(
                "filter_invalid",
                $"{opt} requires a value",
                11);
        }

        return args[++i];
    }
}
