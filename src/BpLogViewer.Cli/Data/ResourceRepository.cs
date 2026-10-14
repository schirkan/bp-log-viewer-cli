using System.Data;
using BpLogViewer.Cli.Models;
using Microsoft.Data.SqlClient;

namespace BpLogViewer.Cli.Data;

public sealed class ResourceRepository
{
    private readonly BpDbContext _context;

    public ResourceRepository(BpDbContext context)
    {
        _context = context;
    }

    public List<ResourceInfo> List(string? name, bool nameContains, int[]? statusIds, string[]? statusNames, int maxRowsPlusOne)
    {
        using var conn = _context.CreateConnection();
        conn.Open();
        using var cmd = BuildListCommand(conn, name, nameContains, statusIds, statusNames, maxRowsPlusOne);
        using var reader = cmd.ExecuteReader();

        var rows = new List<ResourceInfo>(maxRowsPlusOne);
        while (reader.Read())
        {
            rows.Add(new ResourceInfo(
                ResourceId: reader.GetGuid(0),
                Name: reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                DisplayStatus: reader.IsDBNull(2) ? null : reader.GetString(2),
                StatusId: reader.IsDBNull(3) ? 0 : reader.GetInt32(3),
                ProcessesRunning: reader.IsDBNull(4) ? null : (int?)reader.GetInt32(4),
                ActionsRunning: reader.IsDBNull(5) ? null : (int?)reader.GetInt32(5),
                LastUpdated: reader.IsDBNull(6) ? null : DateTimeUtils.AssumeUtc(reader.GetDateTime(6)),
                LastLoggedIn: reader.IsDBNull(7) ? null : DateTimeUtils.AssumeUtc(reader.GetDateTime(7)),
                CurrentSessionNumber: reader.IsDBNull(8) ? null : (int?)reader.GetInt32(8),
                CurrentSessionStartedAt: reader.IsDBNull(9) ? null : DateTimeUtils.AssumeUtc(reader.GetDateTime(9)),
                Fqdn: reader.IsDBNull(10) ? null : reader.GetString(10)));
        }

        return rows;
    }

    private static SqlCommand BuildListCommand(SqlConnection conn, string? name, bool nameContains, int[]? statusIds, string[]? statusNames, int maxRowsPlusOne)
    {
        const string sql = @"
            SELECT TOP (@max_rows)
                   r.resourceid,
                   r.name,
                   r.DisplayStatus,
                   r.statusid,
                   r.processesrunning,
                   r.actionsrunning,
                   r.lastupdated,
                   r.LastTimeLoggedIn,
                   cur.sessionnumber AS current_session_number,
                   cur.startdatetime AS current_session_started_at,
                   r.FQDN
            FROM BPAResource r
            LEFT JOIN (
                SELECT runningresourceid, sessionnumber, startdatetime
                FROM (
                    SELECT runningresourceid, sessionnumber, startdatetime,
                           ROW_NUMBER() OVER (PARTITION BY runningresourceid ORDER BY startdatetime DESC) AS rn
                    FROM BPASession
                    WHERE enddatetime IS NULL
                ) ranked
                WHERE rn = 1
            ) cur ON cur.runningresourceid = r.resourceid
            WHERE (@name IS NULL OR r.name LIKE @name_pattern)
              AND (
                    (@status_id_active = 0 AND @status_name_active = 0)
                    OR (@status_id_active = 1 AND r.statusid IN (SELECT CAST(value AS INT) FROM STRING_SPLIT(@status_id_csv, ',')))
                    OR (@status_name_active = 1 AND r.DisplayStatus IN (SELECT value FROM STRING_SPLIT(@status_name_csv, ',')))
                  )
            ORDER BY r.name ASC;";

        var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.Add(new SqlParameter("@max_rows", SqlDbType.Int) { Value = maxRowsPlusOne });
        cmd.Parameters.Add(new SqlParameter("@name", SqlDbType.NVarChar, 256) { Value = (object?)name ?? DBNull.Value });
        var namePattern = name is null
            ? (object)DBNull.Value
            : (nameContains ? $"%{name}%" : $"{name}%");
        cmd.Parameters.Add(new SqlParameter("@name_pattern", SqlDbType.NVarChar, 260) { Value = namePattern });

        var hasIdFilter = statusIds is { Length: > 0 };
        var hasNameFilter = statusNames is { Length: > 0 };
        cmd.Parameters.Add(new SqlParameter("@status_id_active", SqlDbType.Bit) { Value = hasIdFilter });
        cmd.Parameters.Add(new SqlParameter("@status_name_active", SqlDbType.Bit) { Value = hasNameFilter });
        cmd.Parameters.Add(new SqlParameter("@status_id_csv", SqlDbType.NVarChar, -1) { Value = hasIdFilter ? string.Join(",", statusIds!) : (object)DBNull.Value });
        cmd.Parameters.Add(new SqlParameter("@status_name_csv", SqlDbType.NVarChar, -1) { Value = hasNameFilter ? string.Join(",", statusNames!) : (object)DBNull.Value });
        return cmd;
    }

}
