using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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

        [Authorize]
        [HttpPost("{gameId}/bounty")]
        public async Task<IActionResult> RegisterKnockout(int gameId, [FromBody] KnockoutDto dto)
        {
            try
            {
                var userId = User.GetUserId();
                var isAdmin = User.GetUserRole() == "Admin";

                await _bountyService.RegisterKnockoutAsync(
                    gameId,
                    userId,
                    dto.VictimUserId,
                    isAdmin
                );

                return Ok();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex);
            }
        }

        [Authorize(Roles = "Admin, , Gamemaster")]
        [HttpPost("{gameId}/admin/bounty")]
        public async Task<IActionResult> AdminRegisterKnockout(int gameId, [FromBody] KnockoutDto dto)
        {
            try
            {
                var isAdmin = User.GetUserRole() == "Admin";

                await _bountyService.RegisterKnockoutAsync(
                    gameId,
                    dto.KillerUserId,
                    dto.VictimUserId,
                    isAdmin
                );

                return Ok();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex);
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
