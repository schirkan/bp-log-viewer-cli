using System.Data;
using BpLogViewer.Cli.Models;
using Microsoft.Data.SqlClient;

namespace BpLogViewer.Cli.Data;

/// <summary>
/// Read-Repository für <c>BPVSessionInfo</c> (F-002) und Session-Details (F-003).
/// Alle Queries sind parametrisiert (CWE-89-Schutz, architecture § 7.2).
/// </summary>
public sealed class SessionRepository
{
    public const int WorkQueueItemsLimit = 100;

    private readonly BpDbContext _context;

    public SessionRepository(BpDbContext context)
    {
        _context = context;
    }

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
            results.Add(SessionReader.ReadSessionInfo(reader));
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

    public SessionDetails? GetBySessionNumber(int sessionNumber, bool includeStateXml)
        => GetDetails(bySessionNumber: sessionNumber, bySessionId: null, includeStateXml: includeStateXml);

    public SessionDetails? GetBySessionId(Guid sessionId, bool includeStateXml)
        => GetDetails(bySessionNumber: null, bySessionId: sessionId, includeStateXml: includeStateXml);

    private SessionDetails? GetDetails(int? bySessionNumber, Guid? bySessionId, bool includeStateXml)
    {
        using var conn = _context.CreateConnection();
        conn.Open();
        using var cmd = BuildDetailsCommand(conn, bySessionNumber, bySessionId, includeStateXml);
        using var reader = cmd.ExecuteReader();
        if (!reader.Read()) return null;

        var details = ReadSessionDetails(reader, includeStateXml);
        reader.Close();
        var (items, truncated) = LoadWorkQueueItems(conn, details.SessionId);
        return details with { WorkQueueItems = items, Truncated = truncated };
    }

    private static SqlCommand BuildDetailsCommand(SqlConnection conn, int? bySessionNumber, Guid? bySessionId, bool includeStateXml)
    {
        var sql = includeStateXml
            ? @"
            SELECT TOP 1
                   v.sessionnumber, v.sessionid, v.processname, v.processid,
                   v.runningresourcename, v.runningresourceid,
                   v.statusid,
                   v.startdatetime, v.starttimezoneoffset,
                   v.enddatetime, v.endtimezoneoffset,
                   v.laststage, v.exceptiontype, v.exceptionmessage,
                   s.queueid, s.startparamsxml, s.sessionstatexml
            FROM BPVSessionInfo v
            LEFT JOIN BPASession s ON s.sessionid = v.sessionid
            WHERE (@session_number IS NULL OR v.sessionnumber = @session_number)
              AND (@session_id IS NULL OR v.sessionid = @session_id)
            ORDER BY v.sessionnumber DESC;"
            : @"
            SELECT TOP 1
                   v.sessionnumber, v.sessionid, v.processname, v.processid,
                   v.runningresourcename, v.runningresourceid,
                   v.statusid,
                   v.startdatetime, v.starttimezoneoffset,
                   v.enddatetime, v.endtimezoneoffset,
                   v.laststage, v.exceptiontype, v.exceptionmessage,
                   s.queueid
            FROM BPVSessionInfo v
            LEFT JOIN BPASession s ON s.sessionid = v.sessionid
            WHERE (@session_number IS NULL OR v.sessionnumber = @session_number)
              AND (@session_id IS NULL OR v.sessionid = @session_id)
            ORDER BY v.sessionnumber DESC;";

        var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.Add(new SqlParameter("@session_number", SqlDbType.Int) { Value = (object?)bySessionNumber ?? DBNull.Value });
        cmd.Parameters.Add(new SqlParameter("@session_id", SqlDbType.UniqueIdentifier) { Value = (object?)bySessionId ?? DBNull.Value });
        return cmd;
    }

