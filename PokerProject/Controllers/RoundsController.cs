using Microsoft.AspNetCore.Mvc;
using PokerProject.Services.Rounds;

namespace PokerProject.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RoundsController : ControllerBase
    {
        private readonly IRoundService _roundService;

        public RoundsController(IRoundService roundService)
        {
            _roundService = roundService;
        }

        [HttpPost("{gameId}/rounds/start")]
        public async Task<IActionResult> StartRound(int gameId)
        {
            try
            {
                var round = await _roundService.StartNewRoundAsync(gameId);
                return Ok(round);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                // Generel fallback
                return StatusCode(500, new { message = "Something went wrong", detail = ex.Message });
            }
        }

        [HttpGet("{gameId}/current")]
        public async Task<IActionResult> GetCurrentRound(int gameId)
        {
            try
            {
                var round = await _roundService.GetCurrentRoundAsync(gameId);
                if (round == null)
                    return NotFound(new { message = "No active round found for this game" });

                return Ok(round);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Something went wrong", detail = ex.Message });
            }
        }

        [HttpPost("{roundId}/end")]
        public async Task<IActionResult> EndRound(int roundId)
        {
            try
            {
                var round = await _roundService.EndRoundAsync(roundId);
                return Ok(round);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Something went wrong", detail = ex.Message });
            }
        }
    }
}