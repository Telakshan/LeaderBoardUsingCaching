using LeaderBoardUsingCaching.Data.Models;
using Microsoft.Extensions.Caching.Memory;
using StackExchange.Redis;
using System.Collections.Concurrent;

namespace LeaderBoardUsingCaching.Service;

public class LeaderboardService
{
    private readonly IDatabase _redisDb;
    private readonly IMemoryCache _localCache;
    private readonly ILogger<LeaderboardService> _logger;

    private static readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();

    private const string StreamName = "score_stream";

    public LeaderboardService(IConnectionMultiplexer redis,
        IMemoryCache localCache,
        ILogger<LeaderboardService> logger)
    {
        _redisDb = redis.GetDatabase();
        _localCache = localCache;
        _logger = logger;
    }

    public async Task UpdateScoreAsync(int playerId, double newScore)
    {

        var db = _redisDb;

        var transaction = db.CreateTransaction();

        _ = transaction.SortedSetAddAsync("leaderboard", playerId, newScore);

        _ = transaction.StreamAddAsync(StreamName,
        [
            new NameValueEntry("pid", playerId),
            new NameValueEntry("score", newScore)
        ], maxLength: 1000);

        await transaction.ExecuteAsync();

        // Invalidate the local cache for the most common view to ensure immediate consistency
        _localCache.Remove("leaderboard_top_10");
    }

    public async Task<List<LeaderboardEntry>> GetTopPlayersAsync(int topK = 10)
    {
        string cacheKey = $"leaderboard_top_{topK}";

        // Try to get from local cache, if it doesn't exist, fetch from Redis
        if (_localCache.TryGetValue(cacheKey, out List<LeaderboardEntry>? cachedLeaderboard))
        {
            _logger.LogInformation("Local cache hit!");
            return cachedLeaderboard!;
        }

        // Use a semaphore to prevent cache stampede, ensuring only one fetch from Redis
        // "Request Coalescing"
        var myLock = _locks.GetOrAdd(cacheKey, _ => new SemaphoreSlim(1, 1));

        await myLock.WaitAsync();

        try
        {
            // Double-check the cache after acquiring the lock
            // While we were waiting for the lock, another thread might have finished
            // fetching the data. We check the cache one more time.
            if (_localCache.TryGetValue(cacheKey, out cachedLeaderboard))
            {
                _logger.LogInformation("Double checking local cache hit!");
                return cachedLeaderboard!;
            }

            var entries = await _redisDb.SortedSetRangeByRankWithScoresAsync(
                "leaderboard",
                0,
                topK - 1,
                Order.Descending
            );

            var leaderboard = entries.Select((entry, index) => new LeaderboardEntry
            {
                Rank = index + 1,
                PlayerId = (int)entry.Element,
                Score = entry.Score
            }).ToList();

            /*Lower the cache time to 2 seconds to reduce staleness*/
            _localCache.Set(cacheKey, leaderboard, TimeSpan.FromSeconds(2));

            _logger.LogInformation("Redis cache hit!");
            return leaderboard;
        }
        finally
        {
            myLock.Release();
        }
    }
}
