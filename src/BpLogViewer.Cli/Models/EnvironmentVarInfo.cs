namespace BpLogViewer.Cli.Models;

/// <summary>
/// Eine Environment Variable aus <c>BPAEnvironmentVar</c>.
/// </summary>
public sealed record EnvironmentVarInfo(
    Guid Id,
    string Name,
    string? Datatype,
    string? Value,
    string? Description);
