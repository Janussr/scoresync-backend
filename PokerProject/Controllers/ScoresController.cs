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
        [HttpPost("player/addscore")]
        public async Task<IActionResult> AddScorePlayer([FromBody] AddScoreDto request)
        {
            try
            {
                var currentUserId = User.GetUserId(); 

                var result = await _scoreService.AddScoreAsync(request.GameId, currentUserId, request.Value, null);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [Authorize(Roles = "Admin, User, Gamemaster")]
        [HttpPost("admin/addscore")]
        public async Task<IActionResult> AddScoreAdmin([FromBody] AddScoreAdminDto request)
        {
            try
            {
                var currentUserId = User.GetUserId(); 

                var result = await _scoreService.AddScoreAsync(
                    request.GameId,
                    currentUserId,
                    request.Value,
                    request.TargetPlayerId
                );

                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }






        [HttpGet("{gameId}/players/{playerId}/scores")]
        public async Task<ActionResult<PlayerScoreDetailsDto>> GetPlayerScores(int gameId, int playerId)
        {
            try
            {
                var result = await _scoreService.GetPlayerScoreEntries(gameId, playerId);
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
        [HttpPost("{gameId}/player/rebuy")]
        public async Task<IActionResult> Rebuy(int gameId)
        {
            try
            {
                var userId = User.GetUserId();

                var result = await _scoreService.RegisterRebuyAsync(gameId, userId);
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
        public async Task<IActionResult> AdminRebuy(int gameId, [FromBody] int targetPlayerId)
        {
            try
            {
                var adminId = User.GetUserId();

                var result = await _scoreService.RegisterRebuyAsync(gameId, adminId, targetPlayerId);
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
