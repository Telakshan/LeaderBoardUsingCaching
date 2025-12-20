using LeaderBoardUsingCaching.Data.Models;
using LeaderBoardUsingCaching.Data.Repository;
using System.Threading.Channels;

namespace LeaderBoardUsingCaching.Service;

public class ScorePersistenceService : BackgroundService
{
    private readonly Channel<ScoreUpdate> _channel;
    private readonly IServiceProvider _serviceProvider;

    public ScorePersistenceService(Channel<ScoreUpdate> channel, IServiceProvider serviceProvider)
    {
        _channel = channel;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var update in _channel.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var repository = scope.ServiceProvider.GetRequiredService<IPlayerRepository>();
                await repository.UpdatePlayerScore(update.PlayerId, update.NewScore);
            }
            catch
            {
            }
        }
    }
}
