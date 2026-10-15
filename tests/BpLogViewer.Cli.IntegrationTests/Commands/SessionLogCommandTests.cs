using BpLogViewer.Cli.Commands;
using BpLogViewer.Cli.Filters;
using Xunit;
using FilterSet = BpLogViewer.Cli.Filters.Filters;

namespace BpLogViewer.Cli.IntegrationTests.Commands;

public class SessionLogCommandTests : CommandSmokeTestBase
{
    public SessionLogCommandTests(BpLogViewerEnvironment env) : base(env) { }

    [Fact]
    public void Execute_NonExistentSession_CompletesWithoutError()
    {
        // Non-existent session: either no_log_entries warning (no data) or
        // empty result (session exists but no log rows). Both are valid.
        var (rc, _, _) = RunCommand(SessionLogCommand.Execute, new[] { int.MaxValue.ToString() }, WithConnection(FilterSet.Empty));
        Assert.Equal(0, rc);
    }

    [Fact]
    public void Execute_NoArgs_ReturnsExit11()
    {
        var (rc, _, stderr) = RunCommand(SessionLogCommand.Execute, Array.Empty<string>(), WithConnection(FilterSet.Empty));
        Assert.Equal(11, rc);
        Assert.Contains("command_invalid", stderr);
    }

    [Fact]
    public void Execute_InvalidLevel_BlockedEarlier()
    {
        // --level is a global filter, validated in FilterParser
        var f = WithConnection(FilterSet.Empty) with { Level = "fatal" };
        Assert.Throws<BpLogViewer.Cli.Filters.FilterValidationException>(
            () => FilterParser.Parse(new[] { "--level", "fatal" }));
    }
}
