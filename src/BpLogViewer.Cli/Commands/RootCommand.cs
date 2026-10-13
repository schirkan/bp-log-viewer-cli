namespace BpLogViewer.Cli.Commands;

// Hinweis: Diese Datei enthält absichtlich keinen Code mehr.
//
// Hintergrund: System.CommandLine 2.0.0-beta4.22272.1 löst `Invoke` / `InvokeAsync`
// in dieser Umgebung nicht korrekt auf `RootCommand` auf. Wir sind daher
// auf einen manuellen Dispatcher in `Program.cs` umgestiegen. Diese Datei
// bleibt als Marker für die geplante Migration zurück zu System.CommandLine,
// sobald eine stabilere Version verfügbar ist.
//
// Wenn die Migration ansteht, hier die `RootCommandBuilder.Build()`-Methode
// reaktivieren und die Sub-Commands via `rootCommand.Add(new VersionCommand()) etc.
// einhängen. Siehe F-014 § KI-Integration für das Deployment-Pattern.
internal static class RootCommandBuilder
{
    public static void NotInUse() { }
}
