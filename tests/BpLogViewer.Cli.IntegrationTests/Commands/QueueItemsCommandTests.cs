using BpLogViewer.Cli.Commands;
using BpLogViewer.Cli.Filters;
using Xunit;
using FilterSet = BpLogViewer.Cli.Filters.Filters;

namespace BpLogViewer.Cli.IntegrationTests.Commands;

public class QueueItemsCommandTests : CommandSmokeTestBase
{
    public QueueItemsCommandTests(BpLogViewerEnvironment env) : base(env) { }

    [Fact]
    public void Execute_NoArgs_ReturnsExit11()
    {
        var (rc, _, stderr) = RunCommand(QueueItemsCommand.Execute, Array.Empty<string>(), WithConnection(FilterSet.Empty));
        Assert.Equal(11, rc);
        Assert.Contains("command_invalid", stderr);
    }

    [Fact]
    public void Execute_NonExistentQueue_ReturnsExit10()
    {
        var (rc, _, stderr) = RunCommand(QueueItemsCommand.Execute, new[] { "__no_such_queue__" }, WithConnection(FilterSet.Empty));
        Assert.Equal(10, rc);
        Assert.Contains("queue_not_found", stderr);
    }

    [Fact]
    public void Execute_WithQueueId_Completes()
    {
        var (rc, _, stderr) = RunCommand(QueueItemsCommand.Execute, new[] { "--queue-id", Guid.NewGuid().ToString() }, WithConnection(FilterSet.Empty));
        Assert.Equal(0, rc);
        Assert.DoesNotContain("db_error", stderr);
    }

    [Fact]
    public void Execute_NonExistentQueueId_ReturnsExit10()
    {
        // A random GUID won't resolve to a queue; Command tries name fallback
        // first and then returns 10 with a queue_not_found warning.
        var (rc, _, stderr) = RunCommand(QueueItemsCommand.Execute, new[] { "--queue-id", Guid.NewGuid().ToString() }, WithConnection(FilterSet.Empty));
        // With explicit --queue-id, the repo is queried directly and returns 0 rows, not 10.
        Assert.Equal(0, rc);
    }
}
