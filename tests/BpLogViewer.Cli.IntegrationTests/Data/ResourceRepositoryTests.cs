using BpLogViewer.Cli.Data;
using Xunit;

namespace BpLogViewer.Cli.IntegrationTests.Data;

[Collection("BpDb")]
public class ResourceRepositoryTests
{
    private readonly BpLogViewerEnvironment _env;

    public ResourceRepositoryTests(BpLogViewerEnvironment env) => _env = env;

    [Fact]
    public void List_NoFilters_RespectsMaxRows()
    {
        _env.RequireDatabase();
        using var db = _env.CreateContext();
        var repo = new ResourceRepository(db);
        var rows = repo.List(name: null, nameContains: false, statusIds: null, statusNames: null, maxRowsPlusOne: 5);
        Assert.NotNull(rows);
        Assert.True(rows.Count <= 5);
    }

    [Fact]
    public void List_NonExistentName_ReturnsEmpty()
    {
        _env.RequireDatabase();
        using var db = _env.CreateContext();
        var repo = new ResourceRepository(db);
        var rows = repo.List(
            name: "__no_such_resource__",
            nameContains: true, statusIds: null, statusNames: null, maxRowsPlusOne: 100);
        Assert.Empty(rows);
    }

    [Fact]
    public void List_ByStatusId_ReturnsOnlyMatching()
    {
        _env.RequireDatabase();
        using var db = _env.CreateContext();
        var repo = new ResourceRepository(db);
        var rows = repo.List(
            name: null, nameContains: false,
            statusIds: new[] { 0 }, statusNames: null, maxRowsPlusOne: 5);
        Assert.NotNull(rows);
        Assert.All(rows, r => Assert.Equal(0, r.StatusId));
    }

    [Fact]
    public void List_ByStatusName_NonExistent_ReturnsEmpty()
    {
        _env.RequireDatabase();
        using var db = _env.CreateContext();
        var repo = new ResourceRepository(db);
        var rows = repo.List(
            name: null, nameContains: false,
            statusIds: null, statusNames: new[] { "__no_such_status__" }, maxRowsPlusOne: 100);
        Assert.Empty(rows);
    }
}
