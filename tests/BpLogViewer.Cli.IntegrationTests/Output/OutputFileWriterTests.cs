using System.Text.Json;
using BpLogViewer.Cli.Configuration;
using BpLogViewer.Cli.Models;
using BpLogViewer.Cli.Output;
using Xunit;

namespace BpLogViewer.Cli.IntegrationTests.Output;

public class OutputFileWriterTests : IDisposable
{
    private readonly string _tmpDir;

    public OutputFileWriterTests()
    {
        _tmpDir = Path.Combine(Path.GetTempPath(), $"bp-log-viewer-output-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tmpDir);
        Environment.SetEnvironmentVariable("BP_LOG_VIEWER_OUTPUT_ROOT", _tmpDir);
    }

    public void Dispose()
    {
        Environment.SetEnvironmentVariable("BP_LOG_VIEWER_OUTPUT_ROOT", null);
        if (Directory.Exists(_tmpDir))
        {
            Directory.Delete(_tmpDir, recursive: true);
        }
    }

    [Fact]
    public void TryWriteJsonLines_EmptyList_WritesMarker()
    {
        var origOut = Console.Out;
        var sw = new StringWriter();
        Console.SetOut(sw);
        try
        {
            var ok = OutputFileWriter.TryWriteJsonLines(
                Array.Empty<ProcessInfo>(),
                PayloadJsonContext.Default.ProcessInfo,
                "out.jsonl",
                out _, out _);
            Assert.True(ok);
            Assert.True(File.Exists(Path.Combine(_tmpDir, "out.jsonl")));
            Assert.Empty(File.ReadAllText(Path.Combine(_tmpDir, "out.jsonl")));

            var marker = sw.ToString().Trim();
            using var doc = JsonDocument.Parse(marker);
            var root = doc.RootElement;
            // output_written is the absolute path; just verify the file name is present.
            Assert.EndsWith("out.jsonl", root.GetProperty("output_written").GetString());
            Assert.Equal(0, root.GetProperty("row_count").GetInt32());
        }
        finally
        {
            Console.SetOut(origOut);
        }
    }

    [Fact]
    public void TryWriteJsonLines_NonEmpty_WritesRowsAndMarker()
    {
        var origOut = Console.Out;
        var sw = new StringWriter();
        Console.SetOut(sw);
        try
        {
            var rows = new List<ProcessInfo>
            {
                new(Guid.NewGuid(), "Alpha", "process", "1.0", null, null, null, 0, 0, null),
                new(Guid.NewGuid(), "Beta", "object", "2.0", null, null, null, 0, 0, null)
            };
            var ok = OutputFileWriter.TryWriteJsonLines(
                rows,
                PayloadJsonContext.Default.ProcessInfo,
                "result.jsonl",
                out _, out _);
            Assert.True(ok);

            var content = File.ReadAllText(Path.Combine(_tmpDir, "result.jsonl"));
            var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            Assert.Equal(2, lines.Length);
            Assert.Contains("\"name\":\"Alpha\"", lines[0]); // snake_case
            Assert.Contains("\"name\":\"Beta\"", lines[1]);

            var marker = sw.ToString().Trim();
            using var doc = JsonDocument.Parse(marker);
            Assert.Equal(2, doc.RootElement.GetProperty("row_count").GetInt32());
        }
        finally
        {
            Console.SetOut(origOut);
        }
    }

    [Fact]
    public void TryWriteJsonLines_NoRowsMarkerShape()
    {
        var origOut = Console.Out;
        var sw = new StringWriter();
        Console.SetOut(sw);
        try
        {
            OutputFileWriter.TryWriteJsonLines(
                Array.Empty<ProcessInfo>(),
                PayloadJsonContext.Default.ProcessInfo,
                "empty.jsonl",
                out _, out _);
            var marker = sw.ToString().Trim();
            using var doc = JsonDocument.Parse(marker);
            // Verify both fields exist
            Assert.True(doc.RootElement.TryGetProperty("output_written", out _));
            Assert.True(doc.RootElement.TryGetProperty("row_count", out _));
            Assert.True(doc.RootElement.TryGetProperty("bytes", out _));
        }
        finally
        {
            Console.SetOut(origOut);
        }
    }

    [Fact]
    public void TryWriteJsonLines_OverwriteExistingFile_Works()
    {
        // Pre-create target file
        var targetPath = Path.Combine(_tmpDir, "overwrite.jsonl");
        File.WriteAllText(targetPath, "{\"old\":\"data\"}");

        var rows = new List<ProcessInfo>
        {
            new(Guid.NewGuid(), "New", "process", "1.0", null, null, null, 0, 0, null)
        };
        var ok = OutputFileWriter.TryWriteJsonLines(
            rows,
            PayloadJsonContext.Default.ProcessInfo,
            "overwrite.jsonl",
            out _, out _);
        Assert.True(ok);

        var content = File.ReadAllText(targetPath);
        Assert.Contains("New", content);
        Assert.DoesNotContain("old", content);
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, false)]
    [InlineData(1, true)]
    public void SessionLogOutput_HeaderOnStdoutAndInFile_CountsOnlyDataRows(int count, bool truncated)
    {
        var rows = Enumerable.Range(0, count)
            .Select(i => new SessionLogEntry(i + 1, "internal", null, null, null, null,
                null, null, null, null, null)).ToArray();
        var origOut = Console.Out;
        var capture = new StringWriter();
        Console.SetOut(capture);
        try
        {
            var rc = BpLogViewer.Cli.Commands.CommandHost.Emit(rows,
                PayloadJsonContext.Readable.SessionLogEntry, BpLogViewer.Cli.Filters.Filters.Empty,
                truncated, SessionLogEntryJsonConverter.HeaderJson);
            Assert.Equal(truncated ? 2 : 0, rc);
            var stdout = capture.ToString();
            var lines = stdout.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            Assert.Equal(count + 1, lines.Length);
            Assert.Equal(SessionLogEntryJsonConverter.HeaderJson, lines[0].TrimEnd('\r'));
            if (count > 0)
                Assert.Equal("[1,null,null,null,null,null,null,null]", lines[1].TrimEnd('\r'));

            capture.GetStringBuilder().Clear();
            rc = BpLogViewer.Cli.Commands.CommandHost.Emit(rows,
                PayloadJsonContext.Readable.SessionLogEntry,
                BpLogViewer.Cli.Filters.Filters.Empty with { Output = "log.jsonl" },
                truncated, SessionLogEntryJsonConverter.HeaderJson);
            Assert.Equal(truncated ? 2 : 0, rc);
            Assert.Equal(stdout, File.ReadAllText(Path.Combine(_tmpDir, "log.jsonl")));
            Assert.False(File.Exists(Path.Combine(_tmpDir, "log.jsonl.tmp")));
            using var marker = JsonDocument.Parse(capture.ToString());
            Assert.Equal(count, marker.RootElement.GetProperty("row_count").GetInt32());
        }
        finally
        {
            Console.SetOut(origOut);
        }
    }

    [Fact]
    public void TryWriteJsonLines_Traversal_Blocked()
    {
        var ok = OutputFileWriter.TryWriteJsonLines(
            Array.Empty<ProcessInfo>(),
            PayloadJsonContext.Default.ProcessInfo,
            "..\\..\\escape.jsonl",
            out var code, out var msg);
        Assert.False(ok);
        Assert.Equal("output_path_outside_root", code);
        Assert.NotNull(msg);
    }

    [Fact]
    public void TryWriteJsonLines_AtomicWrite_NoTmpLeftover()
    {
        var rows = new List<ProcessInfo>
        {
            new(Guid.NewGuid(), "X", "process", "1.0", null, null, null, 0, 0, null)
        };
        var ok = OutputFileWriter.TryWriteJsonLines(
            rows, PayloadJsonContext.Default.ProcessInfo, "atomic.jsonl", out _, out _);
        Assert.True(ok);

        // Tmp file should not remain on disk after the atomic move.
        var tmpPath = Path.Combine(_tmpDir, "atomic.jsonl.tmp");
        Assert.False(File.Exists(tmpPath), "Tmp file leaked");
        Assert.True(File.Exists(Path.Combine(_tmpDir, "atomic.jsonl")));
    }
}
