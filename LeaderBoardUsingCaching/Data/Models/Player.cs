namespace LeaderBoardUsingCaching.Data.Models;


public class Player
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public decimal Score { get; set; }
}

public record ScoreUpdate(int PlayerId, decimal NewScore);