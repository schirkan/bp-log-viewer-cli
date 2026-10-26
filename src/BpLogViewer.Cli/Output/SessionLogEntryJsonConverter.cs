using System.Text.Json;
using System.Text.Json.Serialization;
using BpLogViewer.Cli.Models;

namespace BpLogViewer.Cli.Output;

/// <summary>
/// Writes session log entries as compact JSONL arrays: one header line with the
/// column names, then one array per entry in the same column order.
/// </summary>
public sealed class SessionLogEntryJsonConverter : JsonConverter<SessionLogEntry>
{
    /// <summary>
    /// Column names in output order. Header line and array positions belong together,
    /// so both are derived from this single list.
    /// </summary>
    public static readonly IReadOnlyList<string> ColumnNames = new[]
    {
        "log_id",
        "started_at",
        "stage_name",
        "stage_type",
        "result_type",
        "result_type_name",
        "result_preview",
        "attribute_size_bytes",
    };

    /// <summary>First line of the jsonl output: the column names as JSON array.</summary>
    public static readonly string HeaderJson =
        "[" + string.Join(",", ColumnNames.Select(name => "\"" + name + "\"")) + "]";

    public override SessionLogEntry Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => throw new JsonException("Session log entries are output-only.");

    public override void Write(Utf8JsonWriter writer, SessionLogEntry value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        writer.WriteNumberValue(value.LogId);
        if (value.StartedAt.HasValue) writer.WriteStringValue(value.StartedAt.Value);
        else writer.WriteNullValue();
        writer.WriteStringValue(value.StageName);
        if (value.StageType.HasValue) writer.WriteNumberValue(value.StageType.Value);
        else writer.WriteNullValue();
        if (value.ResultType.HasValue) writer.WriteNumberValue(value.ResultType.Value);
        else writer.WriteNullValue();
        writer.WriteStringValue(value.ResultTypeName);
        writer.WriteStringValue(value.ResultPreview);
        if (value.AttributeSizeBytes.HasValue) writer.WriteNumberValue(value.AttributeSizeBytes.Value);
        else writer.WriteNullValue();
        writer.WriteEndArray();
    }
}
