using LeaderBoardUsingCaching.Data.Models;

namespace LeaderBoardUsingCaching.Data.Repository;

public interface IPlayerRepository
{
    Task<IEnumerable<decimal>> GetScores();
    Task UpdatePlayerScore(int playerId, decimal newScore);
    Task<IEnumerable<Player>> GetTopPlayers(int topN = 1000);
}
