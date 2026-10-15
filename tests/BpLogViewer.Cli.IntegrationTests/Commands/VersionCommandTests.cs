using System.Text.Json;
using BpLogViewer.Cli.Commands;
using BpLogViewer.Cli.Output;
using Xunit;

namespace BpLogViewer.Cli.IntegrationTests.Commands;

public class VersionCommandTests
{
    [Fact]
    public void Execute_ReturnsZeroAndWritesJsonl()
    {
        var origOut = Console.Out;
        var sw = new StringWriter();
        Console.SetOut(sw);
        try
        {
            var rc = VersionCommand.Execute();
            Assert.Equal(0, rc);
            var output = sw.ToString().Trim();
            Assert.NotEmpty(output);

            using var doc = JsonDocument.Parse(output);
            var root = doc.RootElement;
            Assert.True(root.TryGetProperty("cli_version", out _));
            Assert.True(root.TryGetProperty("build_commit", out _));
            Assert.True(root.TryGetProperty("build_date", out _));
            Assert.True(root.TryGetProperty("target_framework", out _));
            Assert.Equal("net8.0", root.GetProperty("target_framework").GetString());
            Assert.True(root.TryGetProperty("runtime", out _));
        }
        finally
        {
            Console.SetOut(origOut);
        }
    }

    [Fact]
    public void VersionInfo_RoundtripsViaSourceGenerator()
    {
        var info = new VersionInfo("1.2.3", "abc1234", "2026-10-15", "net8.0", "win-x64");
        var json = JsonSerializer.Serialize(info, PayloadJsonContext.Default.VersionInfo);
        Assert.Contains("\"cli_version\":\"1.2.3\"", json);
        Assert.Contains("\"build_commit\":\"abc1234\"", json);
        Assert.Contains("\"build_date\":\"2026-10-15\"", json);
        Assert.Contains("\"target_framework\":\"net8.0\"", json);
        Assert.Contains("\"runtime\":\"win-x64\"", json);
    }
}
