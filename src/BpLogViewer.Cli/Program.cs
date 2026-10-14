using BpLogViewer.Cli.Commands;
using BpLogViewer.Cli.Filters;
using BpLogViewer.Cli.Output;
using FilterSet = BpLogViewer.Cli.Filters.Filters;

namespace BpLogViewer.Cli;

/// <summary>
/// Entry point for the BP Log Viewer CLI.
/// See <c>specs/architecture.md</c> for the overall design and
/// <c>specs/feature-F-013-output-format.md</c> for the output contract.
/// </summary>
/// <remarks>
/// Verwendet einen minimalen manuellen Dispatcher statt System.CommandLine,
/// weil System.CommandLine 2.0.0-beta4.22272.1 in dieser Umgebung
/// <c>Invoke</c>/<c>InvokeAsync</c> auf <c>RootCommand</c> nicht korrekt auflöst.
/// Sobald das CLI-Layout komplexer wird (mehr Sub-Commands, viele Optionen),
/// migrieren wir zurück auf System.CommandLine mit einer stabileren Version.
/// </remarks>
public static class Program
{
    /// <summary>Process entry point. Returns the CLI exit code (see architecture § 8).</summary>
    public static int Main(string[] args)
    {
        try
        {
            if (args.Length == 0)
            {
                return PrintRootHelp();
            }

            // Split: globale Optionen (vor Sub-Command) | Sub-Command | Sub-Command-Args
            var (subCommand, globalArgs, subArgs) = SplitArgs(args);

            // F-011: globale Filter/Optionen validieren.
            // Zusätzlich parse über subArgs, damit globale Optionen auch hinter dem Command
            // funktionieren (z. B. "version --since ...").
            var allOptionArgs = globalArgs.Concat(subArgs).ToArray();
            var filters = FilterParser.Parse(allOptionArgs);

            return subCommand switch
            {
                "" or "help" or "--help" or "-h" => PrintRootHelp(),
                "version" or "--version" or "-v" => VersionCommand.Execute(),
                "self-test" => SelfTestCommand.Execute(filters),
                "session" => DispatchSession(subArgs, filters),
                "resource" => DispatchResource(subArgs),
                "process" => DispatchProcess(subArgs),
                "queue" => DispatchQueue(subArgs),
                "audit" => DispatchAudit(subArgs),
                _ => UnknownCommand(subCommand)
            };
        }
        catch (FilterValidationException ex)
        {
            ErrorWriter.WriteErrorAsync(ex.Code, ex.Message, ex.ExitCode).GetAwaiter().GetResult();
            return ex.ExitCode;
        }
        catch (Exception ex)
        {
            ErrorWriter.WriteErrorAsync("unhandled_exception", ex.Message, 1).GetAwaiter().GetResult();
            return 1;
        }
    }

    /// <summary>
    /// Trennt <paramref name="args"/> in (Sub-Command-Name, globale Args, Sub-Command-Args).
    /// Das erste Argument, das nicht mit <c>--</c> oder <c>-</c> beginnt, ist der Sub-Command.
    /// </summary>
    private static (string subCommand, string[] globalArgs, string[] subArgs) SplitArgs(string[] args)
    {
        // Globale Optionen mit Value dürfen nicht als Sub-Command interpretiert werden.
        // Beispiel: --max-rows 1000 version
        var optionsWithValue = new HashSet<string>(StringComparer.Ordinal)
        {
            "--since", "--until", "--process", "--resource", "--status", "--level", "--text",
            "--max-rows", "--format", "--output", "--out-file", "--connection-string"
        };

        for (int i = 0; i < args.Length; i++)
        {
            var token = args[i];
            if (optionsWithValue.Contains(token))
            {
                i++; // zugehörigen Wert überspringen
                continue;
            }

            if (token.StartsWith("-", StringComparison.Ordinal))
            {
                continue;
            }

            return (token, args[..i], args[(i + 1)..]);
        }

        // Kein Sub-Command — alle Args sind global
        return ("", args, Array.Empty<string>());
    }

    private static int PrintRootHelp()
    {
        Console.Error.WriteLine("bp-log-viewer — Structured, AI-friendly log access to Blue Prism 7.5.x database");
        Console.Error.WriteLine("");
        Console.Error.WriteLine("Usage: bp-log-viewer [global-options] <command> [command-options]");
        Console.Error.WriteLine("");
        Console.Error.WriteLine("Commands:");
        Console.Error.WriteLine("  version           Show CLI version and build information (jsonl)");
        Console.Error.WriteLine("  self-test         Sanity-check config, DB connection, tables (F-014)");
        Console.Error.WriteLine("  session ...       Session list / get / log / errors (F-002 .. F-005)");
        Console.Error.WriteLine("  resource list     List resources (F-006)");
        Console.Error.WriteLine("  process list      List processes (F-007)");
        Console.Error.WriteLine("  queue ...         Work queues (F-008) + items (F-009)");
        Console.Error.WriteLine("  audit list        Audit events (F-010)");
        Console.Error.WriteLine("");
        Console.Error.WriteLine("Global options (F-011 + architecture § 5):");
        Console.Error.WriteLine("  --connection-string <cs>  --output <path>  --format jsonl|table");
        Console.Error.WriteLine("  --max-rows <n>  --since <iso>  --until <iso>");
        Console.Error.WriteLine("  --process <name>  --resource <name>  --status <code-or-name>");
        Console.Error.WriteLine("  --has-exception  --level <lvl>  --text <substr>  --verbose");
        Console.Error.WriteLine("");
        Console.Error.WriteLine("Status: Specs ✅ freigegeben. Implementation: F-013 ✅ · F-011 ✅ · F-001/F-002/F-003/F-004/F-005 🚧.");
        return 0;
    }

    private static int NotYetImplemented(string command, string spec)
    {
        ErrorWriter.WriteErrorAsync(
            "not_implemented",
            $"Command '{command}' ist in der Spec ({spec}) definiert, aber noch nicht implementiert. " +
            "Folge-Reihenfolge: F-003 → F-004 → F-005 → F-012 → F-006 → F-007 → F-008 → F-009 → F-010.",
            0).GetAwaiter().GetResult();
        return 0;
    }

    private static int UnknownCommand(string command)
    {
        ErrorWriter.WriteErrorAsync("unknown_command", $"Unbekannter Command: '{command}'. 'bp-log-viewer' ohne Argumente zeigt die Hilfe.", 11).GetAwaiter().GetResult();
        return 11;
    }

    private static int DispatchSession(string[] subArgs, FilterSet filters)
    {
        if (subArgs.Length == 0 || subArgs[0] is "help" or "--help" or "-h")
        {
            return NotYetImplemented("session", "F-002..F-005");
        }

        return subArgs[0] switch
        {
            "list" => SessionListCommand.Execute(filters),
            "get" => SessionGetCommand.Execute(subArgs.Skip(1).ToArray(), filters),
            "log" => SessionLogCommand.Execute(subArgs.Skip(1).ToArray(), filters),
            "errors" => SessionErrorsCommand.Execute(subArgs.Skip(1).ToArray(), filters),
            _ => UnknownCommand($"session {subArgs[0]}")
        };
    }

    private static int DispatchResource(string[] _) => NotYetImplemented("resource", "F-006");
    private static int DispatchProcess(string[] _) => NotYetImplemented("process", "F-007");
    private static int DispatchQueue(string[] _) => NotYetImplemented("queue", "F-008, F-009");
    private static int DispatchAudit(string[] _) => NotYetImplemented("audit", "F-010");
}
