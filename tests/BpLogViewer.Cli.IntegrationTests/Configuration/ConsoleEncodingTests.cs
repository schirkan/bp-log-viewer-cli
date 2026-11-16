using System.Text;
using Xunit;

namespace BpLogViewer.Cli.IntegrationTests.Configuration;

public class ConsoleEncodingTests
{
    [Fact]
    public void EntryPoint_ConfiguresUtf8WithoutBom()
    {
        var originalEncoding = Console.OutputEncoding;
        var originalOut = Console.Out;
        using var capture = new StringWriter();
        try
        {
            Console.OutputEncoding = Encoding.ASCII;
            Console.SetOut(capture);
            Assert.Equal(0, global::BpLogViewer.Cli.Program.Main(new[] { "version" }));
            Assert.Equal(Encoding.UTF8.CodePage, Console.OutputEncoding.CodePage);
            Assert.Empty(Console.OutputEncoding.GetPreamble());
        }
        finally
        {
            Console.OutputEncoding = originalEncoding;
            Console.SetOut(originalOut);
        }
    }
}
