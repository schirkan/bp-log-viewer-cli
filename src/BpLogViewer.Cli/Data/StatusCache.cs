using BpLogViewer.Cli.Configuration;
using Microsoft.Data.SqlClient;

namespace BpLogViewer.Cli.Data;

/// <summary>
/// Cached die <c>BPAStatus</c>-Tabelle (Status-Codes + Beschreibungen) zur
/// Laufzeit. Ersetzt das hartkodierte Mapping aus F-002/004/005 (architecture § 4.5).
/// Spec: <c>specs/feature-F-001-db-connection.md</c> § „Runtime-Lookup".
/// </summary>
public sealed class StatusCache
{
    private readonly Dictionary<int, string> _byId = new();
    private readonly Dictionary<string, int> _byName = new(StringComparer.OrdinalIgnoreCase);
    private bool _loaded;

    public int Count => _byId.Count;
    public bool IsLoaded => _loaded;

    /// <summary>Lädt die <c>BPAStatus</c>-Tabelle aus der DB. Wirft bei Verbindungsfehler.</summary>
    public void Load(AppSettings settings)
    {
        if (_loaded) return;
        if (settings.Database.ConnectionString is null or { Length: 0 })
            throw new InvalidOperationException("Database.ConnectionString is not set");

        using var conn = new SqlConnection(settings.Database.ConnectionString);
        conn.Open();
        using var cmd = new SqlCommand("SELECT statusid, description FROM BPAStatus", conn);
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            var id = reader.GetInt32(0);
            var desc = reader.GetString(1);
            _byId[id] = desc;
            _byName[desc] = id;
        }
        _loaded = true;
    }

    /// <summary>Versucht Name ODER numerische ID aufzulösen.</summary>
    public bool TryGetId(string nameOrId, out int id)
    {
        if (int.TryParse(nameOrId, out id))
            return _byId.ContainsKey(id);
        return _byName.TryGetValue(nameOrId, out id);
    }

    public string? GetName(int id) => _byId.GetValueOrDefault(id);

    public IReadOnlyDictionary<int, string> AllById => _byId;
}
