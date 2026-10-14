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

    public static bool TryResolveOutputPath(string configured, out string absolutePath, out string? errorCode, out string? errorMessage)
    {
        absolutePath = string.Empty;
        errorCode = null;
        errorMessage = null;

        if (string.IsNullOrWhiteSpace(configured))
        {
            errorCode = "output_invalid_path";
            errorMessage = "--output path is empty";
            return false;
        }

        if (OperatingSystem.IsWindows() && configured.StartsWith("/", StringComparison.Ordinal))
        {
            errorCode = "output_invalid_path";
            errorMessage = $"unsupported absolute unix-style path: '{configured}'";
            return false;
        }

        var rootOverride = Environment.GetEnvironmentVariable("BP_LOG_VIEWER_OUTPUT_ROOT");
        var anchorRoot = string.IsNullOrWhiteSpace(rootOverride)
            ? AppContext.BaseDirectory
            : rootOverride;
        anchorRoot = Path.GetFullPath(anchorRoot);

        if (Path.IsPathRooted(configured))
        {
            absolutePath = Path.GetFullPath(configured);
        }
        else
        {
            absolutePath = Path.GetFullPath(Path.Combine(anchorRoot, configured));

            var anchorWithSep = anchorRoot.EndsWith(Path.DirectorySeparatorChar)
                ? anchorRoot
                : anchorRoot + Path.DirectorySeparatorChar;

            if (!absolutePath.StartsWith(anchorWithSep, StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(absolutePath, anchorRoot, StringComparison.OrdinalIgnoreCase))
            {
                errorCode = "output_path_outside_root";
                errorMessage = $"resolved output path '{absolutePath}' is outside anchor '{anchorRoot}'";
                return false;
            }
        }

        if (Directory.Exists(absolutePath))
        {
            errorCode = "output_is_directory";
            errorMessage = $"output target is a directory: '{absolutePath}'";
            return false;
        }

        return true;
    }
}
