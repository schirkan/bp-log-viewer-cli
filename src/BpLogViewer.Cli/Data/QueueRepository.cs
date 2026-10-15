using System.Data;
using BpLogViewer.Cli.Models;
using Microsoft.Data.SqlClient;

namespace BpLogViewer.Cli.Data;

public sealed class QueueRepository
{
    private readonly BpDbContext _context;

    public QueueRepository(BpDbContext context)
    {
        _context = context;
    }

    public List<QueueInfo> ListQueues(string? name, bool nameContains, bool onlyExceptions, bool onlyRunning, int maxRowsPlusOne)
    {
        using var conn = _context.CreateConnection();
        conn.Open();
        using var cmd = BuildQueueListCommand(conn, name, nameContains, onlyExceptions, onlyRunning, maxRowsPlusOne);
        using var reader = cmd.ExecuteReader();

        var rows = new List<QueueInfo>(maxRowsPlusOne);
        while (reader.Read())
        {
            DateTimeOffset? oldestPending = reader.IsDBNull(12) ? null : DateTimeUtils.AssumeUtc(reader.GetDateTime(12));
            // Stale-Schwellwert zentral in Configuration.QueueThresholds.
            var stale = oldestPending.HasValue && oldestPending.Value < DateTimeOffset.UtcNow.AddHours(-Configuration.QueueThresholds.StalePendingThresholdHours);

            rows.Add(new QueueInfo(
                QueueId: reader.GetGuid(0),
                Name: reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                KeyField: reader.IsDBNull(2) ? null : reader.GetString(2),
                Running: !reader.IsDBNull(3) && reader.GetBoolean(3),
                MaxAttempts: reader.IsDBNull(4) ? null : (int?)reader.GetInt32(4),
                ProcessId: reader.IsDBNull(5) ? null : (Guid?)reader.GetGuid(5),
                ItemCountTotal: reader.IsDBNull(6) ? 0 : reader.GetInt32(6),
                ItemCountPending: reader.IsDBNull(7) ? 0 : reader.GetInt32(7),
                ItemCountLocked: reader.IsDBNull(8) ? 0 : reader.GetInt32(8),
                ItemCountCompleted: reader.IsDBNull(9) ? 0 : reader.GetInt32(9),
                ItemCountException: reader.IsDBNull(10) ? 0 : reader.GetInt32(10),
                ItemCountDeferred: reader.IsDBNull(11) ? 0 : reader.GetInt32(11),
                OldestPendingLoadedAt: oldestPending,
                NewestPendingLoadedAt: reader.IsDBNull(13) ? null : DateTimeUtils.AssumeUtc(reader.GetDateTime(13)),
                Stale: stale));
        }

        return rows;
    }

    public Guid? ResolveQueueIdByName(string queueName)
    {
        using var conn = _context.CreateConnection();
        conn.Open();
        // ORDER BY name für deterministisches Ergebnis bei (theoretisch) mehreren Queues mit gleichem Namen.
        // ORDER BY name für deterministisches Ergebnis bei (theoretisch) mehreren Queues mit gleichem Namen.
        using var cmd = new SqlCommand("SELECT TOP 1 id FROM BPAWorkQueue WHERE name=@name ORDER BY name", conn);
        cmd.Parameters.Add(new SqlParameter("@name", SqlDbType.NVarChar, 255) { Value = queueName });
        var scalar = cmd.ExecuteScalar();
        return scalar is null or DBNull ? null : (Guid?)ConvertToGuid(scalar);
    }

