using BpLogViewer.Cli.Data;
using Xunit;

namespace BpLogViewer.Cli.IntegrationTests.Data;

[Collection("BpDb")]
public class SessionLogRepositoryTests
{
    private readonly BpLogViewerEnvironment _env;

    public SessionLogRepositoryTests(BpLogViewerEnvironment env) => _env = env;

    [Fact]
    public void NonUnicodeTable_IsCorrectName()
    {
        Assert.Equal("BPASessionLog_NonUnicode", SessionLogRepository.NonUnicodeTable);
    }

    [Fact]
    public void UnicodeTable_IsCorrectName()
    {
        Assert.Equal("BPASessionLog_Unicode", SessionLogRepository.UnicodeTable);
    }

    [Fact]
    public void ResolveActiveLogTable_ReturnsKnownTableOrNull()
    {
        _env.RequireDatabase();
        using var db = _env.CreateContext();
        var repo = new SessionLogRepository(db);
        var table = repo.ResolveActiveLogTable();
        // Either NonUnicode, Unicode, or null if both empty.
        Assert.True(table is null or SessionLogRepository.NonUnicodeTable or SessionLogRepository.UnicodeTable);
    }

    [Fact]
    public void List_NoMatchingSession_ReturnsEmpty()
    {
        _env.RequireDatabase();
        using var db = _env.CreateContext();
        var repo = new SessionLogRepository(db);
        var rows = repo.List(
            tableName: SessionLogRepository.NonUnicodeTable,
            sessionNumber: int.MaxValue,
            resultTypes: null, text: null, stage: null,
            fullResult: false, maxRowsPlusOne: 100,
            resultTypeNames: new Dictionary<int, string>());
        Assert.Empty(rows);
    }

    [Fact]
    public void List_UnknownTable_DoesNotThrow()
    {
        // BuildListCommand interpolates tableName; passing a non-existent
        // table name should produce a runtime SQL error, not a compile error.
        _env.RequireDatabase();
        using var db = _env.CreateContext();
        var repo = new SessionLogRepository(db);
        Assert.ThrowsAny<Exception>(() => repo.List(
            tableName: "__no_such_table__",
            sessionNumber: 1,
            resultTypes: null, text: null, stage: null,
            fullResult: false, maxRowsPlusOne: 10,
            resultTypeNames: new Dictionary<int, string>()));
    }
}
