using Bogus;
using LeaderBoardUsingCaching.Data.Models;

namespace LeaderBoardUsingCaching.Data.Context;

public class PlayerDbContextSeed
{
    public static async Task SeedAsync(PlayerDbContext context)
    {
        if (!context.Players.Any())
        {
            var faker = new Faker<Player>()
                .RuleFor(p => p.Name, f => f.Name.FullName())
                .RuleFor(p => p.Score, f => f.Random.Decimal(0, 1000));
            var players = faker.Generate(1000);
            await context.Players.AddRangeAsync(players);
            await context.SaveChangesAsync();
        }
    }
}
