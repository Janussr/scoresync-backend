using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PokerProject.DTOs;
using PokerProject.Services.Games;

namespace PokerProject.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GamesController : ControllerBase
    {
        private readonly IGameService _gameService;

        public GamesController(IGameService gameService)
        {
            _gameService = gameService;
        }

        [Authorize(Roles = "Admin, Gamemaster")]
        [HttpPost("start")]
        public async Task<ActionResult<GameDto>> StartGame()
        {
            try
            {
                var game = await _gameService.StartGameAsync();
                return Ok(game);
            }
            catch (DbUpdateException ex)
            {
                return StatusCode(500, "Error trying to create game in db");
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Unexpected error");
            }
        }

        [Authorize(Roles = "Admin, Gamemaster")]
        [HttpPost("{gameId}/end")]
        public async Task<ActionResult<GameDto>> EndGame(int gameId)
        {
            try
            {
                var ended = await _gameService.EndGameAsync(gameId);
                return Ok(ended);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }

        }

        [Authorize(Roles = "Admin, Gamemaster")]
        [HttpPost("{gameId}/cancel")]
        public async Task<ActionResult<GameDto>> CancelGame(int gameId)
        {
            try
            {
                var game = await _gameService.CancelGameAsync(gameId);
                return Ok(game);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
        }

        [HttpGet]
        public async Task<ActionResult<List<GameDto>>> GetAllGames()
        {
            try
            {
                var games = await _gameService.GetAllGamesAsync();
                return Ok(games);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Unexpected error while fetching games");
            }
        }

        [HttpGet("active/game")]
        public async Task<ActionResult<List<GameDto>>> GetActiveGame()
        {
            try
            {
                var games = await _gameService.GetActiveGameAsync();
                return Ok(games);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Unexpected error while fetching active game");
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<GameDetailsDto>> GetGameDetails(int id)
        {
            try
            {
                var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;

                var game = await _gameService.GetGameDetailsAsync(id, role);

                if (game == null)
                    return NotFound();

                return Ok(game);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("remove/{gameId}")]
        public async Task<IActionResult> RemoveGame(int gameId)
        {
            try
            {
                await _gameService.RemoveGameAsync(gameId);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }


        [Authorize(Roles = "Admin, Gamemaster")]
        [HttpPatch("{gameId}/rules")]
        public async Task<IActionResult> UpdateRules(int gameId, [FromBody] UpdateRulesDto dto)
        {
            try
            {
                await _gameService.UpdateRulesAsync(gameId, dto);
                return Ok();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message); 
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Unexpected error");
            }
        }
    }
}
