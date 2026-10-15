using BpLogViewer.Cli.Configuration;
using Microsoft.Data.SqlClient;

namespace BpLogViewer.Cli.Data;

/// <summary>
/// Factory für <see cref="SqlConnection"/>. Wird pro Sub-Command einmal
/// instanziert (Connection-Pooling übernimmt Microsoft.Data.SqlClient).
/// </summary>
public sealed class BpDbContext : IDisposable
{
    private readonly string _connectionString;
    private bool _disposed;

    public BpDbContext(AppSettings settings)
    {
        if (settings.Database.ConnectionString is null or { Length: 0 })
            throw new InvalidOperationException(
                "Database.ConnectionString is not set. Configure via appsettings.json, " +
                "ENV BP_DB_CONNECTION, or --connection-string.");
        _connectionString = settings.Database.ConnectionString;
    }

    /// <summary>Erzeugt eine neue <see cref="SqlConnection"/> (Aufrufer muss öffnen/schließen).</summary>
    public SqlConnection CreateConnection()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return new SqlConnection(_connectionString);
    }

    public void Dispose() => _disposed = true;
}
