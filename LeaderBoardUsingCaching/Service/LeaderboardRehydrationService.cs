using LeaderBoardUsingCaching.Data.Models;
using LeaderBoardUsingCaching.Data.Repository;
using Microsoft.Identity.Client;
using StackExchange.Redis;

namespace LeaderBoardUsingCaching.Service;

public class LeaderboardRehydrationService
{
    private readonly IDatabase _redisDb;
    private readonly IConnectionMultiplexer _multiplexer;
    private readonly IPlayerRepository _playerRepository;

    public LeaderboardRehydrationService(IConnectionMultiplexer redis, IPlayerRepository playerRepository)
    {
        _multiplexer = redis;
        _redisDb = redis.GetDatabase();
        _playerRepository = playerRepository;
    }

    public async Task RehydrateLeaderboardAsync(string leaderboardKey)
    {
        long count = await _redisDb.SortedSetLengthAsync(leaderboardKey);

        if (count >=  100)
        {
            //Leaderboard already exists, so return
            return;
        }

        IEnumerable<Player> topPlayers = await _playerRepository.GetTopPlayers();

        var batch = _redisDb.CreateBatch();

        var tasks = new List<Task>();

        foreach (var player in topPlayers)
        {
            tasks.Add(batch.SortedSetAddAsync(leaderboardKey, player.Id, (double) player.Score));
        }

        /*        foreach (Player player in topPlayers)
                {
                    tasks.Add(batch.SortedSetAddAsync(leaderboardKey, player.Id, (double) player.Score));

                    batchSize++;
                    totalLoaded++;

                    if (batchSize >= 5000)
                    {
                        batch.Execute();
                        await Task.WhenAll(tasks);

                        Console.WriteLine($"Loaded {totalLoaded} scores...");

                        batch = _redisDb.CreateBatch();
                        tasks.Clear();
                        batchSize = 0;

                    }
                }*/

        batch.Execute();
        await Task.WhenAll(tasks);

    }
}
