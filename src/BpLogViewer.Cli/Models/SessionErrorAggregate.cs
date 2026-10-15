namespace BpLogViewer.Cli.Models;

/// <summary>
/// Aggregierte Fehler-Sicht pro Exception-Typ.
/// </summary>
public sealed record SessionErrorAggregate(
    string ExceptionType,
    int Count,
    DateTimeOffset FirstSeen,
    DateTimeOffset LastSeen,
    IReadOnlyList<string> Processes,
    IReadOnlyList<string> Resources,
    IReadOnlyList<int> SampleSessionNumbers,
    string? SampleMessagePreview);
