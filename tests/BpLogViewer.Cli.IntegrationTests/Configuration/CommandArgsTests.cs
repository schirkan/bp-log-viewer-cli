using BpLogViewer.Cli.Configuration;
using Xunit;

namespace BpLogViewer.Cli.IntegrationTests.Configuration;

public class CommandArgsTests
{
    [Fact]
    public void CommandOption_ValueIsConsumedAndNotPositional()
    {
        var result = CommandArgs.Parse(new[] { "--name-contains", "Health" }, "process", "list");
        Assert.Null(result.Error);
        Assert.Equal("Health", result.Value("--name-contains"));
        Assert.Empty(result.Positional);
    }

    [Fact]
    public void CommandFlag_DoesNotConsumeTheFollowingToken()
    {
        var result = CommandArgs.Parse(new[] { "--only-running", "--max-rows", "50" }, "queue", "list");
        Assert.Null(result.Error);
        Assert.True(result.HasFlag("--only-running"));
        Assert.Equal("50", result.Value("--max-rows"));
        Assert.Empty(result.Positional);
    }

    [Fact]
    public void MissingValue_IsReportedAsError()
    {
        var result = CommandArgs.Parse(new[] { "--stage" }, "session", "log");
        Assert.NotNull(result.Error);
        Assert.Contains("requires a value", result.Error);
    }

    [Fact]
    public void GlobalOption_ValueIsConsumedAndRecorded()
    {
        var result = CommandArgs.Parse(new[] { "--status", "Failed", "MyQueue" }, "queue", "items");
        Assert.Null(result.Error);
        Assert.Equal("Failed", result.Value("--status"));
        Assert.Equal(new[] { "MyQueue" }, result.Positional);
    }

    [Fact]
    public void UnknownOption_IsIgnored_WithoutError()
    {
        var result = CommandArgs.Parse(new[] { "--typo", "value" }, "process", "list");
        Assert.Null(result.Error);

        // Ein unbekanntes Option verbraucht keinen Wert. Der folgende Token bleibt
        // damit positional - unveraendertes Verhalten gegenüber der früheren
        // handgeschriebenen Parserlogik in den Commands.
        Assert.Equal(new[] { "value" }, result.Positional);
    }

    [Fact]
    public void PositionalArgumentsKeepTheirOrder()
    {
        var result = CommandArgs.Parse(new[] { "4711", "--stage", "X" }, "session", "log");
        Assert.Null(result.Error);
        Assert.Equal(new[] { "4711" }, result.Positional);
        Assert.Equal("X", result.Value("--stage"));
    }

    [Fact]
    public void LastValue_PicksTheOptionThatCameLast()
    {
        var result = CommandArgs.Parse(new[] { "--name", "A", "--name-contains", "B" }, "process", "list");
        Assert.Equal("B", result.LastValue("--name", "--name-contains"));
        Assert.True(result.Values("--name-contains").Count > 0);
    }

    [Fact]
    public void UnknownCommand_HasNoCommandOptions()
    {
        Assert.Empty(OptionRegistry.ForCommand("does-not-exist", "list"));
        Assert.Empty(OptionRegistry.ForCommand("process", "does-not-exist"));
    }

    [Fact]
    public void Registry_GlobalOptionsWithValueMatchTheirArity()
    {
        foreach (var option in OptionRegistry.GlobalOptionsWithValue)
            Assert.Equal(OptionArity.Value, OptionRegistry.GlobalArity(option));

        foreach (var (option, arity) in OptionRegistry.Global)
        {
            if (arity == OptionArity.Value)
                Assert.Contains(option, OptionRegistry.GlobalOptionsWithValue);
        }
    }

    [Fact]
    public void Registry_CommandOptionsAreNotGlobal()
    {
        // Command-Optionen sind exklusiv; sonst würde ein gleichnamiges globales
        // Option den Dispatcher verwirren.
        foreach (var command in new[] { "process", "resource", "queue", "session", "audit" })
        {
            foreach (var action in new[] { "list", "items", "get", "log", "errors" })
            {
                foreach (var option in OptionRegistry.ForCommand(command, action).Keys)
                    Assert.Null(OptionRegistry.GlobalArity(option));
            }
        }
    }

    [Fact]
    public void Registry_AllCommandOptionsAreKnown()
    {
        var expected = new[]
        {
            "process list", "resource list", "queue list", "queue items",
            "session get", "session log", "session errors", "audit list",
        };

        foreach (var key in expected)
        {
            var parts = key.Split(' ');
            Assert.NotEmpty(OptionRegistry.ForCommand(parts[0], parts[1]));
        }
    }
}
