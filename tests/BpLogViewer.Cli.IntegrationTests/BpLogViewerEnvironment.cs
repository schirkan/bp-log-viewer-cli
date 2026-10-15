using BpLogViewer.Cli.Configuration;
using BpLogViewer.Cli.Data;
using Microsoft.Data.SqlClient;
using Xunit;

namespace BpLogViewer.Cli.IntegrationTests;

/// <summary>
/// Shared test environment. Provides the connection string and an
/// <see cref="AppSettings"/> instance pre-populated with it. The connection
/// string is taken from the <c>BP_LOG_VIEWER_TEST_CONNECTION_STRING</c>
/// environment variable; if not set, a sensible LocalDB default is used.
/// <para>
/// The fixture verifies connectivity once at construction. If the DB cannot
/// be reached, <see cref="Available"/> is <c>false</c> and <see cref="SkipReason"/>
/// contains a human-readable explanation. DB-dependent tests must call
/// <see cref="RequireDatabase"/> at the start so they fail fast with a clear
/// message instead of a generic SqlException deep in the stack.
/// </para>
/// </summary>
public sealed class BpLogViewerEnvironment : IDisposable
{
    /// <summary>
    /// Default LocalDB connection string. Overridable via the
    /// <c>BP_LOG_VIEWER_TEST_CONNECTION_STRING</c> environment variable, so
    /// the same test project can target a remote SQL Server.
    /// </summary>
    public const string DefaultLocalDbConnectionString =
        @"Server=(localdb)\BluePrismLocalDB;Database=BluePrism;Integrated Security=true;TrustServerCertificate=true;Encrypt=false;";

    public string ConnectionString { get; }
    public AppSettings Settings { get; }
    public bool Available { get; }
    public string? SkipReason { get; }

    public BpLogViewerEnvironment()
    {
        ConnectionString = Environment.GetEnvironmentVariable("BP_LOG_VIEWER_TEST_CONNECTION_STRING")
            ?? DefaultLocalDbConnectionString;
        Settings = new AppSettings
        {
            Database = new DatabaseSettings { ConnectionString = ConnectionString }
        };

        try
        {
            using var conn = new SqlConnection(ConnectionString);
            conn.Open();
            Available = true;
        }
        catch (Exception ex)
        {
            Available = false;
            SkipReason =
                $"Cannot connect to test database ({ex.GetType().Name}: {ex.Message}). " +
                "Set the BP_LOG_VIEWER_TEST_CONNECTION_STRING environment variable to a valid " +
                "Blue Prism database connection string and re-run.";
        }
    }

    /// <summary>
    /// Throws an <see cref="InvalidOperationException"/> with <see cref="SkipReason"/>
    /// if the database is not available. DB-dependent tests should call this
    /// at the start so they fail with a clear message rather than a generic
    /// connection error.
    /// </summary>
    public void RequireDatabase()
    {
        if (!Available)
        {
            throw new InvalidOperationException(SkipReason);
        }
    }

    public BpDbContext CreateContext() => new(Settings);

    public void Dispose()
    {
        // No persistent resources to clean up; the fixture is a thin
        // configuration holder.
    }
}

[CollectionDefinition("BpDb")]
public class BpDbCollection : ICollectionFixture<BpLogViewerEnvironment>
{
}
