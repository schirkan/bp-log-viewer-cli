using BpLogViewer.Cli.Data;
using Xunit;

namespace BpLogViewer.Cli.IntegrationTests.Data;

[Collection("BpDb")]
public class QueueRepositoryTests
{
    private readonly BpLogViewerEnvironment _env;

    public QueueRepositoryTests(BpLogViewerEnvironment env) => _env = env;

    [Fact]
    public void ListQueues_NoFilters_RespectsMaxRows()
    {
        _env.RequireDatabase();
        using var db = _env.CreateContext();
        var repo = new QueueRepository(db);
        var rows = repo.ListQueues(name: null, nameContains: false, onlyExceptions: false, onlyRunning: false, maxRowsPlusOne: 5);
        Assert.NotNull(rows);
        Assert.True(rows.Count <= 5);
    }

    [Fact]
    public void ListQueues_NonExistentName_ReturnsEmpty()
    {
        _env.RequireDatabase();
        using var db = _env.CreateContext();
        var repo = new QueueRepository(db);
        var rows = repo.ListQueues(name: "__no_such_queue__", nameContains: true, onlyExceptions: false, onlyRunning: false, maxRowsPlusOne: 100);
        Assert.Empty(rows);
    }

    [Fact]
    public void ListQueues_OnlyRunning_Filters()
    {
        _env.RequireDatabase();
        using var db = _env.CreateContext();
        var repo = new QueueRepository(db);
        var rows = repo.ListQueues(name: null, nameContains: false, onlyExceptions: false, onlyRunning: true, maxRowsPlusOne: 100);
        Assert.NotNull(rows);
        Assert.All(rows, r => Assert.True(r.Running));
    }

    [Fact]
    public void ListQueues_OnlyExceptions_Filters()
    {
        _env.RequireDatabase();
        using var db = _env.CreateContext();
        var repo = new QueueRepository(db);
        var rows = repo.ListQueues(name: null, nameContains: false, onlyExceptions: true, onlyRunning: false, maxRowsPlusOne: 100);
        Assert.NotNull(rows);
        Assert.All(rows, r => Assert.True(r.ItemCountException > 0));
    }

    [Fact]
    public void ResolveQueueIdByName_NonExistent_ReturnsNull()
    {
        _env.RequireDatabase();
        using var db = _env.CreateContext();
        var repo = new QueueRepository(db);
        Assert.Null(repo.ResolveQueueIdByName("__no_such_queue__"));
    }

    [Fact]
    public void ListItems_NonExistentQueueId_ReturnsEmpty()
    {
        _env.RequireDatabase();
        using var db = _env.CreateContext();
        var repo = new QueueRepository(db);
        var rows = repo.ListItems(
            queueId: Guid.NewGuid(),
            statuses: null, since: null, until: null, text: null,
            fullData: false, includeTags: false, maxRowsPlusOne: 100);
        Assert.Empty(rows);
    }

    [Fact]
    public void ListItems_FutureSince_ReturnsEmpty()
    {
        _env.RequireDatabase();
        using var db = _env.CreateContext();
        var repo = new QueueRepository(db);
        var rows = repo.ListItems(
            queueId: Guid.NewGuid(),
            statuses: null,
            since: DateTimeOffset.UtcNow.AddYears(10),
            until: null, text: null,
            fullData: false, includeTags: false, maxRowsPlusOne: 100);
        Assert.Empty(rows);
    }
}
