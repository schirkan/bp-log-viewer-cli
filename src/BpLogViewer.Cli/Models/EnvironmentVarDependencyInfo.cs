namespace BpLogViewer.Cli.Models;

/// <summary>
/// Eine Zuordnung eines Prozesses zu einer Environment Variable aus
/// <c>BPAProcessEnvironmentVarDependency</c>. Der Prozessname kommt per
/// <c>LEFT JOIN</c> auf <c>BPAProcess</c> und ist leer, wenn der Prozess nicht
/// (mehr) existiert.
/// </summary>
public sealed record EnvironmentVarDependencyInfo(
    int Id,
    Guid ProcessId,
    string? ProcessName,
    string VariableName);
