using System.Text.Json;

namespace BpLogViewer.Cli.Output;

/// <summary>
/// Schreibt Fehler und Warnungen als jsonl auf <c>Console.Error</c> —
/// immer, unabhängig vom <c>--format</c> (F-013 § 6.3).
/// </summary>
public static class ErrorWriter
{
    /// <summary>Ein Fehler-Objekt + Exit-Code (architecture § 8).</summary>
    public static async Task WriteErrorAsync(string code, string message, int exitCode)
    {
        var envelope = new ErrorEnvelope(code, message, exitCode);
        var json = JsonSerializer.Serialize(envelope, PayloadJsonContext.Default.ErrorEnvelope);
        await Console.Error.WriteLineAsync(json);
    }

    /// <summary>Eine Warnung (eine pro Zeile, architecture § 6.3).</summary>
    public static async Task WriteWarningAsync(string code, string detail)
    {
        var envelope = new WarningEnvelope(code, detail);
        var json = JsonSerializer.Serialize(envelope, PayloadJsonContext.Default.WarningEnvelope);
        await Console.Error.WriteLineAsync(json);
    }
}

/// <summary>Fehler-Envelope für stderr. Felder snake_case via Source-Generator.</summary>
public record ErrorEnvelope(string code, string message, int exit_code);

/// <summary>Warning-Envelope für stderr.</summary>
public record WarningEnvelope(string warning, string detail);
