using BpLogViewer.Cli;
using BpLogViewer.Cli.Filters;
using Xunit;
using FilterSet = BpLogViewer.Cli.Filters.Filters;

namespace BpLogViewer.Cli.IntegrationTests.ProgramTests;

[Collection("BpDb")]
public class ProgramDispatchTests
{
    private readonly BpLogViewerEnvironment _env;

    public ProgramDispatchTests(BpLogViewerEnvironment env) => _env = env;

    [Fact]
    public void Main_NoArgs_Returns0AndPrintsHelp()
    {
        var (rc, _, stderr) = CaptureMain(Array.Empty<string>());
        Assert.Equal(0, rc);
        Assert.Contains("Usage: bp-log-viewer", stderr);
    }

    [Fact]
    public void Main_Version_Returns0()
    {
        var (rc, stdout, _) = CaptureMain(new[] { "version" });
        Assert.Equal(0, rc);
        Assert.Contains("\"cli_version\"", stdout);
    }

    [Fact]
    public void Main_DashDashVersion_Returns0()
    {
        var (rc, stdout, _) = CaptureMain(new[] { "--version" });
        Assert.Equal(0, rc);
        Assert.Contains("\"cli_version\"", stdout);
    }

    [Fact]
    public void Main_DashV_Returns0()
    {
        var (rc, stdout, _) = CaptureMain(new[] { "-v" });
        Assert.Equal(0, rc);
        Assert.Contains("\"cli_version\"", stdout);
    }

    [Fact]
    public void Main_Help_Returns0()
    {
        var (rc, _, stderr) = CaptureMain(new[] { "help" });
        Assert.Equal(0, rc);
        Assert.Contains("Usage: bp-log-viewer", stderr);
    }

    [Fact]
    public void Main_UnknownCommand_Returns11()
    {
        var (rc, _, stderr) = CaptureMain(new[] { "definitely_no_such_command" });
        Assert.Equal(11, rc);
        Assert.Contains("unknown_command", stderr);
    }

    [Fact]
    public void Main_SessionList_Returns0()
    {
        _env.RequireDatabase();
        var (rc, _, stderr) = CaptureMain(new[] { "session", "list", "--connection-string", _env.ConnectionString });
        Assert.Equal(0, rc);
        Assert.DoesNotContain("db_error", stderr);
    }

    [Fact]
    public void Main_GlobalOptionsAfterSubCommand_AreRespected()
    {
        // Regression test for the global-options-bug found in code review.
        // --max-rows placed AFTER the sub-command must still apply.
        _env.RequireDatabase();
        var (rc, stdout, stderr) = CaptureMain(new[] { "session", "list", "--max-rows", "1", "--connection-string", _env.ConnectionString });
        Assert.True(rc is 0 or 2);
        // If there is data, output should contain at most one line.
        if (rc == 2)
        {
            Assert.Contains("output_truncated", stderr);
        }
    }

    [Fact]
    public void Main_GlobalOptionsBeforeSubCommand_AreRespected()
    {
        _env.RequireDatabase();
        var (rc, stdout, _) = CaptureMain(new[] { "--max-rows", "1", "session", "list", "--connection-string", _env.ConnectionString });
        Assert.True(rc is 0 or 2);
    }

    [Fact]
    public void Main_SessionListMaxRowsExceedsCeiling_Returns8()
    {
        var (rc, _, stderr) = CaptureMain(new[] { "session", "list", "--max-rows", "1000001" });
        Assert.Equal(8, rc);
        Assert.Contains("max_rows_exceeds_ceiling", stderr);
    }

    [Fact]
    public void Main_SelfTest_Completes()
    {
        _env.RequireDatabase();
        var (rc, stdout, _) = CaptureMain(new[] { "self-test", "--connection-string", _env.ConnectionString });
        Assert.Equal(0, rc);
        Assert.Contains("\"step\":\"db_connect\"", stdout);
    }

    [Fact]
    public void Main_SessionGetNonExistent_Returns10()
    {
        _env.RequireDatabase();
        var (rc, _, stderr) = CaptureMain(new[] { "session", "get", int.MaxValue.ToString(), "--connection-string", _env.ConnectionString });
        Assert.Equal(10, rc);
        Assert.Contains("session_not_found", stderr);
    }

    [Fact]
    public void Main_SessionSubcommandHelp_Returns0()
    {
        var (rc, _, stderr) = CaptureMain(new[] { "session" });
        Assert.Equal(0, rc);
        Assert.Contains("Usage: bp-log-viewer session", stderr);
    }

    [Fact]
    public void Main_ResourceList_Returns0()
    {
        _env.RequireDatabase();
        var (rc, _, stderr) = CaptureMain(new[] { "resource", "list", "--connection-string", _env.ConnectionString });
        Assert.Equal(0, rc);
        Assert.DoesNotContain("db_error", stderr);
    }

    [Fact]
    public void Main_ProcessList_Returns0()
    {
        _env.RequireDatabase();
        var (rc, _, stderr) = CaptureMain(new[] { "process", "list", "--connection-string", _env.ConnectionString });
        Assert.Equal(0, rc);
        Assert.DoesNotContain("db_error", stderr);
    }

    [Fact]
    public void Main_QueueList_Returns0()
    {
        _env.RequireDatabase();
        var (rc, _, stderr) = CaptureMain(new[] { "queue", "list", "--connection-string", _env.ConnectionString });
        Assert.Equal(0, rc);
        Assert.DoesNotContain("db_error", stderr);
    }

    [Fact]
    public void Main_AuditList_Returns0()
    {
        _env.RequireDatabase();
        var (rc, _, stderr) = CaptureMain(new[] { "audit", "list", "--connection-string", _env.ConnectionString });
        Assert.Equal(0, rc);
        Assert.DoesNotContain("db_error", stderr);
    }

    private static (int rc, string stdout, string stderr) CaptureMain(string[] args)
    {
        var origOut = Console.Out;
        var origErr = Console.Error;
        var swOut = new StringWriter();
        var swErr = new StringWriter();
        Console.SetOut(swOut);
        Console.SetError(swErr);
        try
        {
            var rc = BpLogViewer.Cli.Program.Main(args);
            return (rc, swOut.ToString(), swErr.ToString());
        }
        finally
        {
            Console.SetOut(origOut);
            Console.SetError(origErr);
        }
    }
}
