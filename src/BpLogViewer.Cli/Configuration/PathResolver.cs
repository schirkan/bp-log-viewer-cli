namespace BpLogViewer.Cli.Configuration;

/// <summary>
/// Zentrale Pfad-Resolution. Anker ist <c>AppContext.BaseDirectory</c> (NICHT
/// <c>Environment.CurrentDirectory</c>) — die Binary kann mit fremdem CWD gespawnt werden
/// (z. B. wenn ein KI-Agent sie über einen absoluten Pfad aufruft).
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

    /// <summary>
    /// Validiert einen <c>--output</c>-Pfad und gibt den absoluten Zielpfad zurück.
    /// Bei relativen Pfaden wird der <c>BP_LOG_VIEWER_OUTPUT_ROOT</c>-ENV-Override als Anchor genutzt,
    /// sonst <c>AppContext.BaseDirectory</c>. Pfade außerhalb des Anchors werden blockiert
    /// (Traversal-Schutz, z. B. <c>../../etc/passwd</c>).
    /// </summary>
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

        // Windows-only: absolute Unix-Style-Pfade wie /etc/passwd ablehnen.
        // Path.IsPathRooted ist plattformübergreifend korrekt (auf Linux ist "/" root,
        // auf Windows erkennt es "/foo" als NICHT-rooted, was wir hier explizit verbieten wollen).
        if (OperatingSystem.IsWindows() && configured.StartsWith("/", StringComparison.Ordinal))
        {
            errorCode = "output_invalid_path";
            errorMessage = "unsupported absolute unix-style path";
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
        }

        // Traversal-Schutz: Pfad darf nicht außerhalb des Anchors liegen
        // (gilt auch für absolute Pfade, falls BP_LOG_VIEWER_OUTPUT_ROOT gesetzt).
        var anchorWithSep = anchorRoot.EndsWith(Path.DirectorySeparatorChar)
            ? anchorRoot
            : anchorRoot + Path.DirectorySeparatorChar;

        if (!absolutePath.StartsWith(anchorWithSep, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(absolutePath, anchorRoot, StringComparison.OrdinalIgnoreCase))
        {
            errorCode = "output_path_outside_root";
            errorMessage = "resolved output path is outside the configured root";
            return false;
        }

        if (Directory.Exists(absolutePath))
        {
            errorCode = "output_is_directory";
            errorMessage = "output target is a directory";
            return false;
        }

        return true;
    }
}
