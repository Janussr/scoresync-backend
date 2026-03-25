using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PokerProject.DTOs;
using PokerProject.Services.Bounties;
using PokerProject.Services.Games;

namespace PokerProject.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BountiesController : ControllerBase
    {
        private readonly IBountyService _bountyService;
        public BountiesController(IBountyService bountyService) {
            _bountyService = bountyService;
        }

        // Player knockout
        [Authorize(Roles = "User, Gamemaster, Admin")]
        [HttpPost("player/knockout")]
        public async Task<IActionResult> KnockoutPlayer([FromBody] KnockoutDto request)
        {
            if (!request.VictimPlayerId.HasValue)
                return BadRequest("VictimPlayerId is required");

            try
            {
                var result = await _bountyService.PlayerKnockoutAsync(
                    request.GameId,
                    User.GetUserId(),        // sender kun UserId
                    request.VictimPlayerId
                );

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // Admin knockout
        [Authorize(Roles = "Admin, Gamemaster")]
        [HttpPost("admin/knockout")]
        public async Task<IActionResult> KnockoutAdmin([FromBody] KnockoutAdminDto request)
        {
            if (!request.KillerPlayerId.HasValue || !request.VictimPlayerId.HasValue)
                return BadRequest("Both killerPlayerId and victimPlayerId are required");

            try
            {
                var result = await _bountyService.HandleKnockoutAsync(
                    request.GameId,
                    request.KillerPlayerId.Value,
                    request.VictimPlayerId.Value
                );

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("bounty-leaderboard")]
        public async Task<IActionResult> GetBountyLeaderboard()
        {
            var leaderboard = await _bountyService.GetBountyLeaderboardAsync();
            return Ok(leaderboard);
        }
    }
}
