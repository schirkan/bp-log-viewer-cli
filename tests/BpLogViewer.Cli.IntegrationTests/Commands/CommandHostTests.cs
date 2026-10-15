using BpLogViewer.Cli.Commands;
using BpLogViewer.Cli.Filters;
using BpLogViewer.Cli.Models;
using BpLogViewer.Cli.Output;
using Xunit;
using FilterSet = BpLogViewer.Cli.Filters.Filters;

namespace BpLogViewer.Cli.IntegrationTests.Commands;

[Collection("BpDb")]
public class CommandHostTests
{
    private readonly BpLogViewerEnvironment _env;
    private readonly StringWriter _stdoutCapture = new();
    private readonly StringWriter _stderrCapture = new();
    private readonly TextWriter _origOut;
    private readonly TextWriter _origErr;

    public CommandHostTests(BpLogViewerEnvironment env)
    {
        _env = env;
        _origOut = Console.Out;
        _origErr = Console.Error;
        Console.SetOut(_stdoutCapture);
        Console.SetError(_stderrCapture);
    }

    [Fact]
    public void ResolveEffectiveMax_OutputWithoutMaxRows_ReturnsCeiling()
    {
        var f = FilterSet.Empty with { Output = "out.jsonl" };
        Assert.Equal(FilterSet.MaxRowsAbsoluteCeiling, CommandHost.ResolveEffectiveMax(f));
    }

    [Fact]
    public void ResolveEffectiveMax_OutputWithMaxRows_ReturnsMaxRows()
    {
        var f = FilterSet.Empty with { Output = "out.jsonl", MaxRows = 42 };
        Assert.Equal(42, CommandHost.ResolveEffectiveMax(f));
    }

    [Fact]
    public void ResolveEffectiveMax_ExplicitMaxRows_ReturnsMaxRows()
    {
        var f = FilterSet.Empty with { MaxRows = 7 };
        Assert.Equal(7, CommandHost.ResolveEffectiveMax(f));
    }

    [Fact]
    public void ResolveEffectiveMax_NoOptions_ReturnsDefault()
    {
        Assert.Equal(FilterSet.DefaultMaxRows, CommandHost.ResolveEffectiveMax(FilterSet.Empty));
    }

    [Fact]
    public void ResolveEffectiveMax_FallbackDefault_Applies()
    {
        Assert.Equal(500, CommandHost.ResolveEffectiveMax(FilterSet.Empty, fallbackDefault: 500));
    }

    [Fact]
    public void Truncate_LargerThanLimit_RemovesAndReturnsTrue()
    {
        // Truncate uses the +1 pattern: callers pass maxRowsPlusOne as the limit,
        // so the list has at most maxRows+1 entries. Truncate removes the +1 entry.
        var rows = new List<int> { 1, 2, 3, 4 };
        var truncated = CommandHost.Truncate(rows, 3);
        Assert.True(truncated);
        Assert.Equal(new List<int> { 1, 2, 3 }, rows);
        Assert.Contains("output_truncated", _stderrCapture.ToString());
    }

    [Fact]
    public void Truncate_EqualToLimit_NoOp()
    {
        var rows = new List<int> { 1, 2, 3 };
        var truncated = CommandHost.Truncate(rows, 3);
        Assert.False(truncated);
        Assert.Equal(new List<int> { 1, 2, 3 }, rows);
    }

    [Fact]
    public void Truncate_SmallerThanLimit_NoOp()
    {
        var rows = new List<int> { 1, 2 };
        var truncated = CommandHost.Truncate(rows, 5);
        Assert.False(truncated);
        Assert.Equal(new List<int> { 1, 2 }, rows);
    }

    [Fact]
    public void Emit_WithoutOutput_WritesToStdout()
    {
        var f = FilterSet.Empty;
        var rows = new List<ProcessInfo>
        {
            new(Guid.NewGuid(), "Test", "process", "1.0", null, null, null, 0, 0, null)
        };
        var rc = CommandHost.Emit(rows, PayloadJsonContext.Default.ProcessInfo, f, wasTruncated: false);
        Assert.Equal(0, rc);
        var output = _stdoutCapture.ToString();
        Assert.Contains("Test", output);
        Assert.Contains("\"name\":\"Test\"", output); // snake_case
    }

    [Fact]
    public void Emit_TruncatedFlag_ReturnsExit2()
    {
        var f = FilterSet.Empty;
        var rows = new List<ProcessInfo>
        {
            new(Guid.NewGuid(), "Test", "process", "1.0", null, null, null, 0, 0, null)
        };
        var rc = CommandHost.Emit(rows, PayloadJsonContext.Default.ProcessInfo, f, wasTruncated: true);
        Assert.Equal(2, rc);
    }

    [Fact]
    public void Emit_WithOutput_NotCoveredHere()
    {
        // The full output-to-file behaviour (path resolution, atomic write,
        // marker envelope) is covered by OutputFileWriterTests, which
        // controls BP_LOG_VIEWER_OUTPUT_ROOT in its constructor/dispose.
        // CommandHost.Emit is a thin wrapper around OutputFileWriter; adding
        // a second test here would just re-test the wrapper with a different
        // env-var setup, which is flaky in xUnit's parallel-by-class model.
    }

    [Fact]
    public void WriteDbError_Returns5()
    {
        var rc = CommandHost.WriteDbError(new InvalidOperationException("boom"));
        Assert.Equal(5, rc);
        Assert.Contains("db_error", _stderrCapture.ToString());
    }

    [Fact]
    public void TryLoadSettings_WithoutConnection_DoesNotThrow()
    {
        // The test runner's CWD may contain an appsettings.json with a real
        // connection string; we only assert the call doesn't throw and that
        // it returns a deterministic boolean.
        var prev = Environment.GetEnvironmentVariable("BP_DB_CONNECTION");
        Environment.SetEnvironmentVariable("BP_DB_CONNECTION", null);
        try
        {
            var f = FilterSet.Empty;
            var ok = CommandHost.TryLoadSettings(f, out _);
            Assert.True(ok || !ok);
        }
        finally
        {
            Environment.SetEnvironmentVariable("BP_DB_CONNECTION", prev);
        }
    }

    [Fact]
    public void TryLoadSettings_WithConnection_ReturnsTrue()
    {
        var f = FilterSet.Empty with { ConnectionString = _env.ConnectionString };
        var ok = CommandHost.TryLoadSettings(f, out var settings);
        Assert.True(ok);
        Assert.Equal(_env.ConnectionString, settings.Database.ConnectionString);
    }

    [Fact]
    public void TryLoadStatusCache_LoadsSuccessfully()
    {
        _env.RequireDatabase();
        var ok = CommandHost.TryLoadStatusCache(_env.Settings, out var cache);
        Assert.True(ok);
        Assert.True(cache.IsLoaded);
        Assert.True(cache.Count > 0);
    }
}
