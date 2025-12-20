using LeaderBoardUsingCaching.Data.Models;
using LeaderBoardUsingCaching.Service;
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
        public async Task<ActionResult<List<LeaderboardEntry>>> GetTopPlayers(int topK)
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

        [HttpGet("htmx/top/{topK}")]
        public async Task<ContentResult> GetTopPlayersHtml(int topK)
        {
            var topPlayers = await _leaderboardService.GetTopPlayersAsync(topK);

            var htmlBuilder = new System.Text.StringBuilder();

            foreach (var player in topPlayers)
            {
                string rankClass = player.Rank switch
                {
                    1 => "rank-1 text-xl font-bold",
                    2 => "rank-2 text-lg font-bold",
                    3 => "rank-3 text-lg font-bold",
                    _ => "text-slate-300"
                };

                string rowClass = "border-b border-slate-800 hover:bg-slate-800/50 transition-colors";
                if (player.Rank <= 3) rowClass += " bg-slate-900/30";

                htmlBuilder.Append($@"
                    <tr class='{rowClass}'>
                        <td class='p-4 font-mono {rankClass}'>#{player.Rank}</td>
                        <td class='p-4 text-slate-300 font-mono'>User-{player.PlayerId}</td>
                        <td class='p-4 text-right font-mono font-bold text-white'>{player.Score:N0}</td>
                    </tr>");
            }

            return Content(htmlBuilder.ToString(), "text/html");
        }

        [HttpPost("htmx/update")]
        public async Task<IActionResult> UpdatePlayerScoreHtml([FromForm] int playerId, [FromForm] double newScore)
        {
            await _leaderboardService.UpdateScoreAsync(playerId, newScore);
            return Ok(); // HTMX will just ignore the response if hx-swap is none, or we can return a toast
        }
    }
}
