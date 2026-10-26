using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using BpLogViewer.Cli.Configuration;

namespace BpLogViewer.Cli.Output;

/// <summary>
/// Schreibt jsonl-Listen atomar in eine Datei: zuerst in <c>&lt;name&gt;.tmp</c> schreiben,
/// dann mit <see cref="File.Move(..., overwrite:true)"/> ersetzen. Damit ist die
/// Datei für Konsumenten nie in einem halbfertigen Zustand sichtbar.
/// </summary>
public static class OutputFileWriter
{
    public static bool TryWriteJsonLines<T>(IReadOnlyList<T> rows, JsonTypeInfo<T> typeInfo, string outputArg, out string? errorCode, out string? errorMessage, string? headerJson = null)
    {
        if (!PathResolver.TryResolveOutputPath(outputArg, out var targetPath, out errorCode, out errorMessage))
        {
            return false;
        }

        try
        {
            var parent = Path.GetDirectoryName(targetPath);
            if (!string.IsNullOrWhiteSpace(parent))
            {
                Directory.CreateDirectory(parent);
            }

            // Atomar: erst in *.tmp, dann File.Move (overwrite) zur Ziel-Datei.
            // So sieht ein Konsument nie eine halbfertige Datei.
            var tempPath = targetPath + ".tmp";
            long bytes = 0;
            using (var fs = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.Read))
            using (var writer = new StreamWriter(fs, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)))
            {
                if (headerJson is not null)
                {
                    writer.WriteLine(headerJson);
                    bytes += Encoding.UTF8.GetByteCount(headerJson) + 1;
                }

                foreach (var row in rows)
                {
                    var json = JsonSerializer.Serialize(row, typeInfo);
                    writer.WriteLine(json);
                    // UTF-8 Bytes + 1 für Newline. Auf Windows = 2 (CRLF), auf Linux = 1 (LF).
                    // Wir nehmen +1 als Worst-Case; im Marker wird das nicht ausgewiesen.
                    bytes += Encoding.UTF8.GetByteCount(json) + 1;
                }
            }

            File.Move(tempPath, targetPath, overwrite: true);

            var marker = new OutputWrittenEnvelope(targetPath, rows.Count, bytes);
            Console.WriteLine(JsonSerializer.Serialize(marker, PayloadJsonContext.Readable.OutputWrittenEnvelope));
            errorCode = null;
            errorMessage = null;
            return true;
        }
        catch (Exception ex)
        {
            errorCode = "output_write_failed";
            errorMessage = ex.Message;
            return false;
        }
    }
}

public sealed record OutputWrittenEnvelope(string output_written, int row_count, long bytes);
