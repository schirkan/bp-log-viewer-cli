using System.Data;
using System.Globalization;
using BpLogViewer.Cli.Models;
using Microsoft.Data.SqlClient;

namespace BpLogViewer.Cli.Data;

/// <summary>
/// Gemeinsame Reader-Helper für Sessions (DRY).
/// </summary>
internal static class SessionReader
{
    /// <summary>
    /// Liest UTC-Datetime + Minuten-Offset (BP-Spalten <c>starttimezoneoffset</c>/<c>endtimezoneoffset</c>
    /// sind Minuten). Keine Sekunden-Heuristik, da die Spalten explizit Minuten enthalten.
    /// </summary>
    public static DateTimeOffset ReadDateTimeOffsetSafe(SqlDataReader reader, int dtIndex, int offsetIndex)
    {
        if (reader.IsDBNull(dtIndex) || reader.IsDBNull(offsetIndex))
        {
            return default;
        }
        var dt = reader.GetDateTime(dtIndex);
        var offsetMinutes = Convert.ToInt32(reader.GetValue(offsetIndex), CultureInfo.InvariantCulture);
        var offset = TimeSpan.FromMinutes(offsetMinutes);
        // DateTimeOffset-Validierung: Offsets außerhalb ±14h werfen
        // ArgumentOutOfRangeException. BP-Datenbanken können durch Datenanomalien
        // oder DST-Edge-Cases Offsets außerhalb dieses Bereichs enthalten.
        // Wir clampen statt zu fehlschlagen — Drift ist besser als Crash.
        if (offset.TotalHours > 14) offset = TimeSpan.FromHours(14);
        if (offset.TotalHours < -14) offset = TimeSpan.FromHours(-14);
        return new DateTimeOffset(DateTime.SpecifyKind(dt, DateTimeKind.Unspecified), offset);
    }

    public static SessionInfo ReadSessionInfo(SqlDataReader reader)
    {
        var startedAt = ReadDateTimeOffsetSafe(reader, dtIndex: 7, offsetIndex: 8);
        DateTimeOffset? endedAt = reader.IsDBNull(9)
            ? null
            : ReadDateTimeOffsetSafe(reader, dtIndex: 9, offsetIndex: 10);

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
}
