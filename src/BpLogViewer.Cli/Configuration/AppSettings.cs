namespace BpLogViewer.Cli.Configuration;

/// <summary>
/// Schema für <c>appsettings.json</c>. Wird per
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

    /// <summary>Optionaler LocalDB-Instanzname; CLI versucht <c>sqllocaldb start</c>, falls nicht erreichbar.</summary>
    public string? LocalDbInstance { get; set; }
}

public sealed class OutputSettings
{
    /// <summary><c>jsonl</c> | <c>table</c>.</summary>
    public string DefaultFormat { get; set; } = "jsonl";

    /// <summary>Schwellwert für Hinweis „Output zu groß, in Datei auslagern".</summary>
    public int MaxRowsBeforeFile { get; set; } = 1_000;

    /// <summary>Hard-Cap für <c>--max-rows</c> (Exit 8 bei Überschreitung).</summary>
    public int MaxRowsAbsoluteCeiling { get; set; } = 100_000;
}

public sealed class TimezoneSettings
{
    /// <summary>IANA-TZ für Anzeige.</summary>
    public string DisplayTimezone { get; set; } = "Europe/Berlin";
}

/// <summary>
/// Schwellwerte für die Queue-Stale-Erkennung.
/// Eine Queue gilt als "stale", wenn das älteste Pending-Item älter als
/// <see cref="StalePendingThresholdHours"/> ist.
/// </summary>
public static class QueueThresholds
{
    /// <summary>24h — Schwellwert für <c>queue list</c> <c>stale</c>-Flag.</summary>
    public const int StalePendingThresholdHours = 24;
}
