using System.Data;
using System.Diagnostics;
using System.Globalization;
using BpLogViewer.Cli.Configuration;
using BpLogViewer.Cli.Data;
using BpLogViewer.Cli.Output;
using Microsoft.Data.SqlClient;
using Microsoft.Win32;
using FilterSet = BpLogViewer.Cli.Filters.Filters;

namespace BpLogViewer.Cli.Commands;

/// <summary>
/// <c>bp-log-viewer self-test</c> — Sanity-Check (F-014).
/// </summary>
public static class SelfTestCommand
{
    public static int Execute(FilterSet filters, string? baseDir = null)
    {
        var sw = Stopwatch.StartNew();
        var degraded = false;

        var (configStatus, configMsg, settings) = LoadConfig(baseDir);
        Console.WriteLine($"{{\"step\":\"config\",\"status\":\"{configStatus}\",\"detail\":\"{Escape(configMsg)}\"}}");
        if (configStatus != "ok") return 3;

        var (localdbStatus, localdbMsg) = StartLocalDbIfNeeded(settings);
        Console.WriteLine($"{{\"step\":\"localdb\",\"status\":\"{localdbStatus}\",\"detail\":\"{Escape(localdbMsg)}\"}}");
        if (localdbStatus == "start_failed") return 4;
        if (localdbStatus is "not_configured" or "not_running") degraded = true;

        var (dbStatus, dbMsg, dbVersion) = TryConnect(settings);
        Console.WriteLine($"{{\"step\":\"db_connect\",\"status\":\"{dbStatus}\",\"db_version\":\"{Escape(dbVersion)}\",\"detail\":\"{Escape(dbMsg)}\"}}");
        if (dbStatus != "ok") return 5;

        var (statusStatus, statusMsg, statusCount) = TryLoadStatusCache(settings);
        Console.WriteLine($"{{\"step\":\"status_cache\",\"status\":\"{statusStatus}\",\"count\":{statusCount},\"detail\":\"{Escape(statusMsg)}\"}}");
        if (statusStatus != "ok") degraded = true;

        var (bpVersionStatus, bpVersion, bpSource, bpDetail) = TryGetBpVersion(settings);
        Console.WriteLine($"{{\"step\":\"bp_version\",\"status\":\"{bpVersionStatus}\",\"bp_version\":\"{Escape(bpVersion)}\",\"source\":\"{Escape(bpSource)}\",\"detail\":\"{Escape(bpDetail)}\"}}");
        if (bpVersionStatus != "ok") degraded = true;

        var (tablesStatus, tablesJson, tablesDetail) = CheckCoreTables(settings);
        Console.WriteLine($"{{\"step\":\"tables\",\"status\":\"{tablesStatus}\",\"tables\":{tablesJson},\"detail\":\"{Escape(tablesDetail)}\"}}");
        if (tablesStatus != "ok") degraded = true;

        var (logTableStatus, activeTable, logTableDetail) = DetectActiveLogTable(settings);
        Console.WriteLine($"{{\"step\":\"active_log_table\",\"status\":\"{logTableStatus}\",\"active_log_table\":\"{Escape(activeTable)}\",\"detail\":\"{Escape(logTableDetail)}\"}}");
        if (logTableStatus != "ok") degraded = true;

        var (writeStatus, writeDetail) = WriteTestFile();
        Console.WriteLine($"{{\"step\":\"write_test\",\"status\":\"{writeStatus}\",\"detail\":\"{Escape(writeDetail)}\"}}");
        if (writeStatus != "ok") degraded = true;

        sw.Stop();
        var overall = degraded ? "degraded" : "ok";
        Console.WriteLine($"{{\"self_test\":\"{overall}\",\"duration_ms\":{sw.ElapsedMilliseconds}}}");
        return 0;
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
            var exitCode = proc?.ExitCode ?? 1;
            if (exitCode == 0)
            {
                return ("ok", $"sqllocaldb start {instance} beendet");
            }

            var err = proc?.StandardError.ReadToEnd() ?? "sqllocaldb start returned non-zero";
            return ("not_running", err.Trim());
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
            using var conn = new SqlConnection(settings.Database.ConnectionString);
            conn.Open();
            using var cmd = new SqlCommand("SELECT @@VERSION", conn);
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

    private static (string status, string version, string source, string detail) TryGetBpVersion(AppSettings settings)
    {
        try
        {
            using var conn = new SqlConnection(settings.Database.ConnectionString);
            conn.Open();
            using var cmd = new SqlCommand("SELECT TOP 1 dbversion FROM BPADBVersion ORDER BY scriptrundate DESC", conn);
            var dbValue = cmd.ExecuteScalar()?.ToString();
            if (!string.IsNullOrWhiteSpace(dbValue))
            {
                return ("ok", dbValue, "db", "db schema version from BPADBVersion");
            }
        }
        catch
        {
            // registry fallback below
        }

        try
        {
            if (OperatingSystem.IsWindows())
            {
                using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Blue Prism Limited\Automate");
                var regValue = key?.GetValue("Version")?.ToString();
                if (!string.IsNullOrWhiteSpace(regValue))
                {
                    return ("ok", regValue, "registry", "version from HKLM");
                }
            }
        }
        catch (Exception ex)
        {
            return ("error", "(unknown)", "none", ex.Message);
        }

        return ("error", "(unknown)", "none", "version not found in DB and registry");
    }

    private static (string status, string tablesJson, string detail) CheckCoreTables(AppSettings settings)
    {
        var tableNames = new[]
        {
            "BPASession", "BPASessionLog_NonUnicode", "BPASessionLog_Unicode",
            "BPAProcess", "BPAResource", "BPAWorkQueue", "BPAAuditEvents"
        };

        try
        {
            using var conn = new SqlConnection(settings.Database.ConnectionString);
            conn.Open();

            var state = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var table in tableNames)
            {
                using var cmd = new SqlCommand("SELECT COUNT(1) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME=@name", conn);
                cmd.Parameters.Add(new SqlParameter("@name", SqlDbType.NVarChar, 128) { Value = table });
                var exists = Convert.ToInt32(cmd.ExecuteScalar() ?? 0, CultureInfo.InvariantCulture) > 0;
                state[table] = exists ? "ok" : "missing";
            }

            var missing = state.Where(x => x.Value == "missing").Select(x => x.Key).ToArray();
            var status = missing.Length == 0 ? "ok" : "degraded";
            var detail = missing.Length == 0 ? "all required tables found" : $"missing: {string.Join(",", missing)}";

            var json = "{" + string.Join(',', state.Select(x => $"\"{x.Key}\":\"{x.Value}\"")) + "}";
            return (status, json, detail);
        }
        catch (Exception ex)
        {
            return ("error", "{}", ex.Message);
        }
    }

