using LeaderBoardUsingCaching.Data.Models;
using StackExchange.Redis;
using System.Threading.Channels;

namespace LeaderBoardUsingCaching.Service;

public class LeaderboardService
{
    private readonly IDatabase _redisDb;
    private readonly Channel<ScoreUpdate> _updateQueue;

    public LeaderboardService(IConnectionMultiplexer redis, Channel<ScoreUpdate> updateQueue)
    {
        _redisDb = redis.GetDatabase();
        _updateQueue = updateQueue;
    }

    public async Task UpdateScoreAsync(int playerId, double newScore)
    {
        await _redisDb.SortedSetAddAsync("leaderboard", playerId, newScore);

        await _updateQueue.Writer.WriteAsync(new ScoreUpdate(playerId, (decimal)newScore));
    }

    public async Task<List<LeaderboardEntry>> GetTopPlayersAsync(int topK = 10)
    {
        var entries = await _redisDb.SortedSetRangeByRankWithScoresAsync(
            "leaderboard",
            0,
            topK - 1,
            Order.Descending
        );

        var leaderboard = new List<LeaderboardEntry>();
        int rank = 1;
        foreach (var entry in entries)
        {
            leaderboard.Add(new LeaderboardEntry
            {
                Rank = rank++,
                PlayerId = int.Parse(entry.Element.ToString()),
                Score = entry.Score,
            });
        }

        return leaderboard;
    }
}
