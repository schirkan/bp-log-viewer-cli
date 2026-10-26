using BpLogViewer.Cli.Configuration;
using BpLogViewer.Cli.Data;
using BpLogViewer.Cli.Filters;
using BpLogViewer.Cli.Output;
using FilterSet = BpLogViewer.Cli.Filters.Filters;

namespace BpLogViewer.Cli.Commands;

/// <summary>
/// <c>bp-log-viewer queue items</c>.
/// </summary>
public static class QueueItemsCommand
{
    public static int Execute(string[] args, FilterSet filters)
    {
        if (!ParseArgs(args, out var queueName, out var queueId, out var statuses, out var includeTags, out var fullData, out var parseError))
        {
            ErrorWriter.WriteErrorAsync("command_invalid", parseError!, 11).GetAwaiter().GetResult();
            return 11;
        }

        if (!CommandHost.TryLoadSettings(filters, out var settings))
        {
            return 3;
        }

        if (filters.Text is { Length: > 0 })
        {
            ErrorWriter.WriteWarningAsync("text_filter_perf_warning", "full table scan, consider narrowing filters").GetAwaiter().GetResult();
        }

        try
        {
            using var db = new BpDbContext(settings);
            var repo = new QueueRepository(db);

            var effectiveQueueId = queueId;
            if (!effectiveQueueId.HasValue)
            {
                effectiveQueueId = repo.ResolveQueueIdByName(queueName!);
                if (!effectiveQueueId.HasValue)
                {
                    ErrorWriter.WriteWarningAsync("queue_not_found", queueName!).GetAwaiter().GetResult();
                    return 10;
                }
            }

            var effectiveMax = CommandHost.ResolveEffectiveMax(filters);
            var rows = repo.ListItems(effectiveQueueId.Value, statuses, filters.Since, filters.Until, filters.Text, fullData, includeTags, effectiveMax + 1);
            var truncated = CommandHost.Truncate(rows, effectiveMax);
            return CommandHost.Emit(rows, PayloadJsonContext.Readable.QueueItemInfo, filters, truncated);
        }
        catch (Exception ex)
        {
            return CommandHost.WriteDbError(ex);
        }
    }

    private static bool ParseArgs(
        string[] args,
        out string? queueName,
        out Guid? queueId,
        out string[]? statuses,
        out bool includeTags,
        out bool fullData,
        out string? error)
    {
        queueName = null;
        queueId = null;
        statuses = null;
        includeTags = false;
        fullData = false;
        error = null;

        var parsed = CommandArgs.Parse(args, "queue", "items");
        if (parsed.Error is not null)
        {
            error = parsed.Error;
            return false;
        }

        includeTags = parsed.HasFlag("--include-tags");
        fullData = parsed.HasFlag("--full-data");

        var queueIdValue = parsed.Value("--queue-id");
        if (queueIdValue is not null)
        {
            if (!Guid.TryParse(queueIdValue, out var parsedId))
            {
                error = "invalid --queue-id";
                return false;
            }

            queueId = parsedId;
        }

        // --status ist zugleich globale Option und Filter dieses Commands.
        var statusList = parsed.Values("--status");
        statuses = statusList.Count > 0 ? statusList.ToArray() : null;

        queueName = parsed.Positional.Count > 0 ? parsed.Positional[0] : null;

        if (!string.IsNullOrWhiteSpace(queueName) && queueId.HasValue)
        {
            error = "use either <queue-name> or --queue-id, not both";
            return false;
        }

        if (string.IsNullOrWhiteSpace(queueName) && !queueId.HasValue)
        {
            error = "usage: queue items <queue-name> | --queue-id <uuid>";
            return false;
        }

        return true;
    }
}
