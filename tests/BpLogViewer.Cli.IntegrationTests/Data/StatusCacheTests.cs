using BpLogViewer.Cli.Data;
using Xunit;

namespace BpLogViewer.Cli.IntegrationTests.Data;

[Collection("BpDb")]
public class StatusCacheTests
{
    private readonly BpLogViewerEnvironment _env;

    public StatusCacheTests(BpLogViewerEnvironment env) => _env = env;

    [Fact]
    public void FreshInstance_NotLoadedAndCountZero()
    {
        var cache = new StatusCache();
        Assert.False(cache.IsLoaded);
        Assert.Equal(0, cache.Count);
    }

    [Fact]
    public void Load_EmptyConnectionString_Throws()
    {
        var cache = new StatusCache();
        var s = new BpLogViewer.Cli.Configuration.AppSettings();
        Assert.Throws<InvalidOperationException>(() => cache.Load(s));
    }

    [Fact]
    public void Load_PopulatesCacheFromBpAStatus()
    {
        _env.RequireDatabase();
        var cache = new StatusCache();
        cache.Load(_env.Settings);
        Assert.True(cache.IsLoaded);
        Assert.True(cache.Count > 0, "BP 7.5.x should have at least a few status entries");
    }

    [Fact]
    public void TryGetId_NumericString_ResolvesWhenInCache()
    {
        _env.RequireDatabase();
        var cache = new StatusCache();
        cache.Load(_env.Settings);

        // Pick any known id from the cache
        var firstId = cache.AllById.Keys.First();
        Assert.True(cache.TryGetId(firstId.ToString(), out var resolved));
        Assert.Equal(firstId, resolved);
    }

    [Fact]
    public void TryGetId_UnknownNumeric_ReturnsFalse()
    {
        _env.RequireDatabase();
        var cache = new StatusCache();
        cache.Load(_env.Settings);
        Assert.False(cache.TryGetId(int.MaxValue.ToString(), out _));
    }

    [Fact]
    public void TryGetId_Name_ResolvesToId()
    {
        _env.RequireDatabase();
        var cache = new StatusCache();
        cache.Load(_env.Settings);

        var firstEntry = cache.AllById.First();
        var name = firstEntry.Value;
        Assert.True(cache.TryGetId(name, out var id));
        Assert.Equal(firstEntry.Key, id);
    }

    [Fact]
    public void TryGetId_UnknownName_ReturnsFalse()
    {
        _env.RequireDatabase();
        var cache = new StatusCache();
        cache.Load(_env.Settings);
        Assert.False(cache.TryGetId("__definitely_not_a_status_name__", out _));
    }

    [Fact]
    public void GetName_KnownId_ReturnsName()
    {
        _env.RequireDatabase();
        var cache = new StatusCache();
        cache.Load(_env.Settings);

        var firstEntry = cache.AllById.First();
        Assert.Equal(firstEntry.Value, cache.GetName(firstEntry.Key));
    }

    [Fact]
    public void GetName_UnknownId_ReturnsNull()
    {
        _env.RequireDatabase();
        var cache = new StatusCache();
        cache.Load(_env.Settings);
        Assert.Null(cache.GetName(int.MinValue));
    }

    [Fact]
    public void AllById_IsReadOnlyView()
    {
        _env.RequireDatabase();
        var cache = new StatusCache();
        cache.Load(_env.Settings);
        Assert.NotEmpty(cache.AllById);
    }
}
