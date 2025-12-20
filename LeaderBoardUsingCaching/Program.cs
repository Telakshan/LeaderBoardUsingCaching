using LeaderBoardUsingCaching.Data.Context;
using LeaderBoardUsingCaching.Data.Models;
using LeaderBoardUsingCaching.Data.Repository;
using LeaderBoardUsingCaching.Service;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using System.Threading.Channels;

var builder = WebApplication.CreateBuilder(args);

var configuration = builder.Configuration;

builder.Services.AddDbContext<PlayerDbContext>(options =>
    options.UseSqlServer(configuration.GetConnectionString("PlayerDb"))
    .LogTo(Console.WriteLine, new[] { DbLoggerCategory.Database.Command.Name }));

builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var connectionString = configuration.GetConnectionString("Redis");
    return ConnectionMultiplexer.Connect(connectionString!);
});

builder.Services.AddSingleton(Channel.CreateUnbounded<ScoreUpdate>());

builder.Services.AddScoped<IPlayerRepository, PlayerRepository>();
builder.Services.AddSingleton<LeaderboardService>();
builder.Services.AddScoped<LeaderboardRehydrationService>();
builder.Services.AddHostedService<ScorePersistenceService>();

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        builder =>
        {
            builder.AllowAnyOrigin()
                   .AllowAnyMethod()
                   .AllowAnyHeader();
        });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.MapOpenApi();
}

//Step 1: Migrate
using (var migrationScope = app.Services.CreateScope())
{
    var dbContext = migrationScope.ServiceProvider.GetRequiredService<PlayerDbContext>();
    dbContext.Database.Migrate();
}

//Step 2: Seed database
await SeedDatabase();

//Step 3: Rehydrate leaderboard
using var scope = app.Services.CreateScope();
var rehydrationService = scope.ServiceProvider.GetRequiredService<LeaderboardRehydrationService>();
await rehydrationService.RehydrateLeaderboardAsync("leaderboard");

app.UseCors("AllowAll");

app.UseAuthorization();

app.MapControllers();

async Task SeedDatabase()
{
    using var scope = app.Services.CreateScope();

    try
    {
        var scopedContext = scope.ServiceProvider.GetRequiredService<PlayerDbContext>();
        await PlayerDbContextSeed.SeedAsync(scopedContext);
    }
    catch
    {
        throw;
    }
}
async Task TruncateTable()
{
    using var scope = app.Services.CreateScope();

    try
    {
        var scopedContext = scope.ServiceProvider.GetRequiredService<PlayerDbContext>();

        var length = await scopedContext.Players.CountAsync();

        var itemsToDelete = scopedContext.Players.Take(length);

        scopedContext.Players.RemoveRange(itemsToDelete);

        scopedContext.SaveChanges();

    }
    catch (Exception)
    {
        throw;
    }
}


app.Run();
