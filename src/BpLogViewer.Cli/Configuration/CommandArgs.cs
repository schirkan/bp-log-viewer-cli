using System.Collections.Generic;

namespace BpLogViewer.Cli.Configuration;

/// <summary>
/// Result of parsing command-specific arguments.
/// </summary>
public sealed class CommandArgResult
{
    private readonly List<(string Option, string Value)> _entries = new();
    private readonly HashSet<string> _flags = new(StringComparer.Ordinal);
    private readonly List<string> _positional = new();

    /// <summary>Error message, or <c>null</c> if parsing succeeded.</summary>
    public string? Error { get; internal set; }

    /// <summary>Positional arguments in order of appearance.</summary>
    public IReadOnlyList<string> Positional => _positional;

    internal void AddValue(string option, string value) => _entries.Add((option, value));

    internal void AddFlag(string option) => _flags.Add(option);

    internal void AddPositional(string value) => _positional.Add(value);

    /// <summary>True if the option was given as a flag.</summary>
    public bool HasFlag(string option) => _flags.Contains(option);

    /// <summary>All values given for the option, in order of appearance.</summary>
    public IReadOnlyList<string> Values(string option)
    {
        var list = new List<string>();
        foreach (var entry in _entries)
        {
            if (string.Equals(entry.Option, option, StringComparison.Ordinal))
                list.Add(entry.Value);
        }

        return list;
    }

    /// <summary>The last value given for the option, or <c>null</c> if the option was not used.</summary>
    public string? Value(string option)
    {
        string? last = null;
        foreach (var entry in _entries)
        {
            if (string.Equals(entry.Option, option, StringComparison.Ordinal))
                last = entry.Value;
        }

        return last;
    }

    /// <summary>
    /// Value of the last of the given options that occurred in the arguments.
    /// Mirrors "last one wins" semantics when several aliases are offered.
    /// </summary>
    public string? LastValue(params string[] options)
    {
        for (var i = _entries.Count - 1; i >= 0; i--)
        {
            foreach (var option in options)
            {
                if (string.Equals(_entries[i].Option, option, StringComparison.Ordinal))
                    return _entries[i].Value;
            }
        }

        return null;
    }
}

/// <summary>
/// Parses command-specific arguments against the option vocabulary in
/// <see cref="OptionRegistry"/>. Global options are recognised as well, so their values
/// are consumed here too and cannot be mistaken for positional arguments.
/// </summary>
public static class CommandArgs
{
    /// <summary>
    /// Parses <paramref name="args"/> for the given command/action. Never throws;
    /// a missing option value is reported via <see cref="CommandArgResult.Error"/>.
    /// </summary>
    public static CommandArgResult Parse(string[] args, string command, string? action)
    {
        var result = new CommandArgResult();
        var commandOptions = OptionRegistry.ForCommand(command, action);

        for (var i = 0; i < args.Length; i++)
        {
            var token = args[i];

            if (commandOptions.TryGetValue(token, out var arity))
            {
                if (arity == OptionArity.Flag)
                {
                    result.AddFlag(token);
                    continue;
                }

                if (i + 1 >= args.Length)
                {
                    result.Error = token + " requires a value";
                    return result;
                }

                result.AddValue(token, args[++i]);
                continue;
            }

            if (OptionRegistry.GlobalArity(token) is { } globalArity)
            {
                if (globalArity == OptionArity.Value)
                {
                    if (i + 1 >= args.Length)
                    {
                        // The global parser reports the missing value. Stop here so that
                        // the remaining tokens are not misread as positional arguments.
                        break;
                    }

                    result.AddValue(token, args[++i]);
                }

                continue;
            }

            if (token.StartsWith("-", StringComparison.Ordinal))
            {
                continue;
            }

            result.AddPositional(token);
        }

        return result;
    }
}
