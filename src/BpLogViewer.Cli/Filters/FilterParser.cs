using System.Globalization;

namespace BpLogViewer.Cli.Filters;

/// <summary>
/// Parst CLI-Args in ein <see cref="Filters"/>-Objekt. Validiert gegen
/// architecture § 4.4 (Timezone), § 4.5 (Status-Convention, form-Check),
/// § 5 (Global-Options) und § 8 (Exit-Codes). Wirft
/// <see cref="FilterValidationException"/> bei ungültigen Werten.
/// </summary>
/// <remarks>
/// <para>Filter-Semantik:</para>
/// <list type="bullet">
///   <item><c>--since</c> / <c>--until</c>: ISO-8601, mit oder ohne Offset.
///         Ohne Offset → UTC-Annahme (architecture § 4.4).</item>
///   <item><c>--max-rows</c>: Hard-Ceiling <see cref="Filters.MaxRowsAbsoluteCeiling"/>.</item>
///   <item><c>--status</c>: wiederholbar; Form-Check (Werte via StatusCache in F-001).</item>
///   <item><c>--level</c>: <c>info|warn|error|debug</c> (F-004 § resulttype-Mapping).</item>
///   <item><c>--format</c>: <c>jsonl|table</c> (F-013).</item>
/// </list>
/// <para>Sub-Command-spezifische Args (alles nach dem ersten Nicht-Option-Argument)
/// werden vom Aufrufer über <c>SplitArgs</c> abgetrennt und NICHT hier geparst.</para>
/// </remarks>
public static class FilterParser
{
    /// <summary>Parst <paramref name="args"/> in <see cref="Filters"/>. Wirft <see cref="FilterValidationException"/> bei Fehlern.</summary>
    public static Filters Parse(string[] args, Filters? defaults = null)
    {
        defaults ??= Filters.Empty;

        DateTimeOffset? since = defaults.Since;
        DateTimeOffset? until = defaults.Until;
        string? process = defaults.Process;
        string? resource = defaults.Resource;
        List<string> statusList = defaults.Status?.ToList() ?? new List<string>();
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
            string arg = args[i];
            switch (arg)
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
                    statusList.Add(RequireValue(args, ref i, "--status"));
                    break;
                case "--has-exception":
                    hasException = true;
                    break;
                case "--level":
                    level = RequireValue(args, ref i, "--level");
                    if (level is not ("info" or "warn" or "error" or "debug"))
                        throw new FilterValidationException(
                            "filter_invalid",
                            $"--level: unknown value '{level}' (allowed: info|warn|error|debug)",
                            11);
                    break;
                case "--text":
                    text = RequireValue(args, ref i, "--text");
                    break;
                case "--max-rows":
                    var maxRowsStr = RequireValue(args, ref i, "--max-rows");
                    if (!int.TryParse(maxRowsStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) || parsed < 0)
                        throw new FilterValidationException(
                            "filter_invalid",
                            $"--max-rows: invalid integer '{maxRowsStr}'",
                            11);
                    maxRows = parsed;
                    break;
                case "--verbose":
                    verbose = true;
                    break;
                case "--format":
                    format = RequireValue(args, ref i, "--format");
                    if (format is not ("jsonl" or "table"))
                        throw new FilterValidationException(
                            "filter_invalid",
                            $"--format: unknown value '{format}' (allowed: jsonl|table)",
                            11);
                    break;
                case "--output":
                case "--out-file":
                    output = RequireValue(args, ref i, "--output");
                    break;
                case "--connection-string":
                    connectionString = RequireValue(args, ref i, "--connection-string");
                    break;
                default:
                    // Unbekannte globale Option — überspringen (kann Sub-Command-Arg sein).
                    // Sub-Commands parsen ihre eigenen Optionen.
                    break;
            }
        }

        // Validierungen
        if (since.HasValue && until.HasValue && since.Value > until.Value)
            throw new FilterValidationException(
                "filter_invalid",
                $"since ({since:O}) > until ({until:O})",
                11);

        if (maxRows.HasValue && maxRows.Value > Filters.MaxRowsAbsoluteCeiling)
            throw new FilterValidationException(
                "max_rows_exceeds_ceiling",
                $"{maxRows.Value} > {Filters.MaxRowsAbsoluteCeiling}",
                8);

        return new Filters(
            Since: since, Until: until,
            Process: process, Resource: resource,
            Status: statusList.Count > 0 ? statusList.ToArray() : null,
            HasException: hasException, Level: level, Text: text,
            MaxRows: maxRows, Verbose: verbose,
            Format: format, Output: output, ConnectionString: connectionString);
    }

    /// <summary>ISO-8601 mit oder ohne Offset. Ohne Offset → UTC (architecture § 4.4).</summary>
    private static DateTimeOffset ParseIsoDate(string value)
    {
        if (DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var dto))
        {
            // Normalize to UTC für konsistente Vergleiche (DB speichert UTC).
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
            throw new FilterValidationException(
                "filter_invalid",
                $"{opt} requires a value",
                11);
        return args[++i];
    }
}
