using System.Text.Json.Serialization;
using BpLogViewer.Cli.Commands;
using BpLogViewer.Cli.Models;

namespace BpLogViewer.Cli.Output;

/// <summary>
/// System.Text.Json Source-Generator Context (siehe Skill
/// <c>dotnet-trimmed-binary-json-fix</c>). Alle per jsonl ausgegebenen Typen
/// müssen hier mit <c>[JsonSerializable(typeof(T))]</c> registriert sein.
/// Naming-Policy: <c>SnakeCaseLower</c> (KI-konform, F-013 § 6.1).
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
public partial class PayloadJsonContext : JsonSerializerContext
{
}
