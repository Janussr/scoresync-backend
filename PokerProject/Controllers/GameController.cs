using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PokerProject.DTOs;
using PokerProject.Services;

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

        [Authorize(Roles = "Admin")]
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
                // Alt andet uventet
                return StatusCode(500, "Unexpected error");
            }
        }

        [Authorize(Roles = "Admin, User")]
        [HttpPost("{gameId}/score")]
        public async Task<ActionResult<ScoreDto>> AddScore(int gameId, [FromBody] AddScoreDto dto)
        {
            try
            {
                var added = await _gameService.AddScoreAsync(gameId, dto.UserId, dto.Value);
                return Ok(added);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [Authorize(Roles = "Admin")]
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

        [Authorize(Roles = "Admin")]
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

        [Authorize(Roles = "Admin, User")]
        [HttpPost("{gameId}/participants")]
        public async Task<IActionResult> AddParticipants(int gameId, [FromBody] AddParticipantsDto dto)
        {
            try
            {
                await _gameService.AddParticipantsAsync(gameId, dto.UserIds);
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

        [HttpGet("{gameId}/participants")]
        public async Task<ActionResult<List<ParticipantDto>>> GetParticipants(int gameId)
        {
            try
            {
                var participants = await _gameService.GetParticipantsAsync(gameId);
                return Ok(participants);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Unexpected error while fetching participants");
            }
        }

        [HttpDelete("{gameId}/participants/{userId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RemoveParticipant(int gameId, int userId)
        {

            try
            {
                var updatedParticipants = await _gameService.RemoveParticipantAsync(gameId, userId);
                return Ok(updatedParticipants);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }

        }

        [HttpGet("{gameId}/players/{userId}/scores")]
        public async Task<ActionResult<PlayerScoreDetailsDto>> GetPlayerScores(int gameId, int userId)
        {
            try
            {
                var result = await _gameService.GetPlayerScoreEntries(gameId, userId);
                return Ok(result);
            }
            catch (KeyNotFoundException e)
            {
                return NotFound(e.Message);
            }
        }

        [HttpDelete("points/{scoreId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RemoveGamePoints(int scoreId)
        {
            try
            {
                var updatedScore = await _gameService.RemoveScoreAsync(scoreId);
                return Ok(updatedScore);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpPost("{gameId}/points/bulk")]
        public async Task<IActionResult> AddScoresBulk(int gameId, [FromBody] BulkAddScoresDto dto)
        {
            if (gameId != dto.GameId)
                return BadRequest(new { message = "GameId mismatch" });

            try
            {
                var updatedScores = await _gameService.AddScoresBulkAsync(dto);
                return Ok(updatedScores);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }
        [HttpDelete("remove/{gameId}")]
        [Authorize(Roles = "Admin")]
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


        [Authorize(Roles = "Admin")]
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

        //[Authorize]
        //[HttpPost("{gameId}/rebuy")]
        //public async Task<IActionResult> Rebuy(int gameId)
        //{
        //    try
        //    {
        //        var userId = User.GetUserId();
        //        var isAdmin = User.GetUserRole() == "Admin";
        //        var result = await _gameService.RegisterRebuyAsync(gameId, userId, isAdmin);
        //        return Ok(result);
        //    }
        //    catch (UnauthorizedAccessException ex)
        //    {
        //        return Forbid(ex.Message); 
        //    }
        //    catch (InvalidOperationException ex)
        //    {
        //        return BadRequest(ex.Message); 
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, ex.Message); 
        //    }

        //}

        [Authorize]
        [HttpPost("{gameId}/rebuy")]
        public async Task<IActionResult> Rebuy(int gameId)
        {
            try
            {
                var userId = User.GetUserId();
                var isAdmin = User.GetUserRole() == "Admin";

                var result = await _gameService.RegisterRebuyAsync(
                    gameId,
                    userId,
                    userId,
                    isAdmin
                );

                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("{gameId}/admin/rebuy")]
        public async Task<IActionResult> AdminRebuy(int gameId, [FromBody] int targetUserId)
        {
            try
            {
                var adminId = User.GetUserId();

                var result = await _gameService.RegisterRebuyAsync(
                    gameId,
                    adminId,
                    targetUserId,
                    true
                );

                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }

        }

        [Authorize]
        [HttpPost("{gameId}/bounty")]
        public async Task<IActionResult> RegisterKnockout(int gameId, [FromBody] KnockoutDto dto)
        {
            try
            {
                var userId = User.GetUserId();
                var isAdmin = User.GetUserRole() == "Admin";

                await _gameService.RegisterKnockoutAsync(
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
                return StatusCode(500, "Unexpected error"); 
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("{gameId}/admin/bounty")]
        public async Task<IActionResult> AdminRegisterKnockout(int gameId, [FromBody] KnockoutDto dto)
        {
            try
            {
                 var isAdmin = User.GetUserRole() == "Admin";

                await _gameService.RegisterKnockoutAsync(
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
                return StatusCode(500, "Unexpected error"); 
            }
        }

        [HttpGet("bounty-leaderboard")]
        public async Task<IActionResult> GetBountyLeaderboard()
        {
            var leaderboard = await _gameService.GetBountyLeaderboardAsync();
            return Ok(leaderboard);
        }
    }
}
