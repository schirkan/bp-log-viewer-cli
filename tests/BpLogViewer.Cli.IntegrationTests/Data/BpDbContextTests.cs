using BpLogViewer.Cli.Configuration;
using BpLogViewer.Cli.Data;
using System.Data;
using Microsoft.Data.SqlClient;
using Xunit;

namespace BpLogViewer.Cli.IntegrationTests.Data;

[Collection("BpDb")]
public class BpDbContextTests
{
    private readonly BpLogViewerEnvironment _env;

    public BpDbContextTests(BpLogViewerEnvironment env) => _env = env;

    [Fact]
    public void Constructor_EmptyConnectionString_Throws()
    {
        var settings = new AppSettings
        {
            Database = new DatabaseSettings { ConnectionString = "" }
        };
        Assert.Throws<InvalidOperationException>(() => new BpDbContext(settings));
    }

    [Fact]
    public void Constructor_NullConnectionString_Throws()
    {
        var settings = new AppSettings
        {
            Database = new DatabaseSettings { ConnectionString = null }
        };
        Assert.Throws<InvalidOperationException>(() => new BpDbContext(settings));
    }

    [Fact]
    public void CreateConnection_ReturnsNewConnectionInstance()
    {
        _env.RequireDatabase();
        using var ctx = _env.CreateContext();
        using var conn = ctx.CreateConnection();
        Assert.NotNull(conn);
        Assert.Equal(ConnectionState.Closed, conn.State);
    }

    [Fact]
    public void CreateConnection_CanBeOpenedAndClosed()
    {
        _env.RequireDatabase();
        using var ctx = _env.CreateContext();
        using var conn = ctx.CreateConnection();
        conn.Open();
        Assert.Equal(ConnectionState.Open, conn.State);
        conn.Close();
        Assert.Equal(ConnectionState.Closed, conn.State);
    }

    [Fact]
    public void Dispose_SetsDisposedFlag()
    {
        _env.RequireDatabase();
        var ctx = _env.CreateContext();
        ctx.Dispose();
        Assert.Throws<ObjectDisposedException>(() => ctx.CreateConnection());
    }

    [Fact]
    public void Dispose_IsIdempotent()
    {
        _env.RequireDatabase();
        var ctx = _env.CreateContext();
        ctx.Dispose();
        ctx.Dispose(); // must not throw
    }
}
