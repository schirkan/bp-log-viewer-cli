using System.Data;
using BpLogViewer.Cli.Models;
using Microsoft.Data.SqlClient;

namespace BpLogViewer.Cli.Data;

public sealed class ProcessRepository
{
    private readonly BpDbContext _context;

    public ProcessRepository(BpDbContext context)
    {
        _context = context;
    }

    public List<ProcessInfo> List(string? name, bool nameContains, string? type, DateTimeOffset? hasErrorsSince, int maxRowsPlusOne)
    {
        using var conn = _context.CreateConnection();
        conn.Open();
        using var cmd = BuildListCommand(conn, name, nameContains, type, hasErrorsSince, maxRowsPlusOne);
        using var reader = cmd.ExecuteReader();

        var rows = new List<ProcessInfo>(maxRowsPlusOne);
        while (reader.Read())
        {
            rows.Add(new ProcessInfo(
                ProcessId: reader.GetGuid(0),
                Name: reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                Type: reader.IsDBNull(2) ? null : MapType(reader.GetString(2)),
                Version: reader.IsDBNull(3) ? null : reader.GetString(3),
                LastModified: reader.IsDBNull(4) ? null : DateTimeUtils.AssumeUtc(reader.GetDateTime(4)),
                LastSessionNumber: reader.IsDBNull(5) ? null : (int?)reader.GetInt32(5),
                LastSessionStartedAt: reader.IsDBNull(6) ? null : DateTimeUtils.AssumeUtc(reader.GetDateTime(6)),
                SessionCountTotal: reader.IsDBNull(7) ? 0 : reader.GetInt32(7),
                SessionCountFailedSince: reader.IsDBNull(8) ? 0 : reader.GetInt32(8),
                AvgDurationSec: reader.IsDBNull(9) ? null : (double?)Convert.ToDouble(reader.GetValue(9), System.Globalization.CultureInfo.InvariantCulture)));
        }

        return rows;
    }

    private static SqlCommand BuildListCommand(SqlConnection conn, string? name, bool nameContains, string? type, DateTimeOffset? hasErrorsSince, int maxRowsPlusOne)
    {
        const string sql = @"
            SELECT TOP (@max_rows)
                   p.processid,
                   p.name,
                   p.ProcessType,
                   p.version,
                   p.lastmodifieddate,
                   last_s.sessionnumber AS last_session_number,
                   last_s.startdatetime AS last_session_started_at,
                   ISNULL(total_s.session_count_total, 0) AS session_count_total,
                   ISNULL(failed_s.session_count_failed_since, 0) AS session_count_failed_since,
                   avg_s.avg_duration_sec
            FROM BPAProcess p
            LEFT JOIN (
                SELECT processid, sessionnumber, startdatetime,
                       ROW_NUMBER() OVER (PARTITION BY processid ORDER BY startdatetime DESC) AS rn
                FROM BPASession
            ) last_s ON last_s.processid = p.processid AND last_s.rn = 1
            LEFT JOIN (
                SELECT processid, COUNT(*) AS session_count_total
                FROM BPASession
                GROUP BY processid
            ) total_s ON total_s.processid = p.processid
            LEFT JOIN (
                SELECT processid, COUNT(*) AS session_count_failed_since
                FROM BPASession
                WHERE exceptiontype IS NOT NULL
                  AND (@has_errors_since IS NULL OR startdatetime >= @has_errors_since)
                GROUP BY processid
            ) failed_s ON failed_s.processid = p.processid
            LEFT JOIN (
                SELECT processid, AVG(CONVERT(float, DATEDIFF(SECOND, startdatetime, enddatetime))) AS avg_duration_sec
                FROM BPASession
                WHERE enddatetime IS NOT NULL
                GROUP BY processid
            ) avg_s ON avg_s.processid = p.processid
            WHERE (@name IS NULL OR p.name LIKE @name_pattern)
              AND (@ptype IS NULL OR p.ProcessType = @ptype)
              AND (@has_errors_since IS NULL OR ISNULL(failed_s.session_count_failed_since, 0) > 0)
            ORDER BY p.name ASC;";

        var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.Add(new SqlParameter("@max_rows", SqlDbType.Int) { Value = maxRowsPlusOne });
        cmd.Parameters.Add(new SqlParameter("@name", SqlDbType.NVarChar, 256) { Value = (object?)name ?? DBNull.Value });
        var namePattern = name is null
            ? (object)DBNull.Value
            : (nameContains ? $"%{name}%" : $"{name}%");
        cmd.Parameters.Add(new SqlParameter("@name_pattern", SqlDbType.NVarChar, 260) { Value = namePattern });
        cmd.Parameters.Add(new SqlParameter("@ptype", SqlDbType.NVarChar, 1) { Value = (object?)ToTypeCode(type) ?? DBNull.Value });
        cmd.Parameters.Add(new SqlParameter("@has_errors_since", SqlDbType.DateTimeOffset) { Value = (object?)hasErrorsSince ?? DBNull.Value });
        return cmd;
    }

    private static string? ToTypeCode(string? type)
    {
        if (string.IsNullOrWhiteSpace(type)) return null;
        return type.Equals("object", StringComparison.OrdinalIgnoreCase) ? "O"
            : type.Equals("process", StringComparison.OrdinalIgnoreCase) ? "P"
            : type;
    }

    private static string? MapType(string code)
        => code.Equals("O", StringComparison.OrdinalIgnoreCase) ? "object"
         : code.Equals("P", StringComparison.OrdinalIgnoreCase) ? "process"
         : code;

}
