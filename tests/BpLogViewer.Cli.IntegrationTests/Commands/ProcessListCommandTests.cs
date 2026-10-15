using BpLogViewer.Cli.Commands;
using BpLogViewer.Cli.Filters;
using Xunit;
using FilterSet = BpLogViewer.Cli.Filters.Filters;

namespace BpLogViewer.Cli.IntegrationTests.Commands;

public class ProcessListCommandTests : CommandSmokeTestBase
{
    public ProcessListCommandTests(BpLogViewerEnvironment env) : base(env) { }

    [Fact]
    public void Execute_NoArgs_Completes()
    {
        var (rc, _, stderr) = RunCommand(ProcessListCommand.Execute, Array.Empty<string>(), WithConnection(FilterSet.Empty));
        Assert.Equal(0, rc);
        Assert.DoesNotContain("db_error", stderr);
    }

    [Fact]
    public void Execute_WithType_Completes()
    {
        var (rc, _, stderr) = RunCommand(ProcessListCommand.Execute, new[] { "--type", "process" }, WithConnection(FilterSet.Empty));
        Assert.Equal(0, rc);
        Assert.DoesNotContain("db_error", stderr);
    }

    [Fact]
    public void Execute_InvalidType_ReturnsExit11()
    {
        var (rc, _, stderr) = RunCommand(ProcessListCommand.Execute, new[] { "--type", "wrong" }, WithConnection(FilterSet.Empty));
        Assert.Equal(11, rc);
        Assert.Contains("command_invalid", stderr);
    }

    [Fact]
    public void Execute_InvalidSinceDate_ReturnsExit11()
    {
        var (rc, _, stderr) = RunCommand(ProcessListCommand.Execute, new[] { "--has-errors-since", "not-a-date" }, WithConnection(FilterSet.Empty));
        Assert.Equal(11, rc);
        Assert.Contains("command_invalid", stderr);
    }
}
