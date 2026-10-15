using BpLogViewer.Cli.Data;
using Xunit;

namespace BpLogViewer.Cli.IntegrationTests.Data;

[Collection("BpDb")]
public class AuditRepositoryTests
{
    private readonly BpLogViewerEnvironment _env;

    public AuditRepositoryTests(BpLogViewerEnvironment env) => _env = env;

    [Fact]
    public void List_FutureSince_ReturnsEmpty()
    {
        _env.RequireDatabase();
        using var db = _env.CreateContext();
        var repo = new AuditRepository(db);
        var rows = repo.List(
            since: DateTimeOffset.UtcNow.AddYears(10),
            until: null, codes: null, user: null,
            targetProcess: null, targetResource: null,
            includeXml: false, fullComments: false, maxRowsPlusOne: 100);
        Assert.Empty(rows);
    }

    [Fact]
    public void List_RespectsMaxRows()
    {
        _env.RequireDatabase();
        using var db = _env.CreateContext();
        var repo = new AuditRepository(db);
        var rows = repo.List(
            since: null, until: null, codes: null, user: null,
            targetProcess: null, targetResource: null,
            includeXml: false, fullComments: false, maxRowsPlusOne: 5);
        Assert.NotNull(rows);
        Assert.True(rows.Count <= 5);
    }

    [Fact]
    public void List_NonExistentUser_ReturnsEmpty()
    {
        _env.RequireDatabase();
        using var db = _env.CreateContext();
        var repo = new AuditRepository(db);
        var rows = repo.List(
            since: null, until: null, codes: null,
            user: "__no_such_user__",
            targetProcess: null, targetResource: null,
            includeXml: false, fullComments: false, maxRowsPlusOne: 100);
        Assert.Empty(rows);
    }

    [Fact]
    public void List_NonExistentCode_ReturnsEmpty()
    {
        _env.RequireDatabase();
        using var db = _env.CreateContext();
        var repo = new AuditRepository(db);
        var rows = repo.List(
            since: null, until: null,
            codes: new[] { "__NO_SUCH_CODE__" },
            user: null, targetProcess: null, targetResource: null,
            includeXml: false, fullComments: false, maxRowsPlusOne: 100);
        Assert.Empty(rows);
    }

    [Fact]
    public void List_IncludeXml_DoesNotThrow()
    {
        _env.RequireDatabase();
        using var db = _env.CreateContext();
        var repo = new AuditRepository(db);
        var rows = repo.List(
            since: null, until: null, codes: null, user: null,
            targetProcess: null, targetResource: null,
            includeXml: true, fullComments: true, maxRowsPlusOne: 5);
        Assert.NotNull(rows);
    }
}
