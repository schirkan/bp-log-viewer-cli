using System.Text.Json.Serialization;
using BpLogViewer.Cli.Output;

namespace BpLogViewer.Cli.Models;

/// <summary>
/// Eine Log-Zeile aus <c>BPASessionLog_NonUnicode</c> / <c>BPASessionLog_Unicode</c>.
/// </summary>
[JsonConverter(typeof(SessionLogEntryJsonConverter))]
public sealed record SessionLogEntry(
    long LogId,
    string SourceTable,
    string? StageName,
    int? StageType,
    int? ResultType,
    string? ResultTypeName,
    DateTimeOffset? StartedAt,
    DateTimeOffset? EndedAt,
    long? DurationMs,
    string? ResultPreview,
    long? AttributeSizeBytes);
