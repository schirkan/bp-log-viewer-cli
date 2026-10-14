namespace BpLogViewer.Cli.Filters;

/// <summary>
/// Aggregated global options + filters from CLI args and ENV. See
/// <c>specs/feature-F-011-common-filters.md</c> and <c>specs/architecture.md § 5</c>.
/// Werte sind nullable, wenn nicht gesetzt; Sub-Commands entscheiden selbst,
/// welche Filter sie anwenden.
/// </summary>
public sealed record Filters(
    DateTimeOffset? Since,
    DateTimeOffset? Until,
    string? Process,
    string? Resource,
    string[]? Status,
    bool HasException,
    string? Level,
    string? Text,
    int? MaxRows,
    bool Verbose,
    string? Format,
    string? Output,
    string? ConnectionString)
{
    /// <summary>Hard-Ceiling für <c>--max-rows</c> (architecture § 4.1 + § 8 Exit 8).</summary>
    public const int MaxRowsAbsoluteCeiling = 100_000;

    /// <summary>Default-Wert für <c>--max-rows</c> wenn nicht angegeben (architecture § 4.1).</summary>
    public const int DefaultMaxRows = 1_000;

    /// <summary>Default-Format (architecture § 4.1 + F-013).</summary>
    public const string DefaultFormat = "jsonl";

    public static Filters Empty { get; } = new(
        Since: null, Until: null, Process: null, Resource: null,
        Status: null, HasException: false, Level: null, Text: null,
        MaxRows: null, Verbose: false,
        Format: null, Output: null, ConnectionString: null);

    /// <summary>Aufgelöster <c>--max-rows</c>-Wert: Argument oder Default.</summary>
    public int EffectiveMaxRows => MaxRows ?? DefaultMaxRows;

    /// <summary>Aufgelöstes Format: Argument oder Default (jsonl).</summary>
    public string EffectiveFormat => Format ?? DefaultFormat;
}
