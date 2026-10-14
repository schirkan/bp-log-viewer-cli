namespace BpLogViewer.Cli.Configuration;

/// <summary>
/// Zentrale Pfad-Resolution. Anker ist <c>AppContext.BaseDirectory</c> (NICHT
/// <c>Environment.CurrentDirectory</c>) — der KI-Agent spawnt die Binary mit
/// fremdem CWD (siehe Skill <c>dotnet-config-path-anchoring</c> + architecture § 4.2).
/// </summary>
public static class PathResolver
{
    public static string Resolve(string? configured, string? baseDir = null)
    {
        baseDir ??= AppContext.BaseDirectory;
        if (string.IsNullOrWhiteSpace(configured)) return baseDir;
        return Path.IsPathRooted(configured)
            ? Path.GetFullPath(configured)
            : Path.GetFullPath(Path.Combine(baseDir, configured));
    }
}
