namespace BpLogViewer.Cli.Models;

public sealed record ResourceInfo(
    Guid ResourceId,
    string Name,
    string? DisplayStatus,
    int StatusId,
    int? ProcessesRunning,
    int? ActionsRunning,
    DateTimeOffset? LastUpdated,
    DateTimeOffset? LastLoggedIn,
    int? CurrentSessionNumber,
    DateTimeOffset? CurrentSessionStartedAt,
    string? Fqdn);
