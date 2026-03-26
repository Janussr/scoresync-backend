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
    }
}