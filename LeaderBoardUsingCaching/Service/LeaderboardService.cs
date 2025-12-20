using LeaderBoardUsingCaching.Data.Models;
using LeaderBoardUsingCaching.Data.Repository;
using StackExchange.Redis;
using System.Threading.Channels;

namespace LeaderBoardUsingCaching.Service;

public class LeaderboardService
{
    private readonly IDatabase _redisDb;
    private readonly Channel<ScoreUpdate> _updateQueue;
    private readonly IPlayerRepository _playerRepository;

    public LeaderboardService(IConnectionMultiplexer redis, IPlayerRepository playerRepository)
    {
         _redisDb = redis.GetDatabase();
        _updateQueue = Channel.CreateUnbounded<ScoreUpdate>();
        _ = Task.Run(ProcessBackgroundUpdates);
        _playerRepository = playerRepository;
    }

    public async Task UpdateScoreAsync(int playerId, double newScore)
    {
        await _redisDb.SortedSetAddAsync("leaderboard", playerId, newScore);

        await _updateQueue.Writer.WriteAsync(new ScoreUpdate(playerId, (decimal)newScore));

        //Return immediately after queuing the update, writing to the database is handled asynchronously
    }

    private async Task ProcessBackgroundUpdates()
    {
        await foreach (var update in _updateQueue.Reader.ReadAllAsync())
        {
            try
            {

                Console.WriteLine($"[Write-Behind] Persisting User {update.PlayerId} with score {update.NewScore} to SQL DB...");
                await _playerRepository.UpdatePlayerScore(update.PlayerId, update.NewScore);
            }
            catch (Exception ex)
            {
                // Handle failures (retries, dead-letter queue, etc.)
                Console.WriteLine($"Error syncing to DB: {ex.Message}");
            }
        }
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
