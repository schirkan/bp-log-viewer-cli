using System.Globalization;

namespace BpLogViewer.Cli.Filters;

/// <summary>
/// Parses CLI args into a <see cref="Filters"/> object. Validates against
/// architecture § 4.4 (Timezone), § 5 (Global Options) and § 8 (Exit codes).
/// Throws <see cref="FilterValidationException"/> on invalid input.
/// </summary>
public static class FilterParser
{
    /// <summary>Parses <paramref name="args"/> into <see cref="Filters"/>. Throws on invalid input.</summary>
    public static Filters Parse(string[] args, Filters? defaults = null)
    {
        defaults ??= Filters.Empty;
        DateTimeOffset? since = defaults.Since;
        DateTimeOffset? until = defaults.Until;
        string? process = defaults.Process;
        string? resource = defaults.Resource;
        string[]? status = defaults.Status;
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
                    break;
                case "--until":
                    until = ParseIsoDate(RequireValue(args, ref i, "--until"));
                    break;
                case "--process":
                    process = RequireValue(args, ref i, "--process");
                    break;
                case "--resource":
                    resource = RequireValue(args, ref i, "--resource");
                    break;
                case "--status":
                    status = AppendStatus(status, RequireValue(args, ref i, "--status"));
                    break;
                case "--has-exception":
                    hasException = true;
                    break;
                case "--level":
                    level = RequireValue(args, ref i, "--level");
                    if (level is not ("info" or "warn" or "error" or "debug"))
                    {
                        throw new FilterValidationException(
                            "filter_invalid",
                            $"--level: unknown value '{level}' (allowed: info|warn|error|debug)",
                            11);
                    }
                    break;
                case "--text":
                    text = RequireValue(args, ref i, "--text");
                    break;
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
                    break;
                }
                case "--verbose":
                    verbose = true;
                    break;
                case "--format":
                    format = RequireValue(args, ref i, "--format");
                    if (format is not ("jsonl" or "table"))
                    {
                        throw new FilterValidationException(
                            "filter_invalid",
                            $"--format: unknown value '{format}' (allowed: jsonl|table)",
                            11);
                    }
                    break;
                case "--output":
                case "--out-file":
                    output = RequireValue(args, ref i, args[i]);
                    break;
                case "--connection-string":
                    connectionString = RequireValue(args, ref i, "--connection-string");
                    break;
                // Unknown args are ignored (may be command/subcommand tokens or future options)
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
            Status: status,
            HasException: hasException,
            Level: level,
            Text: text,
            MaxRows: maxRows,
            Verbose: verbose,
            Format: format,
            Output: output,
            ConnectionString: connectionString);
    }

    /// <summary>ISO-8601 with or without offset. Without offset → assume UTC (architecture § 4.4).</summary>
    private static DateTimeOffset ParseIsoDate(string value)
    {
        if (DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var dto))
        {
            return dto.ToUniversalTime();
        }

        throw new FilterValidationException(
            "filter_invalid",
            $"invalid ISO-8601 datetime '{value}'",
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

    private static string[] AppendStatus(string[]? existing, string value)
    {
        if (existing is null)
        {
            return new[] { value };
        }

        var arr = new string[existing.Length + 1];
        Array.Copy(existing, arr, existing.Length);
        arr[existing.Length] = value;
        return arr;
    }
}
