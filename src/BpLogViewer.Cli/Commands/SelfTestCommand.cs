using BpLogViewer.Cli.Configuration;
using BpLogViewer.Cli.Data;
using BpLogViewer.Cli.Output;
using System.Diagnostics;
// Alias, weil der Namespace `BpLogViewer.Cli.Filters` und der Typ `Filters`
// denselben Namen tragen — ohne Alias interpretiert der Compiler `Filters`
// als Namespace statt als Typ (CS0118).
using FilterSet = BpLogViewer.Cli.Filters.Filters;

namespace BpLogViewer.Cli.Commands;

/// <summary>
/// <c>bp-log-viewer self-test</c> — Sanity-Check (F-014 Sub-Command 2).
/// Implementiert für F-001 die ersten 3 Schritte (Config → LocalDB → DB-Connect).
/// Weitere Schritte (BP-Version, Tabellen-Check, aktive Log-Tabelle, Write-Test)
/// folgen mit F-014 komplett.
/// </summary>
public static class SelfTestCommand
{
    public static int Execute(FilterSet filters, string? baseDir = null)
    {
        var sw = Stopwatch.StartNew();
        var (configStatus, configMsg, settings) = LoadConfig(baseDir);
        Console.WriteLine($"{{\"step\":\"config\",\"status\":\"{configStatus}\",\"detail\":\"{configMsg}\"}}");
        if (configStatus != "ok") return 3;

        var (localdbStatus, localdbMsg) = StartLocalDbIfNeeded(settings);
        Console.WriteLine($"{{\"step\":\"localdb\",\"status\":\"{localdbStatus}\",\"detail\":\"{localdbMsg}\"}}");
        if (localdbStatus == "start_failed") return 4;

        var (dbStatus, dbMsg, dbVersion) = TryConnect(settings);
        Console.WriteLine($"{{\"step\":\"db_connect\",\"status\":\"{dbStatus}\",\"db_version\":\"{dbVersion}\",\"detail\":\"{dbMsg}\"}}");
        if (dbStatus != "ok") return 5;

        // StatusCache smoke-test
        var (statusStatus, statusMsg, statusCount) = TryLoadStatusCache(settings);
        Console.WriteLine($"{{\"step\":\"status_cache\",\"status\":\"{statusStatus}\",\"count\":{statusCount},\"detail\":\"{statusMsg}\"}}");

        sw.Stop();
        var overall = (dbStatus == "ok" && statusStatus == "ok") ? "ok" : "degraded";
        Console.WriteLine($"{{\"self_test\":\"{overall}\",\"duration_ms\":{sw.ElapsedMilliseconds}}}");
        return overall == "ok" ? 0 : 1;
    }

    private static (string status, string msg, AppSettings settings) LoadConfig(string? baseDir)
    {
        try
        {
            var settings = ConfigurationLoader.Load(baseDir);
            if (settings.Database.ConnectionString is null or { Length: 0 })
                return ("error", "appsettings.json nicht gefunden oder Database.ConnectionString leer", settings);
            return ("ok", $"connection_string_length={settings.Database.ConnectionString.Length}", settings);
        }
        catch (Exception ex)
        {
            return ("error", ex.Message, new AppSettings());
        }
    }

    private static (string status, string msg) StartLocalDbIfNeeded(AppSettings settings)
    {
        var instance = settings.Database.LocalDbInstance;
        if (string.IsNullOrEmpty(instance))
            return ("not_configured", "kein LocalDbInstance in Config");
        try
        {
            var psi = new ProcessStartInfo("sqllocaldb", $"start \"{instance}\"")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var proc = Process.Start(psi);
            proc?.WaitForExit(10_000);
            return ("ok", $"sqllocaldb start {instance} beendet");
        }
        catch (Exception ex)
        {
            return ("start_failed", ex.Message);
        }
    }

    private static (string status, string msg, string version) TryConnect(AppSettings settings)
    {
        try
        {
            using var conn = new Microsoft.Data.SqlClient.SqlConnection(settings.Database.ConnectionString);
            conn.Open();
            using var cmd = new Microsoft.Data.SqlClient.SqlCommand("SELECT @@VERSION", conn);
            var version = cmd.ExecuteScalar() as string ?? "(null)";
            return ("ok", "connection established", version.Split('\n')[0]);
        }
        catch (Exception ex)
        {
            return ("error", ex.Message, "(none)");
        }
    }

    private static (string status, string msg, int count) TryLoadStatusCache(AppSettings settings)
    {
        try
        {
            var cache = new StatusCache();
            cache.Load(settings);
            return ("ok", $"{cache.Count} status entries aus BPAStatus geladen", cache.Count);
        }
        catch (Exception ex)
        {
            return ("error", ex.Message, 0);
        }
    }
}
