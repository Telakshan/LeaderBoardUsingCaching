
using LeaderBoardUsingCaching.Data.Repository;
using StackExchange.Redis;

namespace LeaderBoardUsingCaching.Service;

public class StreamWorker : BackgroundService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<StreamWorker> _logger;

    private const string StreamName = "score_stream";
    private const string GroupName = "score_writers";

    private readonly string _consumerName;

    public StreamWorker(IConnectionMultiplexer redis,
        IServiceProvider serviceProvider,
        ILogger<StreamWorker> logger)
    {
        _redis = redis;
        _serviceProvider = serviceProvider;

        string _hostName = Environment.GetEnvironmentVariable("HOSTNAME") 
            ?? Environment.MachineName 
            ?? Guid.NewGuid().ToString();

        _consumerName = $"worker-{_hostName}";
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var db = _redis.GetDatabase();

        try
        {
            await db.StreamCreateConsumerGroupAsync(StreamName, GroupName, StreamPosition.Beginning);
            _logger.LogInformation("Created Redis stream consumer group '{GroupName}' on stream '{StreamName}'",
                GroupName, StreamName);
        }
        catch (RedisServerException) { /* Group already exists */ }

        while (!stoppingToken.IsCancellationRequested)
        {
            var entries = await db.StreamReadGroupAsync(
                StreamName,
                GroupName,
                _consumerName,
                ">",
                count: 10
                );

            if (entries.Length == 0)
            {
                await Task.Delay(100, stoppingToken);
                continue;
            }

            using (var scope = _serviceProvider.CreateScope())
            {
                var repository = scope.ServiceProvider.GetRequiredService<IPlayerRepository>();

                foreach (var entry in entries)
                {
                    var pid = entry.Values.First(v => v.Name == "pid").Value;
                    var score = entry.Values.First(v => v.Name == "score").Value;

                    await repository.UpdatePlayerScore(
                        int.Parse(pid!),
                        decimal.Parse(score!)
                        );

                    // Acknowledge the message (Tells Redis we've processed it)
                    await db.StreamAcknowledgeAsync(StreamName, GroupName, entry.Id);

                }

                _logger.LogInformation("Processed {Count} entries from stream", entries.Length);
            }
        }
    }
}
