namespace BpLogViewer.Cli.Configuration;

/// <summary>
/// Schema für <c>appsettings.json</c> (architecture § 4.1). Wird per
/// <see cref="ConfigurationLoader"/> aus Datei + ENV-Variablen geladen.
/// </summary>
public sealed class AppSettings
{
    public DatabaseSettings Database { get; set; } = new();
    public OutputSettings Output { get; set; } = new();
    public TimezoneSettings Timezone { get; set; } = new();
}

public sealed class DatabaseSettings
{
    /// <summary>Vollständiger ADO.NET-Connection-String (Pflicht).</summary>
    public string? ConnectionString { get; set; }

    /// <summary>Optionaler LocalDB-Instanzname; CLI versucht <c>sqllocaldb start</c>, falls nicht erreichbar (F-001 § 4).</summary>
    public string? LocalDbInstance { get; set; }
}

public sealed class OutputSettings
{
    /// <summary><c>jsonl</c> | <c>table</c> (F-013).</summary>
    public string DefaultFormat { get; set; } = "jsonl";

    /// <summary>Schwellwert für Hinweis „Output zu groß, in Datei auslagern" (architecture § 4.1).</summary>
    public int MaxRowsBeforeFile { get; set; } = 1_000;

    /// <summary>Hard-Cap für <c>--max-rows</c> (architecture § 4.1 + § 8 Exit 8).</summary>
    public int MaxRowsAbsoluteCeiling { get; set; } = 100_000;
}

public sealed class TimezoneSettings
{
    /// <summary>IANA-TZ für Anzeige (architecture § 4.4).</summary>
    public string DisplayTimezone { get; set; } = "Europe/Berlin";
}

/// <summary>
/// Schwellwerte für die Queue-Stale-Erkennung (Code-Review-Finding O6).
/// Eine Queue gilt als "stale", wenn das älteste Pending-Item älter als
/// <see cref="StalePendingThresholdHours"/> ist.
/// </summary>
public static class QueueThresholds
{
    /// <summary>24h — Schwellwert für <c>queue list</c> <c>stale</c>-Flag (F-008).</summary>
    public const int StalePendingThresholdHours = 24;
}
