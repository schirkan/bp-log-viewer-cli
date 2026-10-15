using System.Text;
using System.Text.Json;
using BpLogViewer.Cli.Output;
using Xunit;

namespace BpLogViewer.Cli.IntegrationTests.Output;

public class ErrorWriterTests
{
    [Fact]
    public async Task WriteErrorAsync_WritesValidJsonlToStderr()
    {
        var origErr = Console.Error;
        var sw = new StringWriter { NewLine = "\n" };
        Console.SetError(sw);
        try
        {
            await ErrorWriter.WriteErrorAsync("test_code", "test message", 42);
            var output = sw.ToString().Trim();
            Assert.NotEmpty(output);

            using var doc = JsonDocument.Parse(output);
            var root = doc.RootElement;
            Assert.Equal("test_code", root.GetProperty("code").GetString());
            Assert.Equal("test message", root.GetProperty("message").GetString());
            Assert.Equal(42, root.GetProperty("exit_code").GetInt32());
        }
        finally
        {
            Console.SetError(origErr);
        }
    }

    [Fact]
    public async Task WriteWarningAsync_WritesValidJsonlToStderr()
    {
        var origErr = Console.Error;
        var sw = new StringWriter { NewLine = "\n" };
        Console.SetError(sw);
        try
        {
            await ErrorWriter.WriteWarningAsync("warn_code", "warn detail");
            var output = sw.ToString().Trim();
            Assert.NotEmpty(output);

            using var doc = JsonDocument.Parse(output);
            var root = doc.RootElement;
            Assert.Equal("warn_code", root.GetProperty("warning").GetString());
            Assert.Equal("warn detail", root.GetProperty("detail").GetString());
        }
        finally
        {
            Console.SetError(origErr);
        }
    }

    [Fact]
    public async Task WriteErrorAsync_MultipleErrors_EachOnOwnLine()
    {
        var origErr = Console.Error;
        var sw = new StringWriter { NewLine = "\n" };
        Console.SetError(sw);
        try
        {
            await ErrorWriter.WriteErrorAsync("e1", "m1", 1);
            await ErrorWriter.WriteErrorAsync("e2", "m2", 2);
            var lines = sw.ToString().Split('\n', StringSplitOptions.RemoveEmptyEntries);
            Assert.Equal(2, lines.Length);
        }
        finally
        {
            Console.SetError(origErr);
        }
    }
}
