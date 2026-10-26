using BpLogViewer.Cli.Configuration;
using BpLogViewer.Cli.Data;
using BpLogViewer.Cli.Filters;
using BpLogViewer.Cli.Output;
using System.Text.Json.Serialization.Metadata;
using FilterSet = BpLogViewer.Cli.Filters.Filters;

namespace BpLogViewer.Cli.Commands;

/// <summary>
/// Gemeinsame Hilfslogik für List-/Detail-Commands: Config laden,
/// <see cref="StatusCache"/> initialisieren, Output (stdout oder Datei) ausführen.
/// Eliminiert Copy-Paste in den einzelnen Command-Klassen (DRY).
/// </summary>
internal static class CommandHost
{
    /// <summary>
    /// Lädt <see cref="AppSettings"/> und validiert die DB-Verbindungs-Konfiguration.
    /// Schreibt bei Fehler eine jsonl-Error-Envelope und gibt false zurück.
    /// </summary>
    public static bool TryLoadSettings(FilterSet filters, out AppSettings settings)
    {
        settings = ConfigurationLoader.Load(cliConnectionString: filters.ConnectionString);
        if (string.IsNullOrEmpty(settings.Database.ConnectionString))
        {
            ErrorWriter.WriteErrorAsync(
                "config_invalid",
                "appsettings.json nicht gefunden oder Database.ConnectionString leer. Siehe self-test für Diagnose.",
                3).GetAwaiter().GetResult();
            return false;
        }
        return true;
    }

    /// <summary>Lädt den <see cref="StatusCache"/>; gibt false bei Fehler zurück.</summary>
    public static bool TryLoadStatusCache(AppSettings settings, out StatusCache cache)
    {
        cache = new StatusCache();
        try
        {
            cache.Load(settings);
            return true;
        }
        catch (Exception ex)
        {
            ErrorWriter.WriteErrorAsync("status_cache_load_failed", ex.Message, 5).GetAwaiter().GetResult();
            return false;
        }
    }

    /// <summary>
    /// Effektives Max-Rows: bei <c>--output</c> ohne explizites Limit → Hard-Ceiling;
    /// sonst Filters.MaxRows oder Fallback.
    /// </summary>
    public static int ResolveEffectiveMax(FilterSet filters, int fallbackDefault = 0)
    {
        if (filters.Output is { Length: > 0 } && !filters.MaxRows.HasValue)
        {
            return FilterSet.MaxRowsAbsoluteCeiling;
        }
        if (filters.MaxRows.HasValue)
        {
            return filters.MaxRows.Value;
        }
        return fallbackDefault > 0 ? fallbackDefault : FilterSet.DefaultMaxRows;
    }

    /// <summary>
    /// Kürzt die Liste um die Overflow-Zeile und schreibt die truncate-Warning.
    /// Gibt true zurück, wenn truncated.
    /// </summary>
    public static bool Truncate<T>(List<T> rows, int effectiveMax)
    {
        if (rows.Count <= effectiveMax)
        {
            return false;
        }

        rows.RemoveAt(rows.Count - 1);
        ErrorWriter.WriteWarningAsync(
            "output_truncated",
            $"{effectiveMax} of {effectiveMax}+ rows. Use --output to write full result.").GetAwaiter().GetResult();
        return true;
    }

    /// <summary>
    /// Schreibt Rows als jsonl: bei <c>--output</c> in Datei + Marker auf stdout,
    /// sonst zeilenweise nach stdout. Returnt den Exit-Code (0/2/6).
    /// </summary>
    public static int Emit<T>(IReadOnlyList<T> rows, JsonTypeInfo<T> typeInfo, FilterSet filters, bool wasTruncated, string? headerJson = null)
    {
        if (!string.IsNullOrWhiteSpace(filters.Output))
        {
            if (!OutputFileWriter.TryWriteJsonLines(rows, typeInfo, filters.Output, out var code, out var message, headerJson))
            {
                ErrorWriter.WriteErrorAsync(code ?? "output_write_failed", message ?? "failed to write output file", 6).GetAwaiter().GetResult();
                return 6;
            }
            return wasTruncated ? 2 : 0;
        }

        if (headerJson is not null)
            Console.WriteLine(headerJson);

        foreach (var row in rows)
        {
            Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(row, typeInfo));
        }
        return wasTruncated ? 2 : 0;
    }

    /// <summary>Schreibt einen DB-Fehler als jsonl-Envelope auf stderr. Returnt 5.</summary>
    public static int WriteDbError(Exception ex)
    {
        ErrorWriter.WriteErrorAsync("db_error", ex.Message, 5).GetAwaiter().GetResult();
        return 5;
    }
}
