using BpLogViewer.Cli.Commands;
using BpLogViewer.Cli.Filters;
using Xunit;
using FilterSet = BpLogViewer.Cli.Filters.Filters;

namespace BpLogViewer.Cli.IntegrationTests.Commands;

public class ResourceListCommandTests : CommandSmokeTestBase
{
    public ResourceListCommandTests(BpLogViewerEnvironment env) : base(env) { }

    [Fact]
    public void Execute_NoArgs_Completes()
    {
        var (rc, _, stderr) = RunCommand(ResourceListCommand.Execute, Array.Empty<string>(), WithConnection(FilterSet.Empty));
        Assert.Equal(0, rc);
        Assert.DoesNotContain("db_error", stderr);
    }

    [Fact]
    public void Execute_WithNameFilter_ReturnsEmpty()
    {
        var (rc, _, _) = RunCommand(ResourceListCommand.Execute, new[] { "--name", "__no_such_resource__" }, WithConnection(FilterSet.Empty));
        Assert.Equal(0, rc);
    }

    [Fact]
    public void Execute_WithNameContainsFilter_ReturnsEmpty()
    {
        var (rc, _, _) = RunCommand(ResourceListCommand.Execute, new[] { "--name-contains", "__no_such_resource__" }, WithConnection(FilterSet.Empty));
        Assert.Equal(0, rc);
    }
}
