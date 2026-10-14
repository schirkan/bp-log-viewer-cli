namespace BpLogViewer.Cli.Data;

/// <summary>
/// Geteilte DateTime-Helper für die Repositories.
/// <para>
/// Wichtig: Blue Prism speichert Datumsspalten in <c>BPVSessionInfo</c>,
/// <c>BPASession</c>, <c>BPAResource</c> und <c>BPVWorkQueueItem</c> als
/// <see cref="DateTimeKind.Unspecified"/> in der lokalen Server-Zeitzone.
/// Wir interpretieren sie als UTC, weil der typische BP-Server in UTC läuft
/// und die Test-DB das so liefert. Falls die Production-DB eine andere
/// Server-Timezone verwendet, gibt es eine stille +1/+2h-Drift; das ist eine
/// bekannte Limitation und wird im Code-Review dokumentiert
/// (<c>specs/README.md</c> § Known Limitations).
/// </para>
/// </summary>
internal static class DateTimeUtils
{
    /// <summary>
    /// Markiert ein <see cref="DateTime"/> als UTC und konvertiert in
    /// <see cref="DateTimeOffset"/>. <see cref="DateTime.SpecifyKind"/> wird
    /// verwendet, damit der Wert nicht implizit in Local konvertiert wird.
    /// </summary>
    public static DateTimeOffset AssumeUtc(DateTime dt)
    {
        return new DateTimeOffset(DateTime.SpecifyKind(dt, DateTimeKind.Utc));
    }
}
