using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using BpLogViewer.Cli.Commands;

namespace BpLogViewer.Cli.Output;

/// <summary>
/// Schreibt Datensätze als jsonl auf einen <see cref="Stream"/> (Default: <c>Console.OpenStandardOutput()</c>).
/// Spec: <c>specs/feature-F-013-output-format.md</c> § 6.1.
/// Trim-fest: nutzt <see cref="JsonTypeInfo{T}"/> aus dem Source-Generator-Context
/// <see cref="PayloadJsonContext"/>. Reflection wird nicht verwendet (Skill
/// <c>dotnet-trimmed-binary-json-fix</c>).
/// </summary>
public sealed class JsonlWriter : IAsyncDisposable
{
    private readonly Stream _stream;
    private readonly Utf8JsonWriter _jsonWriter;
    private bool _disposed;

    public JsonlWriter(Stream output)
    {
        _stream = output ?? throw new ArgumentNullException(nameof(output));
        _jsonWriter = new Utf8JsonWriter(_stream, new JsonWriterOptions { Indented = false });
    }

    public Task WriteLineAsync(VersionInfo value, CancellationToken ct = default)
        => WriteAsync(value, PayloadJsonContext.Default.VersionInfo, ct);

    public Task WriteLineAsync(ErrorEnvelope value, CancellationToken ct = default)
        => WriteAsync(value, PayloadJsonContext.Default.ErrorEnvelope, ct);

    public Task WriteLineAsync(WarningEnvelope value, CancellationToken ct = default)
        => WriteAsync(value, PayloadJsonContext.Default.WarningEnvelope, ct);

    private async Task WriteAsync<T>(T value, JsonTypeInfo<T> typeInfo, CancellationToken ct)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(typeInfo);
        // JsonSerializer.Serialize mit JsonTypeInfo<T> nutzt den Source-Generator
        // (Skill dotnet-trimmed-binary-json-fix). Direkter _jsonWriter.Write(typeInfo, value)
        // ist mehrdeutig zwischen Write<TValue>(JsonTypeInfo<TValue>, TValue) und
        // Write(JsonTypeInfo, object?) — Serialize ist eindeutig.
        JsonSerializer.Serialize(_jsonWriter, value, typeInfo);
        _jsonWriter.Flush();
        _stream.WriteByte((byte)'\n');
        await _stream.FlushAsync(ct);
    }

    public async Task FlushAsync(CancellationToken ct = default)
    {
        _jsonWriter.Flush();
        await _stream.FlushAsync(ct);
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;
        await FlushAsync();
        _jsonWriter.Dispose();
    }
}
