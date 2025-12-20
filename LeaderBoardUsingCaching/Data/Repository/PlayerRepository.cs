
using LeaderBoardUsingCaching.Data.Context;
using LeaderBoardUsingCaching.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace LeaderBoardUsingCaching.Data.Repository;

public class PlayerRepository : IPlayerRepository
{
    private readonly PlayerDbContext _playerDbContext;
    public PlayerRepository(PlayerDbContext playerDbContext)
    {
        _playerDbContext = playerDbContext;
    }
    public async Task<IEnumerable<decimal>> GetScores()
    {
        var scores = await _playerDbContext.Players
            .Select(p => p.Score)
            .ToListAsync();

        return scores;
    }

    public async Task UpdatePlayerScore(int playerId, decimal newScore)
    {
        var player = await _playerDbContext.Players.FindAsync(playerId);
        if (player != null)
        {
            player.Score = newScore;
            await _playerDbContext.SaveChangesAsync();
        }
    }

    public async Task<IEnumerable<Player>> GetTopPlayers(int topN = 1000)
    {
        return await _playerDbContext.Players
            .OrderByDescending(p => p.Score)
            .Take(topN)
            .ToListAsync();
    }
}
