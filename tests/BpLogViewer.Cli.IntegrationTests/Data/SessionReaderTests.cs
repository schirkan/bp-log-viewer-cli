using System.Data;
using BpLogViewer.Cli.Data;
using Xunit;

namespace BpLogViewer.Cli.IntegrationTests.Data;

public class SessionReaderTests
{
    [Theory]
    [InlineData(7200, 120)]
    [InlineData(3600, 60)]
    [InlineData(0, 0)]
    [InlineData(-18000, -300)]
    [InlineData(19800, 330)]
    [InlineData(50400, 840)]
    [InlineData(-50400, -840)]
    public void ReadDateTimeOffsetSafe_UsesSecondsAndPreservesUtcInstant(int offsetSeconds, int expectedMinutes)
    {
        var timestamp = new DateTime(2026, 10, 15, 11, 13, 14, 943);
        using var table = CreateTable(timestamp, offsetSeconds);
        using var reader = table.CreateDataReader();
        Assert.True(reader.Read());

        var result = SessionReader.ReadDateTimeOffsetSafe(reader, 0, 1);

        Assert.Equal(TimeSpan.FromMinutes(expectedMinutes), result.Offset);
        Assert.Equal(timestamp.AddMinutes(expectedMinutes), result.DateTime);
        Assert.Equal(timestamp, result.UtcDateTime);
    }

    [Theory]
    [InlineData(50460)]
    [InlineData(-50460)]
    public void ReadDateTimeOffsetSafe_InvalidOffsetIsNotClamped(int offsetSeconds)
    {
        using var table = CreateTable(new DateTime(2026, 10, 15), offsetSeconds);
        using var reader = table.CreateDataReader();
        Assert.True(reader.Read());

        Assert.Throws<ArgumentOutOfRangeException>(() => SessionReader.ReadDateTimeOffsetSafe(reader, 0, 1));
    }

    [Fact]
    public void ReadDateTimeOffsetSafe_Session154307_MatchesRecordedTimestampsAndDuration()
    {
        using var table = CreateTable(new DateTime(2026, 10, 15, 11, 13, 14, 943), 7200);
        table.Rows.Add(new DateTime(2026, 10, 15, 11, 23, 36, 893), 7200);
        using var reader = table.CreateDataReader();
        Assert.True(reader.Read());
        var start = SessionReader.ReadDateTimeOffsetSafe(reader, 0, 1);
        Assert.True(reader.Read());
        var end = SessionReader.ReadDateTimeOffsetSafe(reader, 0, 1);

        Assert.Equal("2026-10-15T13:13:14.943+02:00", start.ToString("yyyy-MM-ddTHH:mm:ss.fffzzz"));
        Assert.Equal("2026-10-15T13:23:36.893+02:00", end.ToString("yyyy-MM-ddTHH:mm:ss.fffzzz"));
        Assert.Equal(621950, (end - start).TotalMilliseconds);

        var logStart = DateTimeUtils.AssumeUtc(new DateTime(2026, 10, 15, 11, 23, 32, 623));
        Assert.True(logStart >= start && logStart <= end);
        Assert.Equal(4270, (end - logStart).TotalMilliseconds);
    }

    [Fact]
    public void ReadDateTimeOffsetSafe_OffsetChangeDoesNotChangeElapsedDuration()
    {
        using var table = CreateTable(new DateTime(2026, 10, 25, 0, 30, 0), 7200);
        table.Rows.Add(new DateTime(2026, 10, 25, 1, 30, 0), 3600);
        using var reader = table.CreateDataReader();
        Assert.True(reader.Read());
        var start = SessionReader.ReadDateTimeOffsetSafe(reader, 0, 1);
        Assert.True(reader.Read());
        var end = SessionReader.ReadDateTimeOffsetSafe(reader, 0, 1);

        Assert.Equal(start.DateTime, end.DateTime);
        Assert.Equal(TimeSpan.FromHours(1), end - start);
    }

    private static DataTable CreateTable(DateTime timestamp, int offsetSeconds)
    {
        var table = new DataTable();
        table.Columns.Add("timestamp", typeof(DateTime));
        table.Columns.Add("offset", typeof(int));
        table.Rows.Add(timestamp, offsetSeconds);
        return table;
    }
}
