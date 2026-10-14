using BpLogViewer.Cli.Configuration;
using BpLogViewer.Cli.Data;
using BpLogViewer.Cli.Output;
using FilterSet = BpLogViewer.Cli.Filters.Filters;

namespace BpLogViewer.Cli.Commands;

public static class QueueItemsCommand
{
    public static int Execute(string[] args, FilterSet filters)
    {
        if (!ParseArgs(args, out var queueName, out var queueId, out var statuses, out var includeTags, out var fullData, out var parseError))
        {
            ErrorWriter.WriteErrorAsync("command_invalid", parseError!, 11).GetAwaiter().GetResult();
            return 11;
        }

        var settings = ConfigurationLoader.Load(cliConnectionString: filters.ConnectionString);
        if (string.IsNullOrEmpty(settings.Database.ConnectionString))
        {
            ErrorWriter.WriteErrorAsync("config_invalid", "appsettings.json nicht gefunden oder Database.ConnectionString leer. Siehe self-test für Diagnose.", 3).GetAwaiter().GetResult();
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

            var effectiveMax = (filters.Output is { Length: > 0 } && !filters.MaxRows.HasValue)
                ? FilterSet.MaxRowsAbsoluteCeiling
                : filters.EffectiveMaxRows;

            var rows = repo.ListItems(effectiveQueueId.Value, statuses, filters.Since, filters.Until, filters.Text, fullData, includeTags, effectiveMax + 1);
            var truncated = rows.Count > effectiveMax;
            if (truncated)
            {
                rows.RemoveAt(rows.Count - 1);
                ErrorWriter.WriteWarningAsync("output_truncated", $"{effectiveMax} of {effectiveMax}+ rows. Use --output to write full result.").GetAwaiter().GetResult();
            }

            if (!string.IsNullOrWhiteSpace(filters.Output))
            {
                if (!OutputFileWriter.TryWriteJsonLines(rows, PayloadJsonContext.Default.QueueItemInfo, filters.Output, out var code, out var msg))
                {
                    ErrorWriter.WriteErrorAsync(code ?? "output_write_failed", msg ?? "failed to write output file", 6).GetAwaiter().GetResult();
                    return 6;
                }
                return truncated ? 2 : 0;
            }

            foreach (var row in rows)
            {
                Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(row, PayloadJsonContext.Default.QueueItemInfo));
            }

            return truncated ? 2 : 0;
        }
        catch (Exception ex)
        {
            ErrorWriter.WriteErrorAsync("db_error", ex.Message, 5).GetAwaiter().GetResult();
            return 5;
        }
    }

    private static bool ParseArgs(string[] args, out string? queueName, out Guid? queueId, out string[]? statuses, out bool includeTags, out bool fullData, out string? error)
    {
        queueName = null;
        queueId = null;
        statuses = null;
        includeTags = false;
        fullData = false;
        error = null;

        var statusList = new List<string>();
        var optionsWithValue = new HashSet<string>(StringComparer.Ordinal)
        {
            "--since", "--until", "--process", "--resource", "--status", "--level", "--text",
            "--max-rows", "--format", "--output", "--out-file", "--connection-string", "--queue-id"
        };

        for (var i = 0; i < args.Length; i++)
        {
            var token = args[i];
            if (token == "--include-tags") { includeTags = true; continue; }
            if (token == "--full-data") { fullData = true; continue; }

            if (token == "--queue-id")
            {
                if (i + 1 >= args.Length || !Guid.TryParse(args[++i], out var parsedId))
                {
                    error = "invalid --queue-id";
                    return false;
                }
                queueId = parsedId;
                continue;
            }

            if (token == "--status" && i + 1 < args.Length)
            {
                statusList.Add(args[++i]);
                continue;
            }

            if (optionsWithValue.Contains(token))
            {
                i++;
                continue;
            }

            if (token.StartsWith("-", StringComparison.Ordinal)) continue;
            queueName ??= token;
        }

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

        statuses = statusList.Count > 0 ? statusList.ToArray() : null;
        return true;
    }
}
