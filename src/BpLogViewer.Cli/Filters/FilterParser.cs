using System.Collections.Generic;
using System.Globalization;
using BpLogViewer.Cli.Configuration;
using BpLogViewer.Cli.Output;

namespace BpLogViewer.Cli.Filters;

/// <summary>
/// Parses CLI args into a <see cref="Filters"/> object. Validates ISO-8601
/// timestamps, integer limits and option value enums. Throws
/// <see cref="FilterValidationException"/> on invalid input.
/// </summary>
public static class FilterParser
{
    /// <summary>
    /// Globale Optionen mit Value (für Single-Pass-Parsing im Dispatcher).
    /// Quelle der Wahrheit ist <see cref="OptionRegistry"/>.
    /// </summary>
    public static IReadOnlySet<string> OptionsWithValue => OptionRegistry.GlobalOptionsWithValue;

    /// <summary>Parst <paramref name="args"/> in <see cref="Filters"/>. Wirft <see cref="FilterValidationException"/>.</summary>
    /// <param name="args">Vollständige Argumentliste.</param>
    /// <param name="defaults">Vorbelegte Filterwerte.</param>
    /// <param name="commandOptions">
    /// Command-spezifische Optionen des aktiven Commands inkl. Arity. Deren Werte
    /// dürfen nicht als globale Filter gelesen werden.
    /// </param>
    /// <param name="commandOptionsStartIndex">
    /// Index in <paramref name="args"/>, ab dem Command-Optionen erkannt werden.
    /// Liefert <see cref="Program.SplitArgs"/> mit, damit die Kopplung eindeutig bleibt.
    /// </param>
    public static Filters Parse(string[] args, Filters? defaults = null,
        IReadOnlyDictionary<string, OptionArity>? commandOptions = null, int commandOptionsStartIndex = 0)
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
            if (i >= commandOptionsStartIndex && commandOptions is not null &&
                commandOptions.TryGetValue(args[i], out var commandArity))
            {
                // Command-Optionen und ihre Werte gehören nicht zu den globalen Filtern.
                if (commandArity == OptionArity.Value)
                    _ = RequireValue(args, ref i, args[i]);
                continue;
            }

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

    /// <summary>ISO-8601 mit/ohne Offset. Ohne Offset \x1a UTC.</summary>
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
