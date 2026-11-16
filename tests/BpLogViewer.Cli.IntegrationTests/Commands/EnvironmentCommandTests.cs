using System.Text.Json;
using BpLogViewer.Cli.Commands;
using BpLogViewer.Cli.Filters;
using Xunit;
using FilterSet = BpLogViewer.Cli.Filters.Filters;

namespace BpLogViewer.Cli.IntegrationTests.Commands;

public class EnvironmentCommandTests : CommandSmokeTestBase
{
    public EnvironmentCommandTests(BpLogViewerEnvironment env) : base(env) { }

    [Theory]
    [InlineData("--name-contains", "--name", false)]
    [InlineData("--name", "--name-contains", true)]
    public void Vars_LastNameOptionControlsMatching(string first, string last, bool contains)
    {
        var (rc, stdout, stderr) = RunCommand(EnvironmentVarsCommand.Execute,
            new[] { first, "Text", last, "Text" }, WithConnection(FilterSet.Empty));
        Assert.True(rc == 0, $"rc={rc} stderr={stderr}");
        foreach (var line in stdout.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            using var doc = JsonDocument.Parse(line);
            var name = doc.RootElement.GetProperty("name").GetString()!;
            Assert.True(contains ? name.Contains("Text", StringComparison.OrdinalIgnoreCase)
                : name.StartsWith("Text", StringComparison.OrdinalIgnoreCase));
        }
    }

    [Fact]
    public void Vars_NoArgs_Completes()
    {
        var (rc, _, stderr) = RunCommand(EnvironmentVarsCommand.Execute, Array.Empty<string>(), WithConnection(FilterSet.Empty));
        Assert.True(rc == 0, $"rc={rc} stderr={stderr}");
        Assert.DoesNotContain("db_error", stderr);
    }

    [Fact]
    public void Vars_WithNameFilter_Completes()
    {
        var (rc, _, stderr) = RunCommand(EnvironmentVarsCommand.Execute, new[] { "--name", "My" }, WithConnection(FilterSet.Empty));
        Assert.Equal(0, rc);
        Assert.DoesNotContain("db_error", stderr);
    }

    [Fact]
    public void Vars_WithNameContainsFilter_Completes()
    {
        var (rc, _, stderr) = RunCommand(EnvironmentVarsCommand.Execute, new[] { "--name-contains", "Env" }, WithConnection(FilterSet.Empty));
        Assert.Equal(0, rc);
        Assert.DoesNotContain("db_error", stderr);
    }

    [Fact]
    public void Vars_UnknownNameFilter_ReturnsNoRows()
    {
        var (rc, stdout, stderr) = RunCommand(EnvironmentVarsCommand.Execute, new[] { "--name", "zzz-does-not-exist-4711" }, WithConnection(FilterSet.Empty));
        Assert.True(rc == 0, $"rc={rc} stderr={stderr}");
        Assert.DoesNotContain("db_error", stderr);
        Assert.Equal(string.Empty, stdout.Trim());
    }

    [Fact]
    public void Vars_MissingOptionValue_ReturnsExit11()
    {
        var (rc, _, stderr) = RunCommand(EnvironmentVarsCommand.Execute, new[] { "--name" }, WithConnection(FilterSet.Empty));
        Assert.Equal(11, rc);
        Assert.Contains("command_invalid", stderr);
    }

    [Fact]
    public void Dependencies_NoArgs_Completes()
    {
        var (rc, _, stderr) = RunCommand(EnvironmentDependenciesCommand.Execute, Array.Empty<string>(), WithConnection(FilterSet.Empty));
        Assert.Equal(0, rc);
        Assert.DoesNotContain("db_error", stderr);
    }

    [Fact]
    public void Dependencies_WithProcessNameFilter_Completes()
    {
        var (rc, _, stderr) = RunCommand(EnvironmentDependenciesCommand.Execute, new[] { "--process-name", "bp" }, WithConnection(FilterSet.Empty));
        Assert.Equal(0, rc);
        Assert.DoesNotContain("db_error", stderr);
    }

    [Fact]
    public void Dependencies_WithVariableFilter_Completes()
    {
        var (rc, _, stderr) = RunCommand(EnvironmentDependenciesCommand.Execute, new[] { "--variable", "MyEnv" }, WithConnection(FilterSet.Empty));
        Assert.Equal(0, rc);
        Assert.DoesNotContain("db_error", stderr);
    }

    [Fact]
    public void Dependencies_UnknownVariableFilter_ReturnsNoRows()
    {
        var (rc, stdout, stderr) = RunCommand(EnvironmentDependenciesCommand.Execute, new[] { "--variable", "zzz-does-not-exist-4711" }, WithConnection(FilterSet.Empty));
        Assert.Equal(0, rc);
        Assert.DoesNotContain("db_error", stderr);
        Assert.Equal(string.Empty, stdout.Trim());
    }

    [Fact]
    public void Dependencies_MissingOptionValue_ReturnsExit11()
    {
        var (rc, _, stderr) = RunCommand(EnvironmentDependenciesCommand.Execute, new[] { "--variable" }, WithConnection(FilterSet.Empty));
        Assert.Equal(11, rc);
        Assert.Contains("command_invalid", stderr);
    }

    [Fact]
    public void Dependencies_RowsCarryIdProcessAndVariableName()
    {
        var (rc, stdout, stderr) = RunCommand(EnvironmentDependenciesCommand.Execute, Array.Empty<string>(), WithConnection(FilterSet.Empty));
        Assert.Equal(0, rc);
        Assert.DoesNotContain("db_error", stderr);

        foreach (var line in stdout.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            using var doc = JsonDocument.Parse(line);
            var row = doc.RootElement;
            Assert.Equal(JsonValueKind.Number, row.GetProperty("id").ValueKind);
            Assert.Equal(JsonValueKind.String, row.GetProperty("variable_name").ValueKind);
            // A missing process in the LEFT JOIN omits the nullable process_name.
            if (row.TryGetProperty("process_name", out var processName))
                Assert.Equal(JsonValueKind.String, processName.ValueKind);
            Assert.True(row.TryGetProperty("process_id", out _));
        }
    }

    [Fact]
    public void Vars_RowsCarryNameDatatypeValueAndDescription()
    {
        var (rc, stdout, stderr) = RunCommand(EnvironmentVarsCommand.Execute, Array.Empty<string>(), WithConnection(FilterSet.Empty));
        Assert.Equal(0, rc);
        Assert.DoesNotContain("db_error", stderr);

        foreach (var line in stdout.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            using var doc = JsonDocument.Parse(line);
            var row = doc.RootElement;
            Assert.Equal(JsonValueKind.String, row.GetProperty("name").ValueKind);
            Assert.True(row.TryGetProperty("datatype", out _));
            Assert.True(row.TryGetProperty("value", out _));
            Assert.True(row.TryGetProperty("description", out _));
            Assert.True(row.TryGetProperty("id", out _));
        }
    }
}
