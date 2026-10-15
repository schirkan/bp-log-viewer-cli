using System.Data;
using System.Globalization;
using BpLogViewer.Cli.Models;
using Microsoft.Data.SqlClient;

namespace BpLogViewer.Cli.Data;

/// <summary>
/// Read-Repository für Session-Errors mit optionaler SQL-Aggregation.
/// </summary>
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
            results.Add(SessionReader.ReadSessionInfo(reader));
        }
        return results;
    }

    /// <summary>
    /// SQL-Aggregation pro Exception-Typ: Count, MIN/MAX-Zeitstempel, distinct Processes/Resources,
    /// Sample-Session-Numbers (max 5) und Sample-Message-Preview (200 Zeichen).
    /// Verhindert OOM, das beim LINQ-GroupBy über max+1 Rows entstehen würde.
    /// </summary>
    public List<SessionErrorAggregate> Aggregate(
        DateTimeOffset? since,
        DateTimeOffset? until,
        string? process,
        string? resource,
        string? text,
        string[]? exceptionTypes)
    {
        const string Sql = @"
            WITH agg AS (
                SELECT exceptiontype,
                       COUNT(*) AS cnt,
                       MIN(startdatetime) AS first_seen,
                       MAX(startdatetime) AS last_seen
                FROM BPVSessionInfo
                WHERE exceptiontype IS NOT NULL
                  AND (@since  IS NULL OR startdatetime >= @since)
                  AND (@until  IS NULL OR startdatetime <  @until)
                  AND (@process IS NULL OR processname = @process)
                  AND (@resource IS NULL OR runningresourcename = @resource)
                  AND (@text IS NULL OR exceptionmessage LIKE '%' + @text + '%')
                  AND (@ex_active = 0 OR exceptiontype IN (SELECT value FROM STRING_SPLIT(@ex_csv, ',')))
                GROUP BY exceptiontype
            ),
            ranked_samples AS (
                SELECT exceptiontype, sessionnumber,
                       ROW_NUMBER() OVER (PARTITION BY exceptiontype ORDER BY startdatetime DESC) AS rn,
                       LEFT(exceptionmessage, 200) AS preview
                FROM BPVSessionInfo
                WHERE exceptiontype IS NOT NULL
                  AND (@since  IS NULL OR startdatetime >= @since)
                  AND (@until  IS NULL OR startdatetime <  @until)
                  AND (@process IS NULL OR processname = @process)
                  AND (@resource IS NULL OR runningresourcename = @resource)
                  AND (@text IS NULL OR exceptionmessage LIKE '%' + @text + '%')
                  AND (@ex_active = 0 OR exceptiontype IN (SELECT value FROM STRING_SPLIT(@ex_csv, ',')))
            ),
            process_lists AS (
                SELECT exceptiontype,
                       STRING_AGG(processname, ',') WITHIN GROUP (ORDER BY processname) AS processes
                FROM (SELECT DISTINCT exceptiontype, processname
                      FROM BPVSessionInfo
                      WHERE exceptiontype IS NOT NULL
                        AND processname IS NOT NULL
                        AND (@since  IS NULL OR startdatetime >= @since)
                        AND (@until  IS NULL OR startdatetime <  @until)
                        AND (@process IS NULL OR processname = @process)
                        AND (@resource IS NULL OR runningresourcename = @resource)
                        AND (@text IS NULL OR exceptionmessage LIKE '%' + @text + '%')
                        AND (@ex_active = 0 OR exceptiontype IN (SELECT value FROM STRING_SPLIT(@ex_csv, ',')))
                     ) p
                GROUP BY exceptiontype
            ),
            resource_lists AS (
                SELECT exceptiontype,
                       STRING_AGG(runningresourcename, ',') WITHIN GROUP (ORDER BY runningresourcename) AS resources
                FROM (SELECT DISTINCT exceptiontype, runningresourcename
                      FROM BPVSessionInfo
                      WHERE exceptiontype IS NOT NULL
                        AND runningresourcename IS NOT NULL
                        AND (@since  IS NULL OR startdatetime >= @since)
                        AND (@until  IS NULL OR startdatetime <  @until)
                        AND (@process IS NULL OR processname = @process)
                        AND (@resource IS NULL OR runningresourcename = @resource)
                        AND (@text IS NULL OR exceptionmessage LIKE '%' + @text + '%')
                        AND (@ex_active = 0 OR exceptiontype IN (SELECT value FROM STRING_SPLIT(@ex_csv, ',')))
                     ) r
                GROUP BY exceptiontype
            )
            SELECT a.exceptiontype, a.cnt, a.first_seen, a.last_seen,
                   pl.processes, rl.resources,
                   (SELECT STRING_AGG(CONVERT(varchar(20), rs.sessionnumber), ',')
                      FROM (SELECT TOP 5 sessionnumber, exceptiontype
                            FROM ranked_samples rs2
                            WHERE rs2.exceptiontype = a.exceptiontype AND rs2.rn <= 5
                            ORDER BY rn) rs
                      WHERE rs.exceptiontype = a.exceptiontype
                   ) AS sample_session_numbers,
                   (SELECT TOP 1 preview FROM ranked_samples rs2 WHERE rs2.exceptiontype = a.exceptiontype) AS sample_preview
            FROM agg a
            LEFT JOIN process_lists pl ON pl.exceptiontype = a.exceptiontype
            LEFT JOIN resource_lists rl ON rl.exceptiontype = a.exceptiontype
            ORDER BY a.cnt DESC;";

        using var conn = _context.CreateConnection();
        conn.Open();
        using var cmd = new SqlCommand(Sql, conn);
        cmd.Parameters.Add(new SqlParameter("@since", SqlDbType.DateTimeOffset) { Value = (object?)since ?? DBNull.Value });
        cmd.Parameters.Add(new SqlParameter("@until", SqlDbType.DateTimeOffset) { Value = (object?)until ?? DBNull.Value });
        cmd.Parameters.Add(new SqlParameter("@process", SqlDbType.NVarChar, 128) { Value = (object?)process ?? DBNull.Value });
        cmd.Parameters.Add(new SqlParameter("@resource", SqlDbType.NVarChar, 128) { Value = (object?)resource ?? DBNull.Value });
        cmd.Parameters.Add(new SqlParameter("@text", SqlDbType.NVarChar, -1) { Value = (object?)text ?? DBNull.Value });

        var exActive = exceptionTypes is { Length: > 0 };
        cmd.Parameters.Add(new SqlParameter("@ex_active", SqlDbType.Bit) { Value = exActive });
        cmd.Parameters.Add(new SqlParameter("@ex_csv", SqlDbType.NVarChar, -1) { Value = exActive ? string.Join(',', exceptionTypes!) : (object)DBNull.Value });

        using var reader = cmd.ExecuteReader();
        var results = new List<SessionErrorAggregate>();
        while (reader.Read())
        {
            var samples = reader.IsDBNull(6) ? Array.Empty<int>() : (reader.GetString(6) ?? string.Empty)
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => int.Parse(s, CultureInfo.InvariantCulture))
                .ToArray();

            results.Add(new SessionErrorAggregate(
                ExceptionType: reader.GetString(0),
                Count: reader.GetInt32(1),
                FirstSeen: DateTimeUtils.AssumeUtc(reader.GetDateTime(2)),
                LastSeen: DateTimeUtils.AssumeUtc(reader.GetDateTime(3)),
                Processes: reader.IsDBNull(4) ? Array.Empty<string>() : (reader.GetString(4) ?? string.Empty)
                    .Split(',', StringSplitOptions.RemoveEmptyEntries),
                Resources: reader.IsDBNull(5) ? Array.Empty<string>() : (reader.GetString(5) ?? string.Empty)
                    .Split(',', StringSplitOptions.RemoveEmptyEntries),
                SampleSessionNumbers: samples,
                SampleMessagePreview: reader.IsDBNull(7) ? null : reader.GetString(7)));
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

}
