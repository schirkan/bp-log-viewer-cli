using BpLogViewer.Cli.Data;
using Xunit;

namespace BpLogViewer.Cli.IntegrationTests.Data;

[Collection("BpDb")]
public class SessionRepositoryTests
{
    private readonly BpLogViewerEnvironment _env;

    public SessionRepositoryTests(BpLogViewerEnvironment env) => _env = env;

    [Fact]
    public void WorkQueueItemsLimit_IsPositive()
    {
        Assert.True(SessionRepository.WorkQueueItemsLimit > 0);
    }

    [Fact]
    public void List_NoFilters_ReturnsAtMostMaxRows()
    {
        _env.RequireDatabase();
        using var db = _env.CreateContext();
        var repo = new SessionRepository(db);
        var rows = repo.List(
            since: null, until: null, process: null, resource: null,
            statusIds: null, hasException: false, maxRowsPlusOne: 10);
        Assert.NotNull(rows);
        Assert.True(rows.Count <= 10);
    }

    [Fact]
    public void List_WithSinceInDistantPast_StillRespectsMaxRows()
    {
        _env.RequireDatabase();
        using var db = _env.CreateContext();
        var repo = new SessionRepository(db);
        var rows = repo.List(
            since: DateTimeOffset.UtcNow.AddYears(-10),
            until: null,
            process: null,
            resource: null,
            statusIds: null,
            hasException: false,
            maxRowsPlusOne: 5);
        Assert.NotNull(rows);
        Assert.True(rows.Count <= 5);
    }

    [Fact]
    public void List_WithSinceInFuture_ReturnsEmpty()
    {
        _env.RequireDatabase();
        using var db = _env.CreateContext();
        var repo = new SessionRepository(db);
        var rows = repo.List(
            since: DateTimeOffset.UtcNow.AddYears(10),
            until: null,
            process: null,
            resource: null,
            statusIds: null,
            hasException: false,
            maxRowsPlusOne: 100);
        Assert.Empty(rows);
    }

    [Fact]
    public void List_WithStatusFilter_ReturnsOnlyMatching()
    {
        _env.RequireDatabase();
        using var db = _env.CreateContext();
        var repo = new SessionRepository(db);
        var rows = repo.List(
            since: null, until: null, process: null, resource: null,
            statusIds: new[] { 0 }, // likely a real status code
            hasException: false, maxRowsPlusOne: 5);
        Assert.NotNull(rows);
        // Every returned row has Status == 0
        Assert.All(rows, r => Assert.Equal(0, r.Status));
    }

    [Fact]
    public void List_WithHasExceptionFilter_ReturnsOnlyExceptions()
    {
        _env.RequireDatabase();
        using var db = _env.CreateContext();
        var repo = new SessionRepository(db);
        var rows = repo.List(
            since: null, until: null, process: null, resource: null,
            statusIds: null, hasException: true, maxRowsPlusOne: 5);
        Assert.NotNull(rows);
        Assert.All(rows, r => Assert.NotNull(r.ExceptionType));
    }

    [Fact]
    public void List_WithProcessFilter_NoMatchReturnsEmpty()
    {
        _env.RequireDatabase();
        using var db = _env.CreateContext();
        var repo = new SessionRepository(db);
        var rows = repo.List(
            since: null, until: null,
            process: "__nonexistent_process__",
            resource: null, statusIds: null, hasException: false, maxRowsPlusOne: 5);
        Assert.Empty(rows);
    }

    [Fact]
    public void GetBySessionNumber_NotFound_ReturnsNull()
    {
        _env.RequireDatabase();
        using var db = _env.CreateContext();
        var repo = new SessionRepository(db);
        var result = repo.GetBySessionNumber(int.MaxValue, includeStateXml: false);
        Assert.Null(result);
    }

    [Fact]
    public void GetBySessionId_NotFound_ReturnsNull()
    {
        _env.RequireDatabase();
        using var db = _env.CreateContext();
        var repo = new SessionRepository(db);
        var result = repo.GetBySessionId(Guid.NewGuid(), includeStateXml: false);
        Assert.Null(result);
    }
}
