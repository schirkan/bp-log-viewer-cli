namespace BpLogViewer.Cli.Models;

public sealed record AuditEventInfo(
    int EventId,
    DateTimeOffset EventAt,
    string? Code,
    string? Narrative,
    string? SrcUser,
    string? TgtUser,
    string? TgtProcess,
    string? TgtResource,
    string? CommentsPreview,
    string? OldXml,
    string? NewXml);
