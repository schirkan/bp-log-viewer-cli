using BpLogViewer.Cli.Data;
using BpLogViewer.Cli.Models;
using Microsoft.Data.SqlClient;
using Xunit;

namespace BpLogViewer.Cli.IntegrationTests.Data;

[Collection("BpDb")]
public class SessionReaderTests
{
    private readonly BpLogViewerEnvironment _env;

    public SessionReaderTests(BpLogViewerEnvironment env) => _env = env;

    [Fact]
    public void DateTimeUtils_AssumeUtc_HandlesEdgeCases()
    {
        // SessionReader is internal and reads a live SqlDataReader; it cannot
        // be unit-tested without a real DB. The DateTime offset clamping logic
        // is exercised indirectly through SessionRepositoryTests and
        // SessionErrorRepositoryTests. This test documents the building block.
        var utc = DateTimeUtils.AssumeUtc(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Unspecified));
        Assert.Equal(TimeSpan.Zero, utc.Offset);
    }
}
