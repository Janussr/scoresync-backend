using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PokerProject.DTOs;
using PokerProject.Services.Scores;

namespace PokerProject.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ScoresController : Controller
    {
        private readonly IScoreService _scoreService;

        public ScoresController(IScoreService scoreService)
        {
            _scoreService = scoreService;
        }


        [Authorize(Roles = "Admin, User, Gamemaster")]
        [HttpPost("{gameId}/score")]
        public async Task<ActionResult<ScoreDto>> AddScore(int gameId, [FromBody] AddScoreDto dto)
        {
            try
            {
                var added = await _scoreService.AddScoreAsync(gameId, dto.UserId, dto.Value);
                return Ok(added);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("{gameId}/players/{userId}/scores")]
        public async Task<ActionResult<PlayerScoreDetailsDto>> GetPlayerScores(int gameId, int userId)
        {
            try
            {
                var result = await _scoreService.GetPlayerScoreEntries(gameId, userId);
                return Ok(result);
            }
            catch (KeyNotFoundException e)
            {
                return NotFound(e.Message);
            }
        }

        [HttpDelete("points/{scoreId}")]
        [Authorize(Roles = "Admin, Gamemaster")]
        public async Task<IActionResult> RemoveGamePoints(int scoreId)
        {
            try
            {
                var updatedScore = await _scoreService.RemoveScoreAsync(scoreId);
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

        [Authorize(Roles = "Admin, Gamemaster")]
        [HttpPost("{gameId}/points/bulk")]
        public async Task<IActionResult> AddScoresBulk(int gameId, [FromBody] BulkAddScoresDto dto)
        {
            if (gameId != dto.GameId)
                return BadRequest(new { message = "GameId mismatch" });

            try
            {
                var updatedScores = await _scoreService.AddScoresBulkAsync(dto);
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


        //For player
        [Authorize]
        [HttpPost("{gameId}/rebuy")]
        public async Task<IActionResult> Rebuy(int gameId)
        {
            try
            {
                var userId = User.GetUserId();
                var isAdmin = User.GetUserRole() == "Admin";

                var result = await _scoreService.RegisterRebuyForAdminAsync(
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

        //For admin
        [Authorize(Roles = "Admin, Gamemaster")]
        [HttpPost("{gameId}/admin/rebuy")]
        public async Task<IActionResult> AdminRebuy([FromBody] RebuyRequestDto req)
        {

            try
            {
                var adminId = User.GetUserId();

                var result = await _scoreService.RegisterRebuyForAdminAsync(
                  req.GameId,
            req.ActorUserId,
            req.TargetUserId,
            req.IsAdmin
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
    }
}
