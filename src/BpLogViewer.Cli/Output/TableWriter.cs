using System.Globalization;
using System.Text;
using BpLogViewer.Cli.Commands;

namespace BpLogViewer.Cli.Output;

/// <summary>
/// Operator-taugliche Tabellen-Ausgabe (F-013 § 6.2).
/// Trim-fest (keine Reflection). Spaltenbreite via <c>StringInfo.LengthInTextElements</c>
/// für korrekte UTF-8-Breite (Em-Dash, CJK, Emoji).
/// </summary>
public sealed class TableWriter : IAsyncDisposable
{
    private readonly TextWriter _writer;
    private bool _disposed;

    public TableWriter(TextWriter writer)
    {
        _writer = writer ?? throw new ArgumentNullException(nameof(writer));
    }

    public async Task WriteLineAsync(VersionInfo value, CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        // Minimaler Tabellen-Stub für die erste Version. Volle Spaltenbreiten-Logik
        // kommt mit den ersten Datensatz-Typen (F-002/F-007). Siehe Spec F-013 § 6.2.
        var line = $"{"cli_version",-20} {value.CliVersion}\n"
                  + $"{"build_commit",-20} {value.BuildCommit}\n"
                  + $"{"build_date",-20} {value.BuildDate}\n"
                  + $"{"target_framework",-20} {value.TargetFramework}\n"
                  + $"{"runtime",-20} {value.Runtime}\n";
        await _writer.WriteAsync(line.AsMemory(), ct);
    }

    public Task FlushAsync(CancellationToken ct = default) => _writer.FlushAsync(ct);

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;
        await FlushAsync();
        _writer.Dispose();
    }
}
