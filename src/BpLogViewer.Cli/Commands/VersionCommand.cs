using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using BpLogViewer.Cli.Output;

namespace BpLogViewer.Cli.Commands;

/// <summary>
/// <c>bp-log-viewer version</c> — gibt CLI-Version, Build-Commit und Build-Datum als jsonl aus.
/// Spec: <c>specs/feature-F-014-self-test.md</c> Sub-Command 1.
/// </summary>
/// <remarks>
/// Früher von <c>System.CommandLine.Command</c> abgeleitet mit <c>SetHandler</c>.
/// Da wir aktuell einen manuellen Dispatcher in <c>Program.cs</c> verwenden
/// (System.CommandLine 2.0.0-beta4 hat in dieser Umgebung
/// Invoke-Resolution-Probleme), ist dieser Handler jetzt eine statische Methode.
/// Migration zurück zu System.CommandLine ist später möglich, sobald wir auf
/// eine stabilere Version umstellen.
/// </remarks>
public static class VersionCommand
{
    public static int Execute()
    {
        var assembly = typeof(Program).Assembly;
        var informationalVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
                                  ?? assembly.GetName().Version?.ToString()
                                  ?? "0.0.0";
        // InformationalVersion is often "<version>+<sha>" — strip build metadata
        var version = informationalVersion.Split('+', 2)[0];
        var buildCommit = informationalVersion.Contains('+')
            ? informationalVersion.Split('+', 2)[1]
            : (Environment.GetEnvironmentVariable("GIT_COMMIT") ?? "local");
        var buildDate = File.Exists(assembly.Location)
            ? File.GetLastWriteTimeUtc(assembly.Location).ToString("yyyy-MM-ddTHH:mm:ssZ")
            : "unknown";

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
