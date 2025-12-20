using LeaderBoardUsingCaching.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace LeaderBoardUsingCaching.Data.Context;

public interface IApplicationDbContext
{
    DbSet<Player> Players { get; set; }
}
