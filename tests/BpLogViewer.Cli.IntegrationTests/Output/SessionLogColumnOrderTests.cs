using System.Text.Json;
using BpLogViewer.Cli.Models;
using BpLogViewer.Cli.Output;
using Xunit;

namespace BpLogViewer.Cli.IntegrationTests.Output;

public class SessionLogColumnOrderTests
{
    private static readonly string[] ExpectedColumns =
    {
        "log_id",
        "started_at",
        "stage_name",
        "stage_type",
        "result_type",
        "result_type_name",
        "result_preview",
        "attribute_size_bytes",
    };

    [Fact]
    public void HeaderJson_ListsTheExpectedColumnNames()
    {
        using var doc = JsonDocument.Parse(SessionLogEntryJsonConverter.HeaderJson);
        var names = doc.RootElement.EnumerateArray().Select(e => e.GetString()).ToArray();
        Assert.Equal(ExpectedColumns, names);
    }

    [Fact]
    public void ColumnNames_And_HeaderJson_DescribeTheSameOrder()
    {
        using var doc = JsonDocument.Parse(SessionLogEntryJsonConverter.HeaderJson);
        Assert.Equal(SessionLogEntryJsonConverter.ColumnNames.Count, doc.RootElement.GetArrayLength());
        for (var i = 0; i < SessionLogEntryJsonConverter.ColumnNames.Count; i++)
            Assert.Equal(SessionLogEntryJsonConverter.ColumnNames[i], doc.RootElement[i].GetString());
    }

    [Fact]
    public void HeaderName_And_ArrayPosition_ReferToTheSameValue()
    {
        var startedAt = new DateTimeOffset(2026, 10, 26, 12, 0, 0, TimeSpan.Zero);
        var entry = new SessionLogEntry(42, "internal", "Health", 1, 2, "Error",
            startedAt, startedAt.AddSeconds(1), 1000, "Result", 50);

        using var doc = JsonDocument.Parse(JsonSerializer.Serialize(entry, PayloadJsonContext.Readable.SessionLogEntry));
        var row = doc.RootElement;

        Assert.Equal(42, row[IndexOf("log_id")].GetInt64());
        Assert.Equal(startedAt, row[IndexOf("started_at")].GetDateTimeOffset());
        Assert.Equal("Health", row[IndexOf("stage_name")].GetString());
        Assert.Equal(1, row[IndexOf("stage_type")].GetInt32());
        Assert.Equal(2, row[IndexOf("result_type")].GetInt32());
        Assert.Equal("Error", row[IndexOf("result_type_name")].GetString());
        Assert.Equal("Result", row[IndexOf("result_preview")].GetString());
        Assert.Equal(50, row[IndexOf("attribute_size_bytes")].GetInt64());
    }

    private static int IndexOf(string column)
    {
        var names = SessionLogEntryJsonConverter.ColumnNames;
        for (var i = 0; i < names.Count; i++)
        {
            if (string.Equals(names[i], column, StringComparison.Ordinal))
                return i;
        }

        throw new InvalidOperationException($"unknown column '{column}'");
    }
}
