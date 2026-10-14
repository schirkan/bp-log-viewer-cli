using System.Data;
using BpLogViewer.Cli.Models;
using Microsoft.Data.SqlClient;

namespace BpLogViewer.Cli.Data;

public sealed class AuditRepository
{
    private readonly BpDbContext _context;

    public AuditRepository(BpDbContext context)
    {
        _context = context;
    }

    public List<AuditEventInfo> List(DateTimeOffset? since, DateTimeOffset? until, string[]? codes, string? user, string? targetProcess, string? targetResource, bool includeXml, bool fullComments, int maxRowsPlusOne)
    {
        using var conn = _context.CreateConnection();
        conn.Open();
        using var cmd = BuildListCommand(conn, since, until, codes, user, targetProcess, targetResource, includeXml, fullComments, maxRowsPlusOne);
        using var reader = cmd.ExecuteReader();

        var rows = new List<AuditEventInfo>(maxRowsPlusOne);
        while (reader.Read())
        {
            rows.Add(new AuditEventInfo(
                EventId: reader.GetInt32(0),
                EventAt: AssumeUtc(reader.GetDateTime(1)),
                Code: reader.IsDBNull(2) ? null : reader.GetString(2),
                Narrative: reader.IsDBNull(3) ? null : reader.GetString(3),
                SrcUser: reader.IsDBNull(4) ? null : reader.GetString(4),
                TgtUser: reader.IsDBNull(5) ? null : reader.GetString(5),
                TgtProcess: reader.IsDBNull(6) ? null : reader.GetString(6),
                TgtResource: reader.IsDBNull(7) ? null : reader.GetString(7),
                CommentsPreview: reader.IsDBNull(8) ? null : reader.GetString(8),
                OldXml: includeXml && !reader.IsDBNull(9) ? reader.GetString(9) : null,
                NewXml: includeXml && !reader.IsDBNull(10) ? reader.GetString(10) : null));
        }

        return rows;
    }

    private static SqlCommand BuildListCommand(SqlConnection conn, DateTimeOffset? since, DateTimeOffset? until, string[]? codes, string? user, string? targetProcess, string? targetResource, bool includeXml, bool fullComments, int maxRowsPlusOne)
    {
        var commentsProjection = fullComments ? "a.comments" : "LEFT(a.comments, 500)";
        var xmlOldProjection = includeXml ? "a.oldXML" : "CAST(NULL AS nvarchar(max))";
        var xmlNewProjection = includeXml ? "a.newXML" : "CAST(NULL AS nvarchar(max))";

        var sql = $@"
            SELECT TOP (@max_rows)
                   a.eventid,
                   a.eventdatetime,
                   a.sCode,
                   a.sNarrative,
                   su.username AS src_user,
                   tu.username AS tgt_user,
                   p.name AS tgt_process,
                   r.name AS tgt_resource,
                   {commentsProjection} AS comments_preview,
                   {xmlOldProjection} AS old_xml,
                   {xmlNewProjection} AS new_xml
            FROM BPAAuditEvents a
            LEFT JOIN BPAUser su ON su.userid = a.gSrcUserID
            LEFT JOIN BPAUser tu ON tu.userid = a.gTgtUserID
            LEFT JOIN BPAProcess p ON p.processid = a.gTgtProcID
            LEFT JOIN BPAResource r ON r.resourceid = a.gTgtResourceID
            WHERE (@since IS NULL OR a.eventdatetime >= @since)
              AND (@until IS NULL OR a.eventdatetime < @until)
              AND (@code_active = 0 OR a.sCode IN (SELECT value FROM STRING_SPLIT(@code_csv, ',')))
              AND (@user IS NULL OR su.username LIKE @user_pattern)
              AND (@target_process IS NULL OR p.name LIKE @target_process_pattern)
              AND (@target_resource IS NULL OR r.name LIKE @target_resource_pattern)
            ORDER BY a.eventdatetime DESC;";

        var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.Add(new SqlParameter("@max_rows", SqlDbType.Int) { Value = maxRowsPlusOne });
        cmd.Parameters.Add(new SqlParameter("@since", SqlDbType.DateTimeOffset) { Value = (object?)since ?? DBNull.Value });
        cmd.Parameters.Add(new SqlParameter("@until", SqlDbType.DateTimeOffset) { Value = (object?)until ?? DBNull.Value });
        var codeActive = codes is { Length: > 0 };
        cmd.Parameters.Add(new SqlParameter("@code_active", SqlDbType.Bit) { Value = codeActive });
        cmd.Parameters.Add(new SqlParameter("@code_csv", SqlDbType.NVarChar, -1) { Value = codeActive ? string.Join(',', codes!) : (object)DBNull.Value });
        cmd.Parameters.Add(new SqlParameter("@user", SqlDbType.NVarChar, 255) { Value = (object?)user ?? DBNull.Value });
        cmd.Parameters.Add(new SqlParameter("@user_pattern", SqlDbType.NVarChar, 260) { Value = user is null ? (object)DBNull.Value : $"{user}%" });
        cmd.Parameters.Add(new SqlParameter("@target_process", SqlDbType.NVarChar, 255) { Value = (object?)targetProcess ?? DBNull.Value });
        cmd.Parameters.Add(new SqlParameter("@target_process_pattern", SqlDbType.NVarChar, 260) { Value = targetProcess is null ? (object)DBNull.Value : $"{targetProcess}%" });
        cmd.Parameters.Add(new SqlParameter("@target_resource", SqlDbType.NVarChar, 255) { Value = (object?)targetResource ?? DBNull.Value });
        cmd.Parameters.Add(new SqlParameter("@target_resource_pattern", SqlDbType.NVarChar, 260) { Value = targetResource is null ? (object)DBNull.Value : $"{targetResource}%" });
        return cmd;
    }

    private static DateTimeOffset AssumeUtc(DateTime dt)
    {
        return new DateTimeOffset(DateTime.SpecifyKind(dt, DateTimeKind.Utc));
    }
}
