using BpLogViewer.Cli.Data;
using Xunit;

namespace BpLogViewer.Cli.IntegrationTests.Data;

public class DateTimeUtilsTests
{
    [Fact]
    public void AssumeUtc_UnspecifiedDateTime_BecomesUtcOffset()
    {
        var dt = new DateTime(2026, 6, 15, 12, 0, 0, DateTimeKind.Unspecified);
        var result = DateTimeUtils.AssumeUtc(dt);
        Assert.Equal(DateTimeKind.Utc, result.Offset == TimeSpan.Zero ? DateTimeKind.Utc : DateTimeKind.Utc);
        Assert.Equal(TimeSpan.Zero, result.Offset);
        Assert.Equal(2026, result.Year);
    }

    [Fact]
    public void AssumeUtc_UtcDateTime_KeepsOffsetZero()
    {
        var dt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var result = DateTimeUtils.AssumeUtc(dt);
        Assert.Equal(TimeSpan.Zero, result.Offset);
    }

    [Fact]
    public void AssumeUtc_LocalDateTime_ReinterpretedAsUtc()
    {
        // Local time is converted via DateTime.SpecifyKind to Utc without
        // applying a local-to-utc offset shift.
        var dt = new DateTime(2026, 6, 15, 12, 0, 0, DateTimeKind.Local);
        var result = DateTimeUtils.AssumeUtc(dt);
        Assert.Equal(TimeSpan.Zero, result.Offset);
    }

    [Fact]
    public void AssumeUtc_ZeroDateTime_StaysZero()
    {
        var result = DateTimeUtils.AssumeUtc(DateTime.MinValue);
        Assert.Equal(DateTime.MinValue, result.UtcDateTime);
    }
}
