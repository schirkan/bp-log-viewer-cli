using System.Text.Json;
using BpLogViewer.Cli.Output;
using Xunit;

namespace BpLogViewer.Cli.IntegrationTests.Output;

public class PayloadJsonContextTests
{
    [Fact]
    public void ErrorEnvelope_SnakeCase_AndFieldsPresent()
    {
        var e = new ErrorEnvelope("c", "m", 7);
        var json = JsonSerializer.Serialize(e, PayloadJsonContext.Default.ErrorEnvelope);
        Assert.Contains("\"code\":\"c\"", json);
        Assert.Contains("\"message\":\"m\"", json);
        Assert.Contains("\"exit_code\":7", json);
    }

    [Fact]
    public void WarningEnvelope_SnakeCase()
    {
        var w = new WarningEnvelope("w", "d");
        var json = JsonSerializer.Serialize(w, PayloadJsonContext.Default.WarningEnvelope);
        Assert.Contains("\"warning\":\"w\"", json);
        Assert.Contains("\"detail\":\"d\"", json);
    }

    [Fact]
    public void VersionInfo_AllFieldsSnakeCase()
    {
        var v = new BpLogViewer.Cli.Commands.VersionInfo("1.0.0", "abc", "2026-10-15", "net8.0", "win-x64");
        var json = JsonSerializer.Serialize(v, PayloadJsonContext.Default.VersionInfo);
        Assert.Contains("\"cli_version\":\"1.0.0\"", json);
        Assert.Contains("\"build_commit\":\"abc\"", json);
        Assert.Contains("\"build_date\":\"2026-10-15\"", json);
        Assert.Contains("\"target_framework\":\"net8.0\"", json);
        Assert.Contains("\"runtime\":\"win-x64\"", json);
    }

    [Fact]
    public void OutputWrittenEnvelope_SnakeCase()
    {
        var o = new OutputWrittenEnvelope("path", 5, 100);
        var json = JsonSerializer.Serialize(o, PayloadJsonContext.Default.OutputWrittenEnvelope);
        Assert.Contains("\"output_written\":\"path\"", json);
        Assert.Contains("\"row_count\":5", json);
        Assert.Contains("\"bytes\":100", json);
    }

    [Fact]
    public void Context_IsExposed()
    {
        Assert.NotNull(PayloadJsonContext.Default);
    }
}
