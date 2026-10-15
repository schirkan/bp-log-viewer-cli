using BpLogViewer.Cli.Commands;
using BpLogViewer.Cli.Filters;
using BpLogViewer.Cli.Output;
using FilterSet = BpLogViewer.Cli.Filters.Filters;

namespace BpLogViewer.Cli;

/// <summary>
/// Entry point for the BP Log Viewer CLI.
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
    /// <summary>Process entry point. Returns the CLI exit code.</summary>
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

            // Globale Filter/Optionen validieren. Wichtig: globale Optionen
            // werden aus ALLEN Args geparst (nicht nur globalArgs), damit z. B.
            // "process list --max-rows 1" genauso funktioniert wie
            // "--max-rows 1 process list". Der Sub-Command-Name und Sub-Args
            // werden von FilterParser ignoriert (siehe default-Branch).
            // Sub-Args werden vom jeweiligen Command-Parser verarbeitet, der
            // globale Optionen via FilterParser.OptionsWithValue überspringt.
            var filters = FilterParser.Parse(args);

            // Version-Flag wird gegen die bereits geparsten globalen Args geprüft,
            // damit "session list --version" korrekt als unknown_command behandelt wird
            // (und nicht fälschlich "version" aufruft).
            var wantsVersionFlag = globalArgs.Any(a => a is "--version" or "-v");

            return subCommand switch
            {
                "" when wantsVersionFlag => VersionCommand.Execute(),
                "" or "help" or "--help" or "-h" => PrintRootHelp(),
                "version" or "--version" or "-v" => VersionCommand.Execute(),
                "self-test" => SelfTestCommand.Execute(filters),
                "session" => DispatchSession(subArgs, filters),
                "resource" => DispatchResource(subArgs, filters),
                "process" => DispatchProcess(subArgs, filters),
                "queue" => DispatchQueue(subArgs, filters),
                "audit" => DispatchAudit(subArgs, filters),
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
        Console.Error.WriteLine("  self-test         Sanity-check config, DB connection, tables");
        Console.Error.WriteLine("  session ...       Session list / get / log / errors");
        Console.Error.WriteLine("  resource list     List resources");
        Console.Error.WriteLine("  process list      List processes");
        Console.Error.WriteLine("  queue ...         Work queues + items");
        Console.Error.WriteLine("  audit list        Audit events");
        Console.Error.WriteLine("");
        Console.Error.WriteLine("Global options:");
        Console.Error.WriteLine("  --connection-string <cs>  --output <path>  --format jsonl|table");
        Console.Error.WriteLine("  --max-rows <n>  --since <iso>  --until <iso>");
        Console.Error.WriteLine("  --process <name>  --resource <name>  --status <code-or-name>");
        Console.Error.WriteLine("  --has-exception  --level <lvl>  --text <substr>  --verbose");
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
            Console.Error.WriteLine("Usage: bp-log-viewer session <list|get|log|errors> [options]");
            return 0;
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

    private static int DispatchResource(string[] subArgs, FilterSet filters)
    {
        if (subArgs.Length == 0 || subArgs[0] is "help" or "--help" or "-h")
        {
            Console.Error.WriteLine("Usage: bp-log-viewer resource list [options]");
            return 0;
        }

        return subArgs[0] switch
        {
            "list" => ResourceListCommand.Execute(subArgs.Skip(1).ToArray(), filters),
            _ => UnknownCommand($"resource {subArgs[0]}")
        };
    }

    private static int DispatchProcess(string[] subArgs, FilterSet filters)
    {
        if (subArgs.Length == 0 || subArgs[0] is "help" or "--help" or "-h")
        {
            Console.Error.WriteLine("Usage: bp-log-viewer process list [options]");
            return 0;
        }

        return subArgs[0] switch
        {
            "list" => ProcessListCommand.Execute(subArgs.Skip(1).ToArray(), filters),
            _ => UnknownCommand($"process {subArgs[0]}")
        };
    }
    private static int DispatchQueue(string[] subArgs, FilterSet filters)
    {
        if (subArgs.Length == 0 || subArgs[0] is "help" or "--help" or "-h")
        {
            Console.Error.WriteLine("Usage: bp-log-viewer queue <list|items> [options]");
            return 0;
        }

        return subArgs[0] switch
        {
            "list" => QueueListCommand.Execute(subArgs.Skip(1).ToArray(), filters),
            "items" => QueueItemsCommand.Execute(subArgs.Skip(1).ToArray(), filters),
            _ => UnknownCommand($"queue {subArgs[0]}")
        };
    }

    private static int DispatchAudit(string[] subArgs, FilterSet filters)
    {
        if (subArgs.Length == 0 || subArgs[0] is "help" or "--help" or "-h")
        {
            Console.Error.WriteLine("Usage: bp-log-viewer audit list [options]");
            return 0;
        }

        return subArgs[0] switch
        {
            "list" => AuditListCommand.Execute(subArgs.Skip(1).ToArray(), filters),
            _ => UnknownCommand($"audit {subArgs[0]}")
        };
    }
}
