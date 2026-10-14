namespace BpLogViewer.Cli.Models;

/// <summary>
/// DTO für einen Datensatz aus <c>BPVSessionInfo</c> (F-002).
/// Property-Namen werden via Source-Generator in snake_case serialisiert.
/// </summary>
public sealed record SessionInfo(
    int SessionNumber,
    Guid SessionId,
    string? Process,
    Guid? ProcessId,
    string? Resource,
    Guid? ResourceId,
    int Status,
    string? StatusName,
    DateTimeOffset StartedAt,
    DateTimeOffset? EndedAt,
    int? DurationSec,
    string? LastStage,
    string? ExceptionType,
    string? ExceptionMessage);
