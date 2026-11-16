using System.Data;
using BpLogViewer.Cli.Models;
using Microsoft.Data.SqlClient;

namespace BpLogViewer.Cli.Data;

/// <summary>
/// Liest Environment Variables und ihre Zuordnung zu Prozessen.
/// </summary>
public sealed class EnvironmentRepository
{
    private readonly BpDbContext _context;

    public EnvironmentRepository(BpDbContext context)
    {
        _context = context;
    }

    public List<EnvironmentVarInfo> ListVars(string? name, bool nameContains, int maxRowsPlusOne)
    {
        using var conn = _context.CreateConnection();
        conn.Open();
        using var cmd = BuildVarsCommand(conn, name, nameContains, maxRowsPlusOne);
        using var reader = cmd.ExecuteReader();

        var rows = new List<EnvironmentVarInfo>(maxRowsPlusOne);
        while (reader.Read())
        {
            rows.Add(new EnvironmentVarInfo(
                Id: reader.IsDBNull(0) ? Guid.Empty : reader.GetGuid(0),
                Name: reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                Datatype: reader.IsDBNull(2) ? null : reader.GetString(2),
                Value: reader.IsDBNull(3) ? null : reader.GetString(3),
                Description: reader.IsDBNull(4) ? null : reader.GetString(4)));
        }

        return rows;
    }

    public List<EnvironmentVarDependencyInfo> ListVarDependencies(string? processName, string? variableName, int maxRowsPlusOne)
    {
        using var conn = _context.CreateConnection();
        conn.Open();
        using var cmd = BuildDependenciesCommand(conn, processName, variableName, maxRowsPlusOne);
        using var reader = cmd.ExecuteReader();

        var rows = new List<EnvironmentVarDependencyInfo>(maxRowsPlusOne);
        while (reader.Read())
        {
            rows.Add(new EnvironmentVarDependencyInfo(
                Id: reader.IsDBNull(0) ? 0 : reader.GetInt32(0),
                ProcessId: reader.IsDBNull(1) ? Guid.Empty : reader.GetGuid(1),
                ProcessName: reader.IsDBNull(2) || reader.GetString(2).Length == 0 ? null : reader.GetString(2),
                VariableName: reader.IsDBNull(3) ? string.Empty : reader.GetString(3)));
        }

        return rows;
    }

    private static SqlCommand BuildVarsCommand(SqlConnection conn, string? name, bool nameContains, int maxRowsPlusOne)
    {
        // "value" ist ein reserviertes SQL-Server-Wort und muss quotiert werden.
        const string sql = @"
            SELECT TOP (@max_rows)
                   v.Id,
                   v.name,
                   v.datatype,
                   v.[value],
                   v.description
            FROM BPAEnvironmentVar v
            WHERE (@name IS NULL OR v.name LIKE @name_pattern)
            ORDER BY v.name ASC;";

        var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.Add(new SqlParameter("@max_rows", SqlDbType.Int) { Value = maxRowsPlusOne });
        cmd.Parameters.Add(new SqlParameter("@name", SqlDbType.NVarChar, 256) { Value = (object?)name ?? DBNull.Value });
        var namePattern = name is null
            ? (object)DBNull.Value
            : (nameContains ? $"%{name}%" : $"{name}%");
        cmd.Parameters.Add(new SqlParameter("@name_pattern", SqlDbType.NVarChar, 260) { Value = namePattern });
        return cmd;
    }

    private static SqlCommand BuildDependenciesCommand(SqlConnection conn, string? processName, string? variableName, int maxRowsPlusOne)
    {
        const string sql = @"
            SELECT TOP (@max_rows)
                   d.id,
                   d.processID,
                   ISNULL(p.name, '') AS process_name,
                   d.refVariableName
            FROM BPAProcessEnvironmentVarDependency d
            LEFT JOIN BPAProcess p ON p.processid = d.processID
            WHERE (@process_name IS NULL OR p.name LIKE @process_name_pattern)
              AND (@variable_name IS NULL OR d.refVariableName LIKE @variable_name_pattern)
            ORDER BY p.name ASC, d.refVariableName ASC;";

        var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.Add(new SqlParameter("@max_rows", SqlDbType.Int) { Value = maxRowsPlusOne });
        cmd.Parameters.Add(new SqlParameter("@process_name", SqlDbType.NVarChar, 256) { Value = (object?)processName ?? DBNull.Value });
        cmd.Parameters.Add(new SqlParameter("@process_name_pattern", SqlDbType.NVarChar, 260) { Value = Pattern(processName) });
        cmd.Parameters.Add(new SqlParameter("@variable_name", SqlDbType.NVarChar, 256) { Value = (object?)variableName ?? DBNull.Value });
        cmd.Parameters.Add(new SqlParameter("@variable_name_pattern", SqlDbType.NVarChar, 260) { Value = Pattern(variableName) });
        return cmd;
    }

    private static object Pattern(string? value)
        => value is null ? DBNull.Value : $"{value}%";
}
