using System.Data;
using BpLogViewer.Cli.Models;
using Microsoft.Data.SqlClient;

namespace BpLogViewer.Cli.Data;

public sealed class SessionLogRepository
{
    public const string NonUnicodeTable = "BPASessionLog_NonUnicode";
    public const string UnicodeTable = "BPASessionLog_Unicode";

    private readonly BpDbContext _context;

    public SessionLogRepository(BpDbContext context)
    {
        _context = context;
    }

    public string? ResolveActiveLogTable()
    {
        using var conn = _context.CreateConnection();
        conn.Open();

        if (CountRows(conn, NonUnicodeTable) > 0) return NonUnicodeTable;
        if (CountRows(conn, UnicodeTable) > 0) return UnicodeTable;
        return null;
    }

    public List<SessionLogEntry> List(
        string tableName,
        int sessionNumber,
        int[]? resultTypes,
        string? text,
        string? stage,
        bool fullResult,
        int maxRowsPlusOne,
        IReadOnlyDictionary<int, string> resultTypeNames)
    {
        using var conn = _context.CreateConnection();
        conn.Open();

        using var cmd = BuildListCommand(conn, tableName, sessionNumber, resultTypes, text, stage, fullResult, maxRowsPlusOne);
        using var reader = cmd.ExecuteReader(CommandBehavior.SequentialAccess);

        var rows = new List<SessionLogEntry>(maxRowsPlusOne);
        while (reader.Read())
        {
            var logId = reader.GetInt64(0);
            var stageName = reader.IsDBNull(1) ? null : reader.GetString(1);
            var stageType = reader.IsDBNull(2) ? null : (int?)reader.GetInt32(2);
            var rt = reader.IsDBNull(3) ? null : (int?)reader.GetInt32(3);
            DateTimeOffset? startedAt = reader.IsDBNull(4) ? null : AssumeUtc(reader.GetDateTime(4));
            DateTimeOffset? endedAt = reader.IsDBNull(5) ? null : AssumeUtc(reader.GetDateTime(5));
            var resultPreview = reader.IsDBNull(6) ? null : reader.GetString(6);
            var attributeSize = reader.IsDBNull(7) ? null : (long?)Convert.ToInt64(reader.GetValue(7), System.Globalization.CultureInfo.InvariantCulture);

            long? durationMs = (startedAt.HasValue && endedAt.HasValue)
                ? (long)(endedAt.Value - startedAt.Value).TotalMilliseconds
                : null;

            rows.Add(new SessionLogEntry(
                LogId: logId,
                SourceTable: tableName,
                StageName: stageName,
                StageType: stageType,
                ResultType: rt,
                ResultTypeName: ResolveResultTypeName(rt, resultTypeNames),
                StartedAt: startedAt,
                EndedAt: endedAt,
                DurationMs: durationMs,
                ResultPreview: resultPreview,
                AttributeSizeBytes: attributeSize));
        }

        return rows;
    }

    private static SqlCommand BuildListCommand(
        SqlConnection conn,
        string tableName,
        int sessionNumber,
        int[]? resultTypes,
        string? text,
        string? stage,
        bool fullResult,
        int maxRowsPlusOne)
    {
        var resultProjection = fullResult ? "result" : "LEFT(result, 4000)";

        var sql = $@"
            SELECT TOP (@max_rows)
                   logid,
                   stagename,
                   stagetype,
                   resulttype,
                   startdatetime,
                   enddatetime,
                   {resultProjection} AS result_preview,
                   attributesize
            FROM {tableName}
            WHERE sessionnumber = @session_number
              AND (@result_type_active = 0 OR resulttype IN (SELECT CAST(value AS INT) FROM STRING_SPLIT(@result_type_csv, ',')))
              AND (@text IS NULL OR result LIKE '%' + @text + '%')
              AND (@stage IS NULL OR stagename = @stage)
            ORDER BY logid ASC;";

        var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.Add(new SqlParameter("@max_rows", SqlDbType.Int) { Value = maxRowsPlusOne });
        cmd.Parameters.Add(new SqlParameter("@session_number", SqlDbType.Int) { Value = sessionNumber });
        var resultTypeActive = resultTypes is { Length: > 0 };
        cmd.Parameters.Add(new SqlParameter("@result_type_active", SqlDbType.Bit) { Value = resultTypeActive });
        cmd.Parameters.Add(new SqlParameter("@result_type_csv", SqlDbType.NVarChar, -1) { Value = resultTypeActive ? string.Join(',', resultTypes!) : (object)DBNull.Value });
        cmd.Parameters.Add(new SqlParameter("@text", SqlDbType.NVarChar, -1) { Value = (object?)text ?? DBNull.Value });
        cmd.Parameters.Add(new SqlParameter("@stage", SqlDbType.NVarChar, 256) { Value = (object?)stage ?? DBNull.Value });
        return cmd;
    }

    private static long CountRows(SqlConnection conn, string tableName)
    {
        using var cmd = new SqlCommand($"SELECT COUNT_BIG(1) FROM {tableName};", conn);
        var value = cmd.ExecuteScalar();
        return value is null or DBNull ? 0 : Convert.ToInt64(value, System.Globalization.CultureInfo.InvariantCulture);
    }

    private static DateTimeOffset AssumeUtc(DateTime dt)
    {
        return new DateTimeOffset(DateTime.SpecifyKind(dt, DateTimeKind.Utc));
    }

    private static string? ResolveResultTypeName(int? resultType, IReadOnlyDictionary<int, string> map)
    {
        if (!resultType.HasValue)
        {
            return null;
        }

        if (map.TryGetValue(resultType.Value, out var name))
        {
            return name;
        }

        return $"Unknown({resultType.Value})";
    }
}
