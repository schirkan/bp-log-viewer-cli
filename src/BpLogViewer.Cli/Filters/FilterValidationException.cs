namespace BpLogViewer.Cli.Filters;

/// <summary>
/// Thrown by <see cref="FilterParser"/> for invalid global filter / option values.
/// Carries the error code (for jsonl error envelope) and exit code (architecture § 8) directly.
/// </summary>
public sealed class FilterValidationException : Exception
{
    public string Code { get; }
    public int ExitCode { get; }

    public FilterValidationException(string code, string message, int exitCode)
        : base(message)
    {
        Code = code;
        ExitCode = exitCode;
    }
}
