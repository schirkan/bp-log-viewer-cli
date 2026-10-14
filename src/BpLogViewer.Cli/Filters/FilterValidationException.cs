namespace BpLogViewer.Cli.Filters;

/// <summary>
/// Wirft der <see cref="FilterParser"/>, wenn ein globaler Filter / eine Option
/// ungültig ist. Trägt den Fehler-Code (für jsonl-Error-Envelope) und den
/// Exit-Code (architecture § 8) bereits in sich.
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
