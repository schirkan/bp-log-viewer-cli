using System.Data;
using BpLogViewer.Cli.Models;
using Microsoft.Data.SqlClient;

namespace BpLogViewer.Cli.Data;

/// <summary>
/// Read-Repository für <c>BPVSessionInfo</c> (F-002).
/// Alle Queries sind parametrisiert (CWE-89-Schutz, architecture § 7.2).
/// </summary>
public sealed class SessionRepository
{
    private readonly BpDbContext _context;

    public SessionRepository(BpDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Liefert Sessions aus <c>BPVSessionInfo</c>, gefiltert nach Zeitraum, Prozess,
    /// Resource, Status-IDs (numerisch) und Exception-Flag.
    /// </summary>
    /// <param name="since">UTC; null = kein Lower-Bound.</param>
    /// <param name="until">UTC; null = kein Upper-Bound (exklusiv).</param>
    /// <param name="process">Exakter Prozess-Name; null = kein Filter.</param>
    /// <param name="resource">Exakter Resource-Name; null = kein Filter.</param>
    /// <param name="statusIds">Numerische Status-IDs (bereits via StatusCache aufgelöst); null/leer = kein Filter.</param>
    /// <param name="hasException">true = nur Sessions mit exceptiontype IS NOT NULL.</param>
    /// <param name="maxRowsPlusOne">LIMIT (maxRows+1) — der +1-Datensatz dient zur Overflow-Erkennung.</param>
    public List<SessionInfo> List(
        DateTimeOffset? since,
        DateTimeOffset? until,
        string? process,
        string? resource,
        int[]? statusIds,
        bool hasException,
        int maxRowsPlusOne)
    {
        using var conn = _context.CreateConnection();
        conn.Open();
        using var cmd = BuildListCommand(conn, since, until, process, resource, statusIds, hasException, maxRowsPlusOne);
        using var reader = cmd.ExecuteReader();

        var results = new List<SessionInfo>(capacity: maxRowsPlusOne);
        while (reader.Read())
        {
            results.Add(ReadSession(reader));
        }
        return results;
    }

    private static SqlCommand BuildListCommand(
        SqlConnection conn,
        DateTimeOffset? since,
        DateTimeOffset? until,
        string? process,
        string? resource,
        int[]? statusIds,
        bool hasException,
        int maxRowsPlusOne)
    {
        const string Sql = @"
            SELECT sessionnumber, sessionid, processname, processid,
                   runningresourcename, runningresourceid,
                   statusid,
                   startdatetime, starttimezoneoffset,
                   enddatetime, endtimezoneoffset,
                   laststage, exceptiontype, exceptionmessage
            FROM BPVSessionInfo
            WHERE (@since  IS NULL OR startdatetime >= @since)
              AND (@until  IS NULL OR startdatetime <  @until)
              AND (@process IS NULL OR processname = @process)
              AND (@resource IS NULL OR runningresourcename = @resource)
              AND (@status_active = 0 OR statusid IN (SELECT CAST(value AS INT) FROM STRING_SPLIT(@status_csv, ',')))
              AND (@has_exception = 0 OR exceptiontype IS NOT NULL)
            ORDER BY startdatetime DESC
            OFFSET 0 ROWS FETCH NEXT @max_rows ROWS ONLY;";

        var cmd = new SqlCommand(Sql, conn);
        cmd.Parameters.Add(new SqlParameter("@since", SqlDbType.DateTimeOffset) { Value = (object?)since ?? DBNull.Value });
        cmd.Parameters.Add(new SqlParameter("@until", SqlDbType.DateTimeOffset) { Value = (object?)until ?? DBNull.Value });
        cmd.Parameters.Add(new SqlParameter("@process", SqlDbType.NVarChar, 128) { Value = (object?)process ?? DBNull.Value });
        cmd.Parameters.Add(new SqlParameter("@resource", SqlDbType.NVarChar, 128) { Value = (object?)resource ?? DBNull.Value });

        var hasStatusFilter = statusIds is { Length: > 0 };
        var statusCsv = hasStatusFilter ? string.Join(",", statusIds!) : null;
        cmd.Parameters.Add(new SqlParameter("@status_active", SqlDbType.Bit) { Value = hasStatusFilter });
        cmd.Parameters.Add(new SqlParameter("@status_csv", SqlDbType.NVarChar, -1) { Value = (object?)statusCsv ?? DBNull.Value });

        cmd.Parameters.Add(new SqlParameter("@has_exception", SqlDbType.Bit) { Value = hasException });
        cmd.Parameters.Add(new SqlParameter("@max_rows", SqlDbType.Int) { Value = maxRowsPlusOne });
        return cmd;
    }

    private static SessionInfo ReadSession(SqlDataReader reader)
    {
        var startedAt = ReadDateTimeOffset(reader, startDateIndex: 7, startOffsetIndex: 8);
        DateTimeOffset? endedAt = reader.IsDBNull(9)
            ? null
            : ReadDateTimeOffset(reader, startDateIndex: 9, startOffsetIndex: 10);

        int? durationSec = endedAt.HasValue
            ? (int)(endedAt.Value - startedAt).TotalSeconds
            : null;

        return new SessionInfo(
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
            ExceptionMessage: reader.IsDBNull(13) ? null : reader.GetString(13));
    }

    private static DateTimeOffset ReadDateTimeOffset(SqlDataReader reader, int startDateIndex, int startOffsetIndex)
    {
        var dt = reader.GetDateTime(startDateIndex);
        var rawOffset = Convert.ToInt32(reader.GetValue(startOffsetIndex), System.Globalization.CultureInfo.InvariantCulture);

        // BPVSessionInfo liefert je nach Quelle Sekunden (z. B. 7200) statt Minuten.
        // Normalisieren auf Minuten für DateTimeOffset.
        var offsetMinutes = Math.Abs(rawOffset) > 14 * 60 ? rawOffset / 60 : rawOffset;
        return new DateTimeOffset(dt, TimeSpan.FromMinutes(offsetMinutes));
    }
}
