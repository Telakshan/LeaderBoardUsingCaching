using LeaderBoardUsingCaching.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace LeaderBoardUsingCaching.Data.Context;

public class PlayerDbContext: DbContext, IApplicationDbContext
{
/*    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlServer(
            "Data Source = localhost; Initial Catalog = LeaderboardExample; Integrated Security=True;Connect Timeout=30;Encrypt=False;Trust Server Certificate=False;Application Intent=ReadWrite;Multi Subnet Failover=False"
            ).LogTo(Console.WriteLine, new[] { DbLoggerCategory.Database.Command.Name });
    }*/


    public PlayerDbContext(DbContextOptions<PlayerDbContext> options) : base(options)
    {
    }
    public DbSet<Player> Players { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Player>();
        base.OnModelCreating(modelBuilder);
    }
}
