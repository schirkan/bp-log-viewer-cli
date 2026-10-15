using BpLogViewer.Cli.Filters;
using Xunit;

namespace BpLogViewer.Cli.IntegrationTests.FilterTests;

public class FilterValidationExceptionTests
{
    [Fact]
    public void Constructor_SetsCodeMessageAndExitCode()
    {
        var ex = new FilterValidationException("custom_code", "boom", 42);
        Assert.Equal("custom_code", ex.Code);
        Assert.Equal("boom", ex.Message);
        Assert.Equal(42, ex.ExitCode);
    }

    [Fact]
    public void Exception_IsAnException()
    {
        var ex = new FilterValidationException("c", "m", 1);
        Assert.IsAssignableFrom<Exception>(ex);
    }
}
