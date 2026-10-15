using BpLogViewer.Cli.Commands;
using BpLogViewer.Cli.Filters;
using Xunit;
using FilterSet = BpLogViewer.Cli.Filters.Filters;

namespace BpLogViewer.Cli.IntegrationTests.Commands;

public class QueueListCommandTests : CommandSmokeTestBase
{
    public QueueListCommandTests(BpLogViewerEnvironment env) : base(env) { }

    [Fact]
    public void Execute_NoArgs_Completes()
    {
        var (rc, _, stderr) = RunCommand(QueueListCommand.Execute, Array.Empty<string>(), WithConnection(FilterSet.Empty));
        Assert.Equal(0, rc);
        Assert.DoesNotContain("db_error", stderr);
    }

    [Fact]
    public void Execute_OnlyRunning_Completes()
    {
        var (rc, _, stderr) = RunCommand(QueueListCommand.Execute, new[] { "--only-running" }, WithConnection(FilterSet.Empty));
        Assert.Equal(0, rc);
        Assert.DoesNotContain("db_error", stderr);
    }

    [Fact]
    public void Execute_OnlyExceptions_Completes()
    {
        var (rc, _, stderr) = RunCommand(QueueListCommand.Execute, new[] { "--only-exceptions" }, WithConnection(FilterSet.Empty));
        Assert.Equal(0, rc);
        Assert.DoesNotContain("db_error", stderr);
    }
}
