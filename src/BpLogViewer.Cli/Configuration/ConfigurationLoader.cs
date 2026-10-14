using Microsoft.Extensions.Configuration;

namespace BpLogViewer.Cli.Configuration;

/// <summary>
/// Lädt <see cref="AppSettings"/> aus <c>appsettings.json</c> + ENV-Variablen
/// + CLI-Override (architecture § 4.3 Override-Reihenfolge).
/// </summary>
/// <remarks>
/// Sucht <c>appsettings.json</c> in dieser Reihenfolge:
/// <list type="number">
///   <item><c>AppContext.BaseDirectory</c> — Production: neben der Binary.</item>
///   <item><c>Environment.CurrentDirectory</c> — Dev-Workflow (<c>dotnet run</c>).</item>
///   <item><c>baseDir</c> (explizit) — Test-Isolation.</item>
/// </list>
/// </remarks>
public static class ConfigurationLoader
{
    /// <summary>Override-Priorität: CLI > ENV > appsettings.json.</summary>
    /// <param name="baseDir">Optionales explizites Verzeichnis (z. B. für Tests).</param>
    /// <param name="cliConnectionString">Wert aus <c>--connection-string</c> (höchste Prio).</param>
    public static AppSettings Load(string? baseDir = null, string? cliConnectionString = null)
    {
        var configPath = ResolveConfigPath(baseDir);

        var builder = new ConfigurationBuilder();
        if (File.Exists(configPath))
        {
            builder.AddJsonFile(configPath, optional: false, reloadOnChange: false);
        }
        // ENV-Variablen mit Prefix BP_ (z. B. BP_BpLogViewer__Database__ConnectionString
        // → BpLogViewer:Database:ConnectionString)
        builder.AddEnvironmentVariables(prefix: "BP_");

        var settings = new AppSettings();
        // Wichtig: appsettings.json hat die Section "BpLogViewer" als Root.
        // ConfigurationBuilder.Bind bindet die Root-Section des IConfiguration
        // (die per Default leer ist), NICHT eine benannte Section. Deshalb
        // GetSection("BpLogViewer") vor dem Bind.
        builder.Build().GetSection("BpLogViewer").Bind(settings);

        // Expliziter ENV-Override für Connection-String (architecture § 4.3)
        var envConn = Environment.GetEnvironmentVariable("BP_DB_CONNECTION");
        if (!string.IsNullOrEmpty(envConn))
            settings.Database.ConnectionString = envConn;

        // CLI-Override (höchste Priorität)
        if (!string.IsNullOrEmpty(cliConnectionString))
            settings.Database.ConnectionString = cliConnectionString;

        return settings;
    }

    /// <summary>Sucht <c>appsettings.json</c> in mehreren Verzeichnissen (siehe Klassen-XML-Doc).</summary>
    private static string ResolveConfigPath(string? baseDir)
    {
        var candidates = new List<string>();
        if (baseDir != null) candidates.Add(baseDir);
        candidates.Add(AppContext.BaseDirectory);
        candidates.Add(Environment.CurrentDirectory);

        // Auch im Parent von CWD suchen (Dev-Workflow: `dotnet run` aus dem
        // Projekt-Dir, appsettings.json liegt am Workspace-Root).
        if (Environment.CurrentDirectory is { Length: > 0 } cwd)
        {
            var parent = Directory.GetParent(cwd)?.FullName;
            if (parent != null) candidates.Add(parent);
        }

        foreach (var dir in candidates)
        {
            if (string.IsNullOrEmpty(dir)) continue;
            var candidate = PathResolver.Resolve("appsettings.json", dir);
            if (File.Exists(candidate)) return candidate;
        }

        // Fallback: erstes Verzeichnis (führt zu „Datei nicht gefunden" in Configuration)
        return PathResolver.Resolve("appsettings.json", baseDir ?? AppContext.BaseDirectory);
    }
}