    public List<QueueItemInfo> ListItems(Guid queueId, string[]? statuses, DateTimeOffset? since, DateTimeOffset? until, string? text, bool fullData, bool includeTags, int maxRowsPlusOne)
    {
        using var conn = _context.CreateConnection();
        conn.Open();
        using var cmd = BuildQueueItemsCommand(conn, queueId, statuses, since, until, text, fullData, maxRowsPlusOne);
        using var reader = cmd.ExecuteReader();

        var rows = new List<QueueItemInfo>(maxRowsPlusOne);
        while (reader.Read())
        {
            rows.Add(new QueueItemInfo(
                ItemId: reader.GetGuid(0),
                Ident: reader.GetInt64(1),
                KeyValue: reader.IsDBNull(2) ? null : reader.GetString(2),
                Status: reader.IsDBNull(3) ? null : reader.GetString(3),
                Attempt: reader.IsDBNull(4) ? 0 : reader.GetInt32(4),
                LoadedAt: reader.IsDBNull(5) ? null : DateTimeUtils.AssumeUtc(reader.GetDateTime(5)),
                LockedAt: reader.IsDBNull(6) ? null : DateTimeUtils.AssumeUtc(reader.GetDateTime(6)),
                CompletedAt: reader.IsDBNull(7) ? null : DateTimeUtils.AssumeUtc(reader.GetDateTime(7)),
                ExceptionAt: reader.IsDBNull(8) ? null : DateTimeUtils.AssumeUtc(reader.GetDateTime(8)),
                ExceptionReason: reader.IsDBNull(9) ? null : reader.GetString(9),
                ExceptionTag: reader.IsDBNull(10) ? null : reader.GetString(10),
                Priority: reader.IsDBNull(11) ? null : (int?)reader.GetInt32(11),
                WorkTimeSec: reader.IsDBNull(12) ? null : (int?)reader.GetInt32(12),
                SessionId: reader.IsDBNull(13) ? null : (Guid?)reader.GetGuid(13),
                LastUpdated: reader.IsDBNull(14) ? null : DateTimeUtils.AssumeUtc(reader.GetDateTime(14)),
                DataPreview: reader.IsDBNull(15) ? null : reader.GetString(15),
                Tags: null));
        }

        if (includeTags && rows.Count > 0)
        {
            var tags = LoadTagsByIdent(conn, rows.Select(r => r.Ident).ToArray());
            for (var i = 0; i < rows.Count; i++)
            {
                var ident = rows[i].Ident;
                rows[i] = rows[i] with { Tags = tags.TryGetValue(ident, out var arr) ? arr : Array.Empty<string>() };
            }
        }

        return rows;
    }

    private static SqlCommand BuildQueueListCommand(SqlConnection conn, string? name, bool nameContains, bool onlyExceptions, bool onlyRunning, int maxRowsPlusOne)
    {
        const string sql = @"
            SELECT TOP (@max_rows)
                   q.id,
                   q.name,
                   q.keyfield,
                   q.running,
                   q.maxattempts,
                   q.processid,
                   ISNULL(agg.item_count_total, 0) AS item_count_total,
                   ISNULL(agg.item_count_pending, 0) AS item_count_pending,
                   ISNULL(agg.item_count_locked, 0) AS item_count_locked,
                   ISNULL(agg.item_count_completed, 0) AS item_count_completed,
                   ISNULL(agg.item_count_exception, 0) AS item_count_exception,
                   ISNULL(agg.item_count_deferred, 0) AS item_count_deferred,
                   agg.oldest_pending_loaded_at,
                   agg.newest_pending_loaded_at
            FROM BPAWorkQueue q
            LEFT JOIN (
                SELECT queueid,
                       COUNT(*) AS item_count_total,
                       SUM(CASE WHEN status = 'Pending' THEN 1 ELSE 0 END) AS item_count_pending,
                       SUM(CASE WHEN status = 'Locked' THEN 1 ELSE 0 END) AS item_count_locked,
                       SUM(CASE WHEN status = 'Completed' THEN 1 ELSE 0 END) AS item_count_completed,
                       SUM(CASE WHEN status = 'Exception' THEN 1 ELSE 0 END) AS item_count_exception,
                       SUM(CASE WHEN status = 'Deferred' THEN 1 ELSE 0 END) AS item_count_deferred,
                       MIN(CASE WHEN status = 'Pending' THEN loaded END) AS oldest_pending_loaded_at,
                       MAX(CASE WHEN status = 'Pending' THEN loaded END) AS newest_pending_loaded_at
                FROM BPAWorkQueueItem
                GROUP BY queueid
            ) agg ON agg.queueid = q.id
            WHERE (@name IS NULL OR q.name LIKE @name_pattern)
              AND (@only_running = 0 OR q.running = 1)
              AND (@only_exceptions = 0 OR ISNULL(agg.item_count_exception, 0) > 0)
            ORDER BY q.name ASC;";

        var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.Add(new SqlParameter("@max_rows", SqlDbType.Int) { Value = maxRowsPlusOne });
        cmd.Parameters.Add(new SqlParameter("@name", SqlDbType.NVarChar, 255) { Value = (object?)name ?? DBNull.Value });
        var namePattern = name is null ? (object)DBNull.Value : (nameContains ? $"%{name}%" : $"{name}%");
        cmd.Parameters.Add(new SqlParameter("@name_pattern", SqlDbType.NVarChar, 260) { Value = namePattern });
        cmd.Parameters.Add(new SqlParameter("@only_running", SqlDbType.Bit) { Value = onlyRunning });
        cmd.Parameters.Add(new SqlParameter("@only_exceptions", SqlDbType.Bit) { Value = onlyExceptions });
        return cmd;
    }

