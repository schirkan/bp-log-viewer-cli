using BpLogViewer.Cli.Commands;
using BpLogViewer.Cli.Filters;
using Xunit;
using FilterSet = BpLogViewer.Cli.Filters.Filters;

namespace BpLogViewer.Cli.IntegrationTests.Commands;

public class SessionListCommandTests : CommandSmokeTestBase
{
    public SessionListCommandTests(BpLogViewerEnvironment env) : base(env) { }

    [Fact]
    public void Execute_NoFilters_Completes()
    {
        var (rc, stdout, stderr) = RunCommand(SessionListCommand.Execute, WithConnection(FilterSet.Empty));
        Assert.Equal(0, rc);
        // Empty test DB is fine — should be a valid empty result, not an error.
        Assert.DoesNotContain("db_error", stderr);
    }

    [Fact]
    public void Execute_WithMaxRows1_RespectsLimit()
    {
        var f = WithConnection(FilterSet.Empty) with { MaxRows = 1 };
        var (rc, stdout, stderr) = RunCommand(SessionListCommand.Execute, f);
        // If there are any rows, output is truncated; rc = 2.
        // If there are no rows, rc = 0.
        Assert.True(rc is 0 or 2, $"Unexpected exit code {rc}; stderr={stderr}");
        if (rc == 2)
        {
            Assert.Contains("output_truncated", stderr);
        }
    }

    [Fact]
    public void Execute_NonExistentProcess_ReturnsEmpty()
    {
        var f = WithConnection(FilterSet.Empty) with { Process = "__no_such_process__" };
        var (rc, stdout, stderr) = RunCommand(SessionListCommand.Execute, f);
        Assert.Equal(0, rc);
        Assert.Empty(stdout.Trim());
    }

    [Fact]
    public void Execute_WithInvalidStatus_ReportsError()
    {
        var f = WithConnection(FilterSet.Empty) with { Status = new[] { "__no_such_status__" } };
        var (rc, _, stderr) = RunCommand(SessionListCommand.Execute, f);
        Assert.Equal(11, rc);
        Assert.Contains("unknown_status_value", stderr);
    }
}
