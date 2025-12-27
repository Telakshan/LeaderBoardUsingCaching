
using LeaderBoardUsingCaching.Data.Repository;
using StackExchange.Redis;

namespace LeaderBoardUsingCaching.Service;

public class StreamWorker : BackgroundService
{
    private readonly IConnectionMultiplexer _blockingRedis; // Dedicated connection
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<StreamWorker> _logger;

    private readonly string _consumerName;

    public StreamWorker(IConfiguration config,
        IServiceProvider serviceProvider,
        ILogger<StreamWorker> logger)
    {
        // Create a dedicated connection for this worker to avoid blocking the global multiplexer
        // with XREADGROUP BLOCK commands.
        var connectionString = config.GetConnectionString("Redis");
        _blockingRedis = ConnectionMultiplexer.Connect(connectionString!);

        _serviceProvider = serviceProvider;

        string hostName = Environment.GetEnvironmentVariable("HOSTNAME")
            ?? Environment.MachineName
            ?? Guid.NewGuid().ToString();

        _consumerName = $"worker-{hostName}";
        _logger = logger;
    }

    public override void Dispose()
    {
        _blockingRedis.Dispose();
        base.Dispose();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var db = _blockingRedis.GetDatabase();

        try
        {
            await db.StreamCreateConsumerGroupAsync(Constants.StreamName, Constants.GroupName, StreamPosition.Beginning);
            _logger.LogInformation("Created Redis stream consumer group '{GroupName}' on stream '{StreamName}'",
                Constants.GroupName, Constants.StreamName);
        }
        catch (RedisServerException) { /* Group already exists */ }

        //Recover Pending Messages (Crash Recovery)
        await ProcessPendingMessagesAsync(db);

        //Process New Messages
        while (!stoppingToken.IsCancellationRequested)
        {
            /* Use XREADGROUP with BLOCK to reduce polling overhead.
               StackExchange.Redis StreamReadGroupAsync helpers don't expose BLOCK directly in all versions, 
               so we use ExecuteAsync to send the raw command. */

            try
            {
                var result = await db.ExecuteAsync("XREADGROUP", "GROUP", Constants.GroupName, _consumerName, "BLOCK", "2000", "COUNT", "10", "STREAMS", Constants.StreamName, ">");

                if (result.IsNull) continue; // Timed out or empty

                var entries = ParseResult(result);

                if (entries.Length == 0) continue;

                await ProcessEntriesAsync(db, entries);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "An unexpected error occurred in the stream worker. Retrying after a delay...");
                await Task.Delay(5000, stoppingToken);
            }

        }
    }

    private async Task ProcessPendingMessagesAsync(IDatabase db)
    {
        while (true)
        {
            var pendingEntries = await db.StreamReadGroupAsync(Constants.StreamName, Constants.GroupName, _consumerName, "0", count: 100);

            if (pendingEntries.Length == 0) break;

            _logger.LogInformation("Recovered {Count} pending messages.", pendingEntries.Length);
            await ProcessEntriesAsync(db, pendingEntries);
        }
    }

    private async Task ProcessEntriesAsync(IDatabase db, StreamEntry[] entries)
    {
        using (var scope = _serviceProvider.CreateScope())
        {
            var repository = scope.ServiceProvider.GetRequiredService<IPlayerRepository>();

            foreach (var entry in entries)
            {
                try {
                    var pid = entry.Values.First(v => v.Name == "pid").Value;
                    var score = entry.Values.First(v => v.Name == "score").Value;

                    await repository.UpdatePlayerScore(
                        int.Parse(pid!),
                        decimal.Parse(score!)
                        );

                    // Acknowledge the message (Tells Redis we've processed it)
                    await db.StreamAcknowledgeAsync(Constants.StreamName, Constants.GroupName, entry.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to process message {MessageId}. It will be retried on the next poll.", entry.Id);
                    continue;
                }
            }

            _logger.LogInformation("Processed {Count} entries from stream", entries.Length);
        }
    }

    private static StreamEntry[] ParseResult(RedisResult result)
    {
        var streams = (RedisResult[]?)result;

        if (streams == null || streams.Length == 0)
            return Array.Empty<StreamEntry>();

        var inner = (RedisResult[]?)streams[0];
        if (inner == null || inner.Length < 2) return Array.Empty<StreamEntry>();

        var entriesArr = (RedisResult[]?)inner[1];

        if (entriesArr == null || entriesArr.Length == 0)
            return Array.Empty<StreamEntry>();

        var entries = new StreamEntry[entriesArr.Length];

        for (int i = 0; i < entriesArr.Length; i++)
        {
            var entryData = (RedisResult[])entriesArr[i]!;
            var id = (RedisValue)entryData[0];
            var valuesArr = (RedisResult[])entryData[1]!;

            var nameValues = new NameValueEntry[valuesArr.Length / 2];
            for (int j = 0; j < valuesArr.Length; j += 2)
            {
                nameValues[j / 2] = new NameValueEntry(
                    (RedisValue)valuesArr[j],
                    (RedisValue)valuesArr[j + 1]
                );
            }

            entries[i] = new StreamEntry(id, nameValues);
        }

        return entries;
    }
}
