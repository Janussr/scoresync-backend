using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PokerProject.DTOs.Games;
using PokerProject.Services.Games;
using System.Security.Claims;

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
        public async Task<ActionResult<GameDto>> StartGame([FromBody] StartGameRequestDto request)
        {
            try
            {
                var game = await _gameService.StartGameAsync(User, request);
                return Ok(game);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [Authorize(Roles = "Admin, Gamemaster")]
        [HttpPost("{gameId}/open")]
        public async Task<ActionResult> OpenGame(int gameId)
        {
            try
            {
                await _gameService.OpenGameForPlayers(gameId);
                return Ok();
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
                return StatusCode(500, new { message = ex.Message });
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
        public async Task<ActionResult<List<GameListItemDto>>> GetAllGames()
        {
            try
            {
                var games = await _gameService.GetAllGamesAsync();
                return Ok(games);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex);
            }
        }

        [HttpGet("history-page")]
        public async Task<ActionResult<List<GameHistoryListItemDto>>> GetGameHistory()
        {
            try
            {
                var games = await _gameService.GetGameHistoryAsync();
                return Ok(games);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex);
            }
        }

        [HttpGet("player-page/active")]
        public async Task<ActionResult<ActiveGamePlayerPageDto>> GetActiveGameForPlayer()
        {
            try
            {
                var userId = User.GetUserId();

                var game = await _gameService.GetActiveGameForPlayerAsync(userId);
                if (game == null) return NotFound();

                return Ok(game);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [Authorize(Roles = "Admin, Gamemaster")]
        [HttpGet("game-panel/active/all")]
        public async Task<ActionResult<List<GamePanelDto>>> GetAllActiveGamesForGamePanel()
        {
            try
            {
                int userId = User.GetUserId();
                var games = await _gameService.GetAllActiveGamesForGamePanelAsync(userId);
                return Ok(games);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }





        [HttpGet("{id}")]
        public async Task<ActionResult<GameDetailsDto>> GetGameDetails(int id)
        {
            try
            {
                var role = User.GetUserRole();

                var game = await _gameService.GetGameDetailsAsync(id, role);

                if (game == null)
                    return NotFound();

                return Ok(game);
            }
            catch (UnauthorizedAccessException)
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
                return StatusCode(500, ex);
            }
        }

        [Authorize]
        [HttpPost("{gameId}/join")]
        public async Task<IActionResult> JoinGameAsPlayer(int gameId)
        {
            try
            {
                var currentUserId = User.GetUserId(); 
                var player = await _gameService.JoinGameAsPlayerAsync(gameId, currentUserId);

                return Ok(new { message = "Joined game", player });
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
                return StatusCode(500, new { message = ex.Message });
            }
        }

        //[Authorize]
        //[HttpPost("{gameId}/leave")]
        //public async Task<IActionResult> LeaveGame(int gameId, [FromBody] GameActionDto? dto = null)
        //{
        //    try
        //    {
        //        var currentUserId = User.GetUserId();
        //        var role = User.GetUserRole();

        //        var userIdToLeave = (role == "Admin" || role == "Gamemaster") && dto != null
        //            ? dto.TargetUserId
        //            : currentUserId;

        //        await _gameService.LeaveGameAsync(gameId, userIdToLeave);

        //        return Ok(new { message = "Player left the game", GameId = gameId, UserId = userIdToLeave });
        //    }
        //    catch (KeyNotFoundException ex)
        //    {
        //        return NotFound(new { message = ex.Message });
        //    }
        //    catch (InvalidOperationException ex)
        //    {
        //        return BadRequest(new { message = ex.Message });
        //    }
        //    catch (Exception)
        //    {
        //        return StatusCode(500, "Unexpected error");
        //    }
        //}


        [HttpGet("lobby")]
        public async Task<ActionResult<List<GameListDto>>> GetActiveGamesLobby()
        {
            try
            {
                var games = await _gameService.GetActiveGamesLobbyListAsync();
                return Ok(games);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }
    }
}
