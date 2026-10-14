namespace BpLogViewer.Cli.Models;

public sealed record QueueItemInfo(
    Guid ItemId,
    long Ident,
    string? KeyValue,
    string? Status,
    int Attempt,
    DateTimeOffset? LoadedAt,
    DateTimeOffset? LockedAt,
    DateTimeOffset? CompletedAt,
    DateTimeOffset? ExceptionAt,
    string? ExceptionReason,
    string? ExceptionTag,
    int? Priority,
    int? WorkTimeSec,
    Guid? SessionId,
    DateTimeOffset? LastUpdated,
    string? DataPreview,
    string[]? Tags);
