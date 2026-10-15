using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using BpLogViewer.Cli.Output;

namespace BpLogViewer.Cli.Commands;

/// <summary>
/// <c>bp-log-viewer version</c> — gibt CLI-Version, Build-Commit und Build-Datum als jsonl aus.
/// </summary>
public static class VersionCommand
{
    public static int Execute()
    {
        var assembly = typeof(Program).Assembly;
        // IL3000: `Assembly.Location` kann in single-file-EXE leer sein.
#pragma warning disable IL3000
        var location = assembly.Location;
#pragma warning restore IL3000
        var informationalVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
                                  ?? assembly.GetName().Version?.ToString()
                                  ?? "0.0.0";
        var version = informationalVersion.Split('+', 2)[0];
        var buildCommit = informationalVersion.Contains('+')
            ? informationalVersion.Split('+', 2)[1]
            : (Environment.GetEnvironmentVariable("GIT_COMMIT") ?? "local");
        var buildDate = !string.IsNullOrEmpty(location) && File.Exists(location)
            ? File.GetLastWriteTimeUtc(location).ToString("yyyy-MM-ddTHH:mm:ssZ")
            : (Environment.GetEnvironmentVariable("BUILD_DATE") ?? "unknown");

        var info = new VersionInfo(
            CliVersion: version,
            BuildCommit: buildCommit,
            BuildDate: buildDate,
            TargetFramework: "net8.0",
            Runtime: RuntimeInformation.RuntimeIdentifier);

        var json = JsonSerializer.Serialize(info, PayloadJsonContext.Default.VersionInfo);
        Console.WriteLine(json);
        return 0;
    }
}

/// <summary>VersionInfo wird via Source-Generator in snake_case serialisiert.</summary>
public record VersionInfo(
    string CliVersion,
    string BuildCommit,
    string BuildDate,
    string TargetFramework,
    string Runtime);
