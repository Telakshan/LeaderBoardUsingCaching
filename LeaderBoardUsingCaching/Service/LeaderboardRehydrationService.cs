using LeaderBoardUsingCaching.Data.Models;
using LeaderBoardUsingCaching.Data.Repository;
using StackExchange.Redis;

namespace LeaderBoardUsingCaching.Service;

public class LeaderboardRehydrationService
{
    private readonly IDatabase _redisDb;
    private readonly IPlayerRepository _playerRepository;

    public LeaderboardRehydrationService(IConnectionMultiplexer redis, IPlayerRepository playerRepository)
    {
        _redisDb = redis.GetDatabase();
        _playerRepository = playerRepository;
    }

    public async Task RehydrateLeaderboardAsync(string leaderboardKey)
    {
        long count = await _redisDb.SortedSetLengthAsync(leaderboardKey);

        if (count >= 100)
        {
            return;
        }

        IEnumerable<Player> topPlayers = await _playerRepository.GetTopPlayers();

        var batch = _redisDb.CreateBatch();

        var tasks = new List<Task>();

        foreach (var player in topPlayers)
        {
            tasks.Add(batch.SortedSetAddAsync(leaderboardKey, player.Id, (double)player.Score));
        }

        batch.Execute();
        await Task.WhenAll(tasks);
    }
}
