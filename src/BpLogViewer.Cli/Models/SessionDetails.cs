namespace BpLogViewer.Cli.Models;

/// <summary>
/// Detail-Ansicht einer Session inklusive optionaler XML-Felder und Queue-Items (F-003).
/// </summary>
public sealed record SessionDetails(
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
    bool IsRunning,
    string? LastStage,
    string? ExceptionType,
    string? ExceptionMessage,
    int? QueueId,
    string? StartParamsXml,
    string? SessionStateXml,
    bool Truncated,
    IReadOnlyList<WorkQueueItemInfo> WorkQueueItems);
