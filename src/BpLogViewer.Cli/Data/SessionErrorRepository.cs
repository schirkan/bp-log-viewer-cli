using System.Data;
using BpLogViewer.Cli.Models;
using Microsoft.Data.SqlClient;

namespace BpLogViewer.Cli.Data;

public sealed class SessionErrorRepository
{
    private readonly BpDbContext _context;

    public SessionErrorRepository(BpDbContext context)
    {
        _context = context;
    }

    public List<SessionInfo> List(
        DateTimeOffset? since,
        DateTimeOffset? until,
        string? process,
        string? resource,
        string? text,
        string[]? exceptionTypes,
        int maxRowsPlusOne)
    {
        using var conn = _context.CreateConnection();
        conn.Open();
        using var cmd = BuildListCommand(conn, since, until, process, resource, text, exceptionTypes, maxRowsPlusOne);
        using var reader = cmd.ExecuteReader();

        var results = new List<SessionInfo>(maxRowsPlusOne);
        while (reader.Read())
        {
            var startedAt = ReadDateTimeOffset(reader, 7, 8);
            DateTimeOffset? endedAt = reader.IsDBNull(9) ? null : ReadDateTimeOffset(reader, 9, 10);
            int? durationSec = endedAt.HasValue ? (int)(endedAt.Value - startedAt).TotalSeconds : null;

            results.Add(new SessionInfo(
                SessionNumber: reader.GetInt32(0),
                SessionId: reader.GetGuid(1),
                Process: reader.IsDBNull(2) ? null : reader.GetString(2),
                ProcessId: reader.IsDBNull(3) ? null : (Guid?)reader.GetGuid(3),
                Resource: reader.IsDBNull(4) ? null : reader.GetString(4),
                ResourceId: reader.IsDBNull(5) ? null : (Guid?)reader.GetGuid(5),
                Status: reader.GetInt32(6),
                StatusName: null,
                StartedAt: startedAt,
                EndedAt: endedAt,
                DurationSec: durationSec,
                LastStage: reader.IsDBNull(11) ? null : reader.GetString(11),
                ExceptionType: reader.IsDBNull(12) ? null : reader.GetString(12),
                ExceptionMessage: reader.IsDBNull(13) ? null : reader.GetString(13)));
        }

        return results;
    }

    private static SqlCommand BuildListCommand(
        SqlConnection conn,
        DateTimeOffset? since,
        DateTimeOffset? until,
        string? process,
        string? resource,
        string? text,
        string[]? exceptionTypes,
        int maxRowsPlusOne)
    {
        const string sql = @"
            SELECT sessionnumber, sessionid, processname, processid,
                   runningresourcename, runningresourceid,
                   statusid,
                   startdatetime, starttimezoneoffset,
                   enddatetime, endtimezoneoffset,
                   laststage, exceptiontype, exceptionmessage
            FROM BPVSessionInfo
            WHERE exceptiontype IS NOT NULL
              AND (@since  IS NULL OR startdatetime >= @since)
              AND (@until  IS NULL OR startdatetime <  @until)
              AND (@process IS NULL OR processname = @process)
              AND (@resource IS NULL OR runningresourcename = @resource)
              AND (@text IS NULL OR exceptionmessage LIKE '%' + @text + '%')
              AND (@ex_active = 0 OR exceptiontype IN (SELECT value FROM STRING_SPLIT(@ex_csv, ',')))
            ORDER BY startdatetime DESC
            OFFSET 0 ROWS FETCH NEXT @max_rows ROWS ONLY;";

        var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.Add(new SqlParameter("@since", SqlDbType.DateTimeOffset) { Value = (object?)since ?? DBNull.Value });
        cmd.Parameters.Add(new SqlParameter("@until", SqlDbType.DateTimeOffset) { Value = (object?)until ?? DBNull.Value });
        cmd.Parameters.Add(new SqlParameter("@process", SqlDbType.NVarChar, 128) { Value = (object?)process ?? DBNull.Value });
        cmd.Parameters.Add(new SqlParameter("@resource", SqlDbType.NVarChar, 128) { Value = (object?)resource ?? DBNull.Value });
        cmd.Parameters.Add(new SqlParameter("@text", SqlDbType.NVarChar, -1) { Value = (object?)text ?? DBNull.Value });

        var exActive = exceptionTypes is { Length: > 0 };
        var exCsv = exActive ? string.Join(",", exceptionTypes!) : null;
        cmd.Parameters.Add(new SqlParameter("@ex_active", SqlDbType.Bit) { Value = exActive });
        cmd.Parameters.Add(new SqlParameter("@ex_csv", SqlDbType.NVarChar, -1) { Value = (object?)exCsv ?? DBNull.Value });
        cmd.Parameters.Add(new SqlParameter("@max_rows", SqlDbType.Int) { Value = maxRowsPlusOne });
        return cmd;
    }

    private static DateTimeOffset ReadDateTimeOffset(SqlDataReader reader, int dtIndex, int offsetIndex)
    {
        var dt = reader.GetDateTime(dtIndex);
        var rawOffset = Convert.ToInt32(reader.GetValue(offsetIndex), System.Globalization.CultureInfo.InvariantCulture);
        var offsetMinutes = Math.Abs(rawOffset) > 14 * 60 ? rawOffset / 60 : rawOffset;
        return new DateTimeOffset(dt, TimeSpan.FromMinutes(offsetMinutes));
    }
}
