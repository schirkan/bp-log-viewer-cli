using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Unicode;
using BpLogViewer.Cli.Commands;
using BpLogViewer.Cli.Models;

namespace BpLogViewer.Cli.Output;

/// <summary>
/// System.Text.Json Source-Generator Context. Alle per jsonl ausgegebenen Typen
/// müssen hier mit <c>[JsonSerializable(typeof(T))]</c> registriert sein.
/// Naming-Policy: <c>SnakeCaseLower</c>.
/// </summary>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower,
    DictionaryKeyPolicy = JsonKnownNamingPolicy.SnakeCaseLower,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    WriteIndented = false)]
[JsonSerializable(typeof(ErrorEnvelope))]
[JsonSerializable(typeof(WarningEnvelope))]
[JsonSerializable(typeof(VersionInfo))]
[JsonSerializable(typeof(SessionInfo))]
[JsonSerializable(typeof(SessionDetails))]
[JsonSerializable(typeof(WorkQueueItemInfo))]
[JsonSerializable(typeof(SessionLogEntry))]
[JsonSerializable(typeof(SessionErrorAggregate))]
[JsonSerializable(typeof(ResourceInfo))]
[JsonSerializable(typeof(ProcessInfo))]
[JsonSerializable(typeof(QueueInfo))]
[JsonSerializable(typeof(QueueItemInfo))]
[JsonSerializable(typeof(AuditEventInfo))]
[JsonSerializable(typeof(OutputWrittenEnvelope))]
public partial class PayloadJsonContext : JsonSerializerContext
{
    private static readonly Lazy<PayloadJsonContext> ReadableContext = new(CreateReadableContext);

    public static PayloadJsonContext Readable => ReadableContext.Value;

    private static PayloadJsonContext CreateReadableContext() => new(new JsonSerializerOptions(Default.Options)
    {
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
    });
}
