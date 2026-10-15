using BpLogViewer.Cli.Data;
using Xunit;

namespace BpLogViewer.Cli.IntegrationTests.Data;

[Collection("BpDb")]
public class ProcessRepositoryTests
{
    private readonly BpLogViewerEnvironment _env;

    public ProcessRepositoryTests(BpLogViewerEnvironment env) => _env = env;

    [Fact]
    public void List_NoFilters_RespectsMaxRows()
    {
        _env.RequireDatabase();
        using var db = _env.CreateContext();
        var repo = new ProcessRepository(db);
        var rows = repo.List(name: null, nameContains: false, type: null, hasErrorsSince: null, maxRowsPlusOne: 5);
        Assert.NotNull(rows);
        Assert.True(rows.Count <= 5);
    }

    [Fact]
    public void List_NonExistentName_ReturnsEmpty()
    {
        _env.RequireDatabase();
        using var db = _env.CreateContext();
        var repo = new ProcessRepository(db);
        var rows = repo.List(name: "__no_such_process__", nameContains: true, type: null, hasErrorsSince: null, maxRowsPlusOne: 100);
        Assert.Empty(rows);
    }

    [Fact]
    public void List_TypeProcess_Filters()
    {
        _env.RequireDatabase();
        using var db = _env.CreateContext();
        var repo = new ProcessRepository(db);
        var rows = repo.List(name: null, nameContains: false, type: "process", hasErrorsSince: null, maxRowsPlusOne: 100);
        Assert.NotNull(rows);
        Assert.All(rows, r => Assert.Equal("process", r.Type));
    }

    [Fact]
    public void List_TypeObject_Filters()
    {
        _env.RequireDatabase();
        using var db = _env.CreateContext();
        var repo = new ProcessRepository(db);
        var rows = repo.List(name: null, nameContains: false, type: "object", hasErrorsSince: null, maxRowsPlusOne: 100);
        Assert.NotNull(rows);
        Assert.All(rows, r => Assert.Equal("object", r.Type));
    }

    [Fact]
    public void List_HasErrorsSinceInFuture_ReturnsEmpty()
    {
        _env.RequireDatabase();
        using var db = _env.CreateContext();
        var repo = new ProcessRepository(db);
        var rows = repo.List(
            name: null, nameContains: false, type: null,
            hasErrorsSince: DateTimeOffset.UtcNow.AddYears(10), maxRowsPlusOne: 100);
        Assert.Empty(rows);
    }
}
