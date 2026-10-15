using BpLogViewer.Cli.Data;
using Xunit;

namespace BpLogViewer.Cli.IntegrationTests.Data;

[Collection("BpDb")]
public class SessionErrorRepositoryTests
{
    private readonly BpLogViewerEnvironment _env;

    public SessionErrorRepositoryTests(BpLogViewerEnvironment env) => _env = env;

    [Fact]
    public void List_RespectsMaxRows()
    {
        _env.RequireDatabase();
        using var db = _env.CreateContext();
        var repo = new SessionErrorRepository(db);
        var rows = repo.List(
            since: null, until: null, process: null, resource: null,
            text: null, exceptionTypes: null, maxRowsPlusOne: 5);
        Assert.NotNull(rows);
        Assert.True(rows.Count <= 5);
    }

    [Fact]
    public void List_NonexistentProcess_ReturnsEmpty()
    {
        _env.RequireDatabase();
        using var db = _env.CreateContext();
        var repo = new SessionErrorRepository(db);
        var rows = repo.List(
            since: null, until: null,
            process: "__nonexistent_process__",
            resource: null, text: null, exceptionTypes: null, maxRowsPlusOne: 100);
        Assert.Empty(rows);
    }

    [Fact]
    public void List_TextFilter_NoMatchReturnsEmpty()
    {
        _env.RequireDatabase();
        using var db = _env.CreateContext();
        var repo = new SessionErrorRepository(db);
        var rows = repo.List(
            since: null, until: null, process: null, resource: null,
            text: "__nonexistent_text_pattern_zxcvbnm__",
            exceptionTypes: null, maxRowsPlusOne: 100);
        Assert.Empty(rows);
    }

    [Fact]
    public void Aggregate_ReturnsGroupedByExceptionType()
    {
        _env.RequireDatabase();
        using var db = _env.CreateContext();
        var repo = new SessionErrorRepository(db);
        var result = repo.Aggregate(
            since: null, until: null, process: null, resource: null,
            text: null, exceptionTypes: null);
        Assert.NotNull(result);
        // Result may be empty if there are no exceptions; structure must still be valid.
        foreach (var a in result)
        {
            Assert.False(string.IsNullOrEmpty(a.ExceptionType));
            Assert.True(a.Count > 0);
            Assert.True(a.FirstSeen <= a.LastSeen);
        }
    }

    [Fact]
    public void Aggregate_FutureSince_ReturnsEmpty()
    {
        _env.RequireDatabase();
        using var db = _env.CreateContext();
        var repo = new SessionErrorRepository(db);
        var result = repo.Aggregate(
            since: DateTimeOffset.UtcNow.AddYears(10), until: null,
            process: null, resource: null, text: null, exceptionTypes: null);
        Assert.Empty(result);
    }

    [Fact]
    public void Aggregate_ProcessFilter_Scoped()
    {
        _env.RequireDatabase();
        using var db = _env.CreateContext();
        var repo = new SessionErrorRepository(db);
        var result = repo.Aggregate(
            since: null, until: null,
            process: "__nonexistent_process__",
            resource: null, text: null, exceptionTypes: null);
        Assert.Empty(result);
    }
}