    private static SqlCommand BuildQueueItemsCommand(SqlConnection conn, Guid queueId, string[]? statuses, DateTimeOffset? since, DateTimeOffset? until, string? text, bool fullData, int maxRowsPlusOne)
    {
        var dataProjection = fullData ? "data" : "LEFT(data, 1000)";
        var sql = $@"
            SELECT TOP (@max_rows)
                   id,
                   ident,
                   keyvalue,
                   status,
                   attempt,
                   loaded,
                   locked,
                   completed,
                   exception,
                   exceptionreason,
                   exceptionreasontag,
                   priority,
                   worktime,
                   sessionid,
                   lastupdated,
                   {dataProjection} AS data_preview
            FROM BPVWorkQueueItem
            WHERE queueid = @queue_id
              AND (@status_active = 0 OR status IN (SELECT value FROM STRING_SPLIT(@status_csv, ',')))
              AND (@since IS NULL OR lastupdated >= @since)
              AND (@until IS NULL OR lastupdated < @until)
              AND (@text IS NULL OR keyvalue LIKE '%' + @text + '%' OR exceptionreason LIKE '%' + @text + '%')
            ORDER BY priority DESC, lastupdated DESC;";

        var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.Add(new SqlParameter("@max_rows", SqlDbType.Int) { Value = maxRowsPlusOne });
        cmd.Parameters.Add(new SqlParameter("@queue_id", SqlDbType.UniqueIdentifier) { Value = queueId });
        var statusActive = statuses is { Length: > 0 };
        cmd.Parameters.Add(new SqlParameter("@status_active", SqlDbType.Bit) { Value = statusActive });
        cmd.Parameters.Add(new SqlParameter("@status_csv", SqlDbType.NVarChar, -1) { Value = statusActive ? string.Join(',', statuses!) : (object)DBNull.Value });
        cmd.Parameters.Add(new SqlParameter("@since", SqlDbType.DateTimeOffset) { Value = (object?)since ?? DBNull.Value });
        cmd.Parameters.Add(new SqlParameter("@until", SqlDbType.DateTimeOffset) { Value = (object?)until ?? DBNull.Value });
        cmd.Parameters.Add(new SqlParameter("@text", SqlDbType.NVarChar, -1) { Value = (object?)text ?? DBNull.Value });
        return cmd;
    }

    private static Dictionary<long, string[]> LoadTagsByIdent(SqlConnection conn, long[] idents)
    {
        if (idents.Length == 0) return new Dictionary<long, string[]>();

        var paramNames = new List<string>(idents.Length);
        using var cmd = new SqlCommand();
        cmd.Connection = conn;
        for (var i = 0; i < idents.Length; i++)
        {
            var pn = "@id" + i;
            paramNames.Add(pn);
            cmd.Parameters.Add(new SqlParameter(pn, SqlDbType.BigInt) { Value = idents[i] });
        }

        cmd.CommandText = $@"
            SELECT queueitemident, tag
            FROM BPViewWorkQueueItemTag
            WHERE queueitemident IN ({string.Join(",", paramNames)})
            ORDER BY queueitemident;";

        using var reader = cmd.ExecuteReader();
        var map = new Dictionary<long, List<string>>();
        while (reader.Read())
        {
            var ident = reader.GetInt64(0);
            var tag = reader.IsDBNull(1) ? null : reader.GetString(1);
            if (string.IsNullOrWhiteSpace(tag)) continue;
            if (!map.TryGetValue(ident, out var list))
            {
                list = new List<string>();
                map[ident] = list;
            }
            list.Add(tag);
        }

        return map.ToDictionary(k => k.Key, v => v.Value.Distinct(StringComparer.Ordinal).ToArray());
    }

    private static Guid ConvertToGuid(object value)
    {
        return value switch
        {
            Guid g => g,
            string s => Guid.Parse(s),
            _ => (Guid)value
        };
    }

}
