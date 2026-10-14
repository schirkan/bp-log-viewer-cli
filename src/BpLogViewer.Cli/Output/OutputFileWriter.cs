using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using BpLogViewer.Cli.Configuration;

namespace BpLogViewer.Cli.Output;

public static class OutputFileWriter
{
    public static bool TryWriteJsonLines<T>(IReadOnlyList<T> rows, JsonTypeInfo<T> typeInfo, string outputArg, out string? errorCode, out string? errorMessage)
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

            long bytes = 0;
            using (var fs = new FileStream(targetPath, FileMode.Create, FileAccess.Write, FileShare.Read))
            using (var writer = new StreamWriter(fs, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)))
            {
                foreach (var row in rows)
                {
                    var json = JsonSerializer.Serialize(row, typeInfo);
                    writer.WriteLine(json);
                    bytes += Encoding.UTF8.GetByteCount(json) + 1;
                }
            }

            var marker = new OutputWrittenEnvelope(targetPath, rows.Count, bytes);
            Console.WriteLine(JsonSerializer.Serialize(marker, PayloadJsonContext.Default.OutputWrittenEnvelope));
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