    private static SessionDetails ReadSessionDetails(SqlDataReader reader, bool includeStateXml)
    {
        var startedAt = SessionReader.ReadDateTimeOffsetSafe(reader, dtIndex: 7, offsetIndex: 8);
        DateTimeOffset? endedAt = reader.IsDBNull(9)
            ? null
            : SessionReader.ReadDateTimeOffsetSafe(reader, dtIndex: 9, offsetIndex: 10);

        int? durationSec = endedAt.HasValue
            ? (int)(endedAt.Value - startedAt).TotalSeconds
            : null;

        // queueId (BPASession.queueid) und optionale State-XML-Spalten.
        // Wichtig: BPASession.queueid kann NULL sein, daher der IsDBNull-Check.
        // loaded/finished sind DateTime-Spalten ohne eigene Offset-Spalte → Offset-Index = dt-Index.
        const int baseIdx = 14;
        int? queueId = reader.IsDBNull(baseIdx) ? null : Convert.ToInt32(reader.GetValue(baseIdx), System.Globalization.CultureInfo.InvariantCulture);
        var startParamsXml = includeStateXml && !reader.IsDBNull(baseIdx + 1) ? reader.GetString(baseIdx + 1) : null;
        var sessionStateXml = includeStateXml && !reader.IsDBNull(baseIdx + 2) ? reader.GetString(baseIdx + 2) : null;

        return new SessionDetails(
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
            IsRunning: !endedAt.HasValue,
            LastStage: reader.IsDBNull(11) ? null : reader.GetString(11),
            ExceptionType: reader.IsDBNull(12) ? null : reader.GetString(12),
            ExceptionMessage: reader.IsDBNull(13) ? null : reader.GetString(13),
            QueueId: queueId,
            StartParamsXml: startParamsXml,
            SessionStateXml: sessionStateXml,
            Truncated: false,
            WorkQueueItems: Array.Empty<WorkQueueItemInfo>());
    }

    private static (IReadOnlyList<WorkQueueItemInfo> items, bool truncated) LoadWorkQueueItems(SqlConnection conn, Guid sessionId)
    {
        const string Sql = @"
            SELECT TOP (@max_plus_one)
                   keyvalue, status, attempt,
                   exceptionreason, exceptionreasontag,
                   loaded, finished
            FROM BPAWorkQueueItem
            WHERE sessionid = @session_id
            ORDER BY loaded DESC, ident DESC;";

        using var cmd = new SqlCommand(Sql, conn);
        cmd.Parameters.Add(new SqlParameter("@session_id", SqlDbType.UniqueIdentifier) { Value = sessionId });
        cmd.Parameters.Add(new SqlParameter("@max_plus_one", SqlDbType.Int) { Value = WorkQueueItemsLimit + 1 });
        using var reader = cmd.ExecuteReader();

        var items = new List<WorkQueueItemInfo>(WorkQueueItemsLimit + 1);
        // loaded/finished sind DateTime-Spalten ohne eigene Offset-Spalte → Offset-Index = dt-Index.
        while (reader.Read())
        {
            items.Add(new WorkQueueItemInfo(
                KeyValue: reader.IsDBNull(0) ? null : reader.GetString(0),
                Status: reader.IsDBNull(1) ? null : reader.GetString(1),
                Attempt: reader.IsDBNull(2) ? 0 : reader.GetInt32(2),
                ExceptionReason: reader.IsDBNull(3) ? null : reader.GetString(3),
                ExceptionReasonTag: reader.IsDBNull(4) ? null : reader.GetString(4),
                LoadedAt: reader.IsDBNull(5) ? null : SessionReader.ReadDateTimeOffsetSafe(reader, dtIndex: 5, offsetIndex: 5),
                FinishedAt: reader.IsDBNull(6) ? null : SessionReader.ReadDateTimeOffsetSafe(reader, dtIndex: 6, offsetIndex: 6)));
        }

        var truncated = items.Count > WorkQueueItemsLimit;
        if (truncated) items.RemoveAt(items.Count - 1);
        return (items, truncated);
    }
}
