using BpLogViewer.Cli.Commands;
using BpLogViewer.Cli.Filters;
using Xunit;
using FilterSet = BpLogViewer.Cli.Filters.Filters;

namespace BpLogViewer.Cli.IntegrationTests.Commands;

public class SessionGetCommandTests : CommandSmokeTestBase
{
    public SessionGetCommandTests(BpLogViewerEnvironment env) : base(env) { }

    [Fact]
    public void Execute_NonExistentSession_ReturnsExit10()
    {
        var (rc, _, stderr) = RunCommand(SessionGetCommand.Execute, new[] { int.MaxValue.ToString() }, WithConnection(FilterSet.Empty));
        Assert.Equal(10, rc);
        Assert.Contains("session_not_found", stderr);
    }

    [Fact]
    public void Execute_NoArgs_ReturnsExit11()
    {
        var (rc, _, stderr) = RunCommand(SessionGetCommand.Execute, Array.Empty<string>(), WithConnection(FilterSet.Empty));
        Assert.Equal(11, rc);
        Assert.Contains("command_invalid", stderr);
    }

    [Fact]
    public void Execute_InvalidSessionId_ReturnsExit11()
    {
        var (rc, _, stderr) = RunCommand(SessionGetCommand.Execute, new[] { "--session-id", "not-a-guid" }, WithConnection(FilterSet.Empty));
        Assert.Equal(11, rc);
        Assert.Contains("invalid --session-id", stderr);
    }

    [Fact]
    public void Execute_InvalidSessionNumber_ReturnsExit11()
    {
        var (rc, _, stderr) = RunCommand(SessionGetCommand.Execute, new[] { "abc" }, WithConnection(FilterSet.Empty));
        Assert.Equal(11, rc);
        Assert.Contains("invalid session number", stderr);
    }

    [Fact]
    public void Execute_NonExistentSessionId_ReturnsExit10()
    {
        var (rc, _, stderr) = RunCommand(SessionGetCommand.Execute, new[] { "--session-id", Guid.NewGuid().ToString() }, WithConnection(FilterSet.Empty));
        Assert.Equal(10, rc);
        Assert.Contains("session_not_found", stderr);
    }
}
