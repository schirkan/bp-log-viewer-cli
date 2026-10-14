namespace BpLogViewer.Cli.Models;

public sealed record QueueInfo(
    Guid QueueId,
    string Name,
    string? KeyField,
    bool Running,
    int? MaxAttempts,
    Guid? ProcessId,
    int ItemCountTotal,
    int ItemCountPending,
    int ItemCountLocked,
    int ItemCountCompleted,
    int ItemCountException,
    int ItemCountDeferred,
    DateTimeOffset? OldestPendingLoadedAt,
    DateTimeOffset? NewestPendingLoadedAt,
    bool Stale);
