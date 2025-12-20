# Redis Write-Behind Leaderboard

This project demonstrates a high-performance leaderboard system using Redis Sorted Sets for low-latency reads and a Write-Behind pattern for asynchronous SQL database persistence. This is very much like your leetcode leaderboard, While a traditional database can easily store scores, calculating a "Global Rank" among millions of users in real-time is computationally expensive for SQL. This project solves that bottleneck by leveraging a Polyglot Persistence architecture, using Redis Sorted Sets as the primary engine for real-time ranking and a persistent database (like SQL Server or PostgreSQL) for long-term durability.

## Architecture

1.  **API Layer**: ASP.NET Core Web API.
2.  **Caching Layer**: Redis Sorted Sets ($O(\log N)$ rank/score operations).
3.  **Persistence Layer**: SQL Server.
4.  **Write-Behind**:
    -   Updates are written immediately to Redis.
    -   Updates are queued in an in-memory `Channel<ScoreUpdate>`.
    -   `ScorePersistenceService` (BackgroundService) drains the queue and updates SQL Server.

## Prerequisites

-   Docker Desktop

## Docker Setup

To run the entire stack (API, Redis, SQL Server):

```bash
docker-compose up --build
```

The services will be available at:
-   **API (Swagger)**: http://localhost:5082/swagger/index.html
-   **SQL Server**: localhost:1433
-   **Redis**: localhost:6379

> **Note**: The SQL Server `SA` password is set to `Password123!` in `docker-compose.yml`.

## API Endpoints

### 1. Update Score
**POST** `/api/Leaderboard/update`
-   Updates the player's score in Redis immediately.
-   Queues the update for background persistence.

### 2. Get Top Players
**GET** `/api/Leaderboard/top/{topK}`
-   Retrieves the top `K` players directly from Redis.
-   Returns `List<LeaderboardEntry>`.

## Design Decisions

### Write-Behind Pattern
Instead of writing to SQL synchronously (which is slow), we write to Redis and acknowledge the request. A background service persists the data eventually. This improves API latency significantly but introduces a small risk of data loss if the server crashes before draining the queue.

### Redis Sorted Sets
Redis `ZSET` is used because it maintains order automatically.
-   `ZADD`: Updates score in $O(\log N)$.
-   `ZREVRANGE`: Fetches top players in $O(\log N + M)$.

### Docker Networking
The `docker-compose.yml` creates a bridge network `leaderboard-network` allowing containers to resolve each other by name (`app`, `db`, `redis`).
