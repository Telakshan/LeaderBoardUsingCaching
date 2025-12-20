using LeaderBoardUsingCaching.Data.Context;
using LeaderBoardUsingCaching.Data.Repository;
using LeaderBoardUsingCaching.Service;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

var configuration = builder.Configuration;

// Add DbContext
builder.Services.AddDbContext<PlayerDbContext>(options =>
    options.UseSqlServer(configuration.GetConnectionString("PlayerDb")));

//Bad practice, should make Services Singleton and use IServiceProvider
builder.Services.AddScoped<IPlayerRepository, PlayerRepository>();
builder.Services.AddScoped<LeaderboardService>();
builder.Services.AddScoped<LeaderboardRehydrationService>();

builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var connectionString = configuration.GetConnectionString("Redis");

    return ConnectionMultiplexer.Connect(connectionString!);
});

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();


//Rehydrate leaderboard cache from database on startup
using var scope = app.Services.CreateScope();
var rehydrationService = scope.ServiceProvider.GetRequiredService<LeaderboardRehydrationService>();
await rehydrationService.RehydrateLeaderboardAsync("leaderboard");


//SeedDatabase();

async void SeedDatabase()
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
async void TruncateTable()
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

//Run after seeding
app.Run();

