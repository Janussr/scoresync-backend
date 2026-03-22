using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PokerProject.DTOs;
using PokerProject.Services.Players;

namespace PokerProject.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PlayersController : ControllerBase
    {
        private readonly IPlayerService _playerService;

        public PlayersController(IPlayerService playerService)
        {
            _playerService = playerService;
        }

        [Authorize(Roles = "Admin, Gamemaster")]
        [HttpPost("{gameId}")]
        public async Task<ActionResult<List<PlayerDto>>> AddPlayersToGameAsAdmin(int gameId, [FromBody] AddPlayersDto dto)
        {
            try
            {
                var addedPlayers = await _playerService.AddPlayersToGameAsAdminAsync(gameId, dto.UserIds);
                return Ok(addedPlayers);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex);
            }
        }

        [HttpGet("{gameId}/participants")]
        public async Task<ActionResult<List<PlayerDto>>> GetPlayers(int gameId)
        {
            try
            {
                var players = await _playerService.GetPlayersAsync(gameId);
                return Ok(players);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex);
            }
        }

        [HttpDelete("{gameId}/participants/{userId}")]
        [Authorize(Roles = "Admin, Gamemaster")]
        public async Task<IActionResult> RemovePlayer(int gameId, int userId)
        {
            try
            {
                var updatedPlayers = await _playerService.RemovePlayerAsync(gameId, userId);
                return Ok(updatedPlayers);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }

        }

    }
}
