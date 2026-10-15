namespace BpLogViewer.Cli.Models;

/// <summary>
/// Verknüpftes Queue-Item einer Session.
/// </summary>
public sealed record WorkQueueItemInfo(
    string? KeyValue,
    string? Status,
    int Attempt,
    string? ExceptionReason,
    string? ExceptionReasonTag,
    DateTimeOffset? LoadedAt,
    DateTimeOffset? FinishedAt);
