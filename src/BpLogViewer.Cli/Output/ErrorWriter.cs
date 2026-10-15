using System.Text;
using System.Text.Json;

namespace BpLogViewer.Cli.Output;

/// <summary>
/// Schreibt Fehler und Warnungen als jsonl auf <c>Console.Error</c> —
/// immer, unabhängig vom <c>--format</c>.
/// </summary>
public static class ErrorWriter
{
    static ErrorWriter()
    {
        // UTF-8 für stdout + stderr, damit Umlaute und andere Nicht-ASCII-Zeichen
        // in Fehlermeldungen nicht korrumpiert werden. Auf Windows ist die
        // Default-Encoding oft cp1252, was bei fehlerhaften Werten aus der DB
        // zu kaputten Zeichen führt. Console.OutputEncoding setzt die Encoding
        // für BEIDE (stdout + stderr) gleichzeitig, weil sie auf Windows
        // denselben Console-Buffer nutzen.
        try
        {
            Console.InputEncoding = Encoding.UTF8;
            Console.OutputEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
        }
        catch
        {
            // Encoding-Setup ist best-effort; in restriktiven Umgebungen
            // (Sandbox) kann das fehlschlagen, ohne die Funktionalität zu
            // beeinträchtigen.
        }
    }

    /// <summary>Ein Fehler-Objekt + Exit-Code.</summary>
    public static async Task WriteErrorAsync(string code, string message, int exitCode)
    {
        var envelope = new ErrorEnvelope(code, message, exitCode);
        var json = JsonSerializer.Serialize(envelope, PayloadJsonContext.Readable.ErrorEnvelope);
        await Console.Error.WriteLineAsync(json);
    }

    /// <summary>Eine Warnung (eine pro Zeile).</summary>
    public static async Task WriteWarningAsync(string code, string detail)
    {
        var envelope = new WarningEnvelope(code, detail);
        var json = JsonSerializer.Serialize(envelope, PayloadJsonContext.Readable.WarningEnvelope);
        await Console.Error.WriteLineAsync(json);
    }
}

/// <summary>Fehler-Envelope für stderr. Felder snake_case via Source-Generator.</summary>
public record ErrorEnvelope(string code, string message, int exit_code);

/// <summary>Warning-Envelope für stderr.</summary>
public record WarningEnvelope(string warning, string detail);