    private static (string status, string activeTable, string detail) DetectActiveLogTable(AppSettings settings)
    {
        try
        {
            using var db = new BpDbContext(settings);
            var repo = new SessionLogRepository(db);
            var table = repo.ResolveActiveLogTable();
            if (table == SessionLogRepository.NonUnicodeTable) return ("ok", "NonUnicode", table);
            if (table == SessionLogRepository.UnicodeTable) return ("ok", "Unicode", table);
            return ("ok", "none", "both log tables empty");
        }
        catch (Exception ex)
        {
            return ("error", "unknown", ex.Message);
        }
    }

    private static (string status, string detail) WriteTestFile()
    {
        try
        {
            var tempDir = PathResolver.Resolve("temp", AppContext.BaseDirectory);
            Directory.CreateDirectory(tempDir);
            var file = Path.Combine(tempDir, $"self-test-write-{DateTimeOffset.UtcNow:yyyyMMddHHmmssfff}.json");
            File.WriteAllText(file, "{\"write_test\":\"ok\"}");
            File.Delete(file);
            return ("ok", "write+delete in temp succeeded");
        }
        catch (Exception ex)
        {
            return ("failed", ex.Message);
        }
    }

    private static string Escape(string value)
    {
        return value.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", " ").Replace("\n", " ");
    }
}
