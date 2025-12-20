using LeaderBoardUsingCaching.Service;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace LeaderBoardUsingCaching.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LeaderboardController : ControllerBase
    {
        private readonly LeaderboardService _leaderboardService;
        public LeaderboardController(LeaderboardService leaderboardService)
        {
            _leaderboardService = leaderboardService;
        }

        [HttpGet("top/{topK}")]
        public async Task<IActionResult> GetTopPlayers(int topK)
        {
            var topPlayers = await _leaderboardService.GetTopPlayersAsync(topK);
            return Ok(topPlayers);
        }

        [HttpPost("update")]
        public async Task<IActionResult> UpdatePlayerScore([FromQuery] int playerId, [FromQuery] double newScore)
        {
            await _leaderboardService.UpdateScoreAsync(playerId, newScore);
            return Ok();
        }
    }
}
