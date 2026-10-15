using BpLogViewer.Cli.Filters;
using Xunit;
using FilterSet = BpLogViewer.Cli.Filters.Filters;

namespace BpLogViewer.Cli.IntegrationTests.Commands;

/// <summary>
/// Base class for command smoke tests. Provides stdout/stderr capture and
/// a <see cref="RunCommand"/> helper that runs a command with the test
/// environment's connection string.
/// </summary>
[Collection("BpDb")]
public abstract class CommandSmokeTestBase
{
    private readonly BpLogViewerEnvironment _env;

    protected CommandSmokeTestBase(BpLogViewerEnvironment env) => _env = env;

    protected BpLogViewerEnvironment Env => _env;

    /// <summary>
    /// Runs a command delegate with stdout/stderr captured. Returns the
    /// exit code plus the captured output.
    /// </summary>
    protected (int exitCode, string stdout, string stderr) RunCommand(
        Func<FilterSet, int> command,
        FilterSet filters)
    {
        _env.RequireDatabase();
        var origOut = Console.Out;
        var origErr = Console.Error;
        var swOut = new StringWriter();
        var swErr = new StringWriter();
        Console.SetOut(swOut);
        Console.SetError(swErr);
        try
        {
            var rc = command(filters);
            return (rc, swOut.ToString(), swErr.ToString());
        }
        finally
        {
            Console.SetOut(origOut);
            Console.SetError(origErr);
        }
    }

    protected (int exitCode, string stdout, string stderr) RunCommand(
        Func<string[], FilterSet, int> command,
        string[] subArgs,
        FilterSet filters)
    {
        _env.RequireDatabase();
        var origOut = Console.Out;
        var origErr = Console.Error;
        var swOut = new StringWriter();
        var swErr = new StringWriter();
        Console.SetOut(swOut);
        Console.SetError(swErr);
        try
        {
            var rc = command(subArgs, filters);
            return (rc, swOut.ToString(), swErr.ToString());
        }
        finally
        {
            Console.SetOut(origOut);
            Console.SetError(origErr);
        }
    }

    protected FilterSet WithConnection(FilterSet filters) =>
        filters with { ConnectionString = _env.ConnectionString };
}
