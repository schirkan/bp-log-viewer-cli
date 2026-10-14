namespace BpLogViewer.Cli.Models;

public sealed record ProcessInfo(
    Guid ProcessId,
    string Name,
    string? Type,
    string? Version,
    DateTimeOffset? LastModified,
    int? LastSessionNumber,
    DateTimeOffset? LastSessionStartedAt,
    int SessionCountTotal,
    int SessionCountFailedSince,
    double? AvgDurationSec);
