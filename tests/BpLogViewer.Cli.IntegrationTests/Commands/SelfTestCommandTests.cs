using System.Text.Json;
using BpLogViewer.Cli.Commands;
using BpLogViewer.Cli.Filters;
using Xunit;
using FilterSet = BpLogViewer.Cli.Filters.Filters;

namespace BpLogViewer.Cli.IntegrationTests.Commands;

[Collection("BpDb")]
public class SelfTestCommandTests
{
    private readonly BpLogViewerEnvironment _env;

    public SelfTestCommandTests(BpLogViewerEnvironment env) => _env = env;

    [Fact]
    public void Execute_WritesMultipleJsonlSteps()
    {
        var origOut = Console.Out;
        var origErr = Console.Error;
        var swOut = new StringWriter();
        var swErr = new StringWriter();
        Console.SetOut(swOut);
        Console.SetError(swErr);
        try
        {
            var rc = SelfTestCommand.Execute(FilterSet.Empty, baseDir: null);
            // With a working DB, all steps should be ok; exit 0.
            // Without a working DB, the test fixture's RequireDatabase would
            // have failed before reaching here.
            Assert.Equal(0, rc);

            var output = swOut.ToString();
            var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            Assert.True(lines.Length >= 8, $"Expected at least 8 jsonl lines, got {lines.Length}");

            // Each line should be valid jsonl with a "step" or final marker.
            foreach (var line in lines)
            {
                using var doc = JsonDocument.Parse(line.Trim());
                var root = doc.RootElement;
                var hasStep = root.TryGetProperty("step", out _);
                var hasSelfTest = root.TryGetProperty("self_test", out _);
                Assert.True(hasStep || hasSelfTest, $"Line has no step/self_test: {line}");
            }
        }
        finally
        {
            Console.SetOut(origOut);
            Console.SetError(origErr);
        }
    }

    [Fact]
    public void Execute_ReportsDbConnectStep()
    {
        var origOut = Console.Out;
        var origErr = Console.Error;
        var swOut = new StringWriter();
        var swErr = new StringWriter();
        Console.SetOut(swOut);
        Console.SetError(swErr);
        try
        {
            SelfTestCommand.Execute(FilterSet.Empty, baseDir: null);
            var output = swOut.ToString();
            Assert.Contains("\"step\":\"db_connect\"", output);
            Assert.Contains("\"status\":\"ok\"", output);
        }
        finally
        {
            Console.SetOut(origOut);
            Console.SetError(origErr);
        }
    }

    [Fact]
    public void Execute_ReportsBpVersion()
    {
        var origOut = Console.Out;
        var origErr = Console.Error;
        var swOut = new StringWriter();
        var swErr = new StringWriter();
        Console.SetOut(swOut);
        Console.SetError(swErr);
        try
        {
            SelfTestCommand.Execute(FilterSet.Empty, baseDir: null);
            var output = swOut.ToString();
            Assert.Contains("\"step\":\"bp_version\"", output);
        }
        finally
        {
            Console.SetOut(origOut);
            Console.SetError(origErr);
        }
    }

    [Fact]
    public void Execute_ReportsAllCoreTables()
    {
        var origOut = Console.Out;
        var origErr = Console.Error;
        var swOut = new StringWriter();
        var swErr = new StringWriter();
        Console.SetOut(swOut);
        Console.SetError(swErr);
        try
        {
            SelfTestCommand.Execute(FilterSet.Empty, baseDir: null);
            var output = swOut.ToString();
            Assert.Contains("\"step\":\"tables\"", output);
            Assert.Contains("BPASession", output);
            Assert.Contains("BPAWorkQueue", output);
        }
        finally
        {
            Console.SetOut(origOut);
            Console.SetError(origErr);
        }
    }
}
