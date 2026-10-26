using System.Collections.Generic;

namespace BpLogViewer.Cli.Configuration;

/// <summary>
/// Arity of a CLI option: does it consume the next argument as its value?
/// </summary>
public enum OptionArity
{
    /// <summary>Option without a value (flag).</summary>
    Flag,

    /// <summary>Option that consumes the following argument as its value.</summary>
    Value,
}

/// <summary>
/// Single source of truth for the CLI option vocabulary.
/// <para>
/// Global options apply to every command. Command options belong to exactly one
/// command/action pair. The dispatcher, the global filter parser and the individual
/// commands all read their option metadata from here, so an option can no longer be
/// declared in one place and forgotten in another.
/// </para>
/// </summary>
public static class OptionRegistry
{
    private static readonly Dictionary<string, OptionArity> GlobalOptions = new(StringComparer.Ordinal)
    {
        ["--since"] = OptionArity.Value,
        ["--until"] = OptionArity.Value,
        ["--process"] = OptionArity.Value,
        ["--resource"] = OptionArity.Value,
        ["--status"] = OptionArity.Value,
        ["--level"] = OptionArity.Value,
        ["--text"] = OptionArity.Value,
        ["--max-rows"] = OptionArity.Value,
        ["--format"] = OptionArity.Value,
        ["--output"] = OptionArity.Value,
        ["--out-file"] = OptionArity.Value,
        ["--connection-string"] = OptionArity.Value,
        ["--has-exception"] = OptionArity.Flag,
        ["--verbose"] = OptionArity.Flag,
    };

    private static readonly Dictionary<string, Dictionary<string, OptionArity>> CommandOptions = new(StringComparer.Ordinal)
    {
        ["process list"] = new(StringComparer.Ordinal)
        {
            ["--name"] = OptionArity.Value,
            ["--name-contains"] = OptionArity.Value,
            ["--type"] = OptionArity.Value,
            ["--has-errors-since"] = OptionArity.Value,
        },
        ["resource list"] = new(StringComparer.Ordinal)
        {
            ["--name"] = OptionArity.Value,
            ["--name-contains"] = OptionArity.Value,
        },
        ["queue list"] = new(StringComparer.Ordinal)
        {
            ["--name"] = OptionArity.Value,
            ["--name-contains"] = OptionArity.Value,
            ["--only-exceptions"] = OptionArity.Flag,
            ["--only-running"] = OptionArity.Flag,
        },
        ["queue items"] = new(StringComparer.Ordinal)
        {
            ["--queue-id"] = OptionArity.Value,
            ["--include-tags"] = OptionArity.Flag,
            ["--full-data"] = OptionArity.Flag,
        },
        ["session get"] = new(StringComparer.Ordinal)
        {
            ["--session-id"] = OptionArity.Value,
            ["--include-state-xml"] = OptionArity.Flag,
        },
        ["session log"] = new(StringComparer.Ordinal)
        {
            ["--stage"] = OptionArity.Value,
            ["--full-result"] = OptionArity.Flag,
        },
        ["session errors"] = new(StringComparer.Ordinal)
        {
            ["--exception-type"] = OptionArity.Value,
            ["--aggregate"] = OptionArity.Flag,
        },
        ["audit list"] = new(StringComparer.Ordinal)
        {
            ["--code"] = OptionArity.Value,
            ["--user"] = OptionArity.Value,
            ["--target-process"] = OptionArity.Value,
            ["--target-resource"] = OptionArity.Value,
            ["--include-xml"] = OptionArity.Flag,
            ["--full-comments"] = OptionArity.Flag,
        },
    };

    private static readonly IReadOnlyDictionary<string, OptionArity> EmptyOptions =
        new Dictionary<string, OptionArity>(StringComparer.Ordinal);

    /// <summary>Global options that consume a value.</summary>
    public static IReadOnlySet<string> GlobalOptionsWithValue { get; } =
        new HashSet<string>(
            GlobalOptions.Where(p => p.Value == OptionArity.Value).Select(p => p.Key),
            StringComparer.Ordinal);

    /// <summary>All known global options, mapped to their arity.</summary>
    public static IReadOnlyDictionary<string, OptionArity> Global => GlobalOptions;

    /// <summary>Arity of a global option, or <c>null</c> if the option is unknown.</summary>
    public static OptionArity? GlobalArity(string option)
        => GlobalOptions.TryGetValue(option, out var arity) ? arity : null;

    /// <summary>
    /// Command options of the given command/action pair. Unknown commands yield an empty map.
    /// </summary>
    public static IReadOnlyDictionary<string, OptionArity> ForCommand(string command, string? action)
    {
        var key = string.IsNullOrEmpty(action) ? command : command + " " + action;
        return CommandOptions.TryGetValue(key, out var options) ? options : EmptyOptions;
    }

    /// <summary>Arity of a command option, or <c>null</c> if unknown for that command.</summary>
    public static OptionArity? CommandArity(string command, string? action, string option)
        => ForCommand(command, action).TryGetValue(option, out var arity) ? arity : null;
}
