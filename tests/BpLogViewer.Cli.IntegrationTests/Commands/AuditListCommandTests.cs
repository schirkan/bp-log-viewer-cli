using BpLogViewer.Cli.Commands;
using BpLogViewer.Cli.Filters;
using Xunit;
using FilterSet = BpLogViewer.Cli.Filters.Filters;

namespace BpLogViewer.Cli.IntegrationTests.Commands;

public class AuditListCommandTests : CommandSmokeTestBase
{
    public AuditListCommandTests(BpLogViewerEnvironment env) : base(env) { }

    [Fact]
    public void Execute_NoArgs_Completes()
    {
        var (rc, _, stderr) = RunCommand(AuditListCommand.Execute, Array.Empty<string>(), WithConnection(FilterSet.Empty));
        Assert.Equal(0, rc);
        Assert.DoesNotContain("db_error", stderr);
    }

    [Fact]
    public void Execute_WithCodeFilter_Completes()
    {
        var (rc, _, stderr) = RunCommand(AuditListCommand.Execute, new[] { "--code", "RESSTART" }, WithConnection(FilterSet.Empty));
        Assert.Equal(0, rc);
        Assert.DoesNotContain("db_error", stderr);
    }
}
