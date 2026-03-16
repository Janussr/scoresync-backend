using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PokerProject.DTOs;
using PokerProject.Services.Participants;

namespace PokerProject.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ParticipantsController : ControllerBase
    {
        private readonly IParticipantService _participantService;

        public ParticipantsController(IParticipantService participantService)
        {
            _participantService = participantService;
        }

        [Authorize(Roles = "Admin, User, Gamemaster")]
        [HttpPost("{gameId}")]
        public async Task<IActionResult> AddParticipants(int gameId, [FromBody] AddParticipantsDto dto)
        {
            try
            {
                await _participantService.AddParticipantsAsync(gameId, dto.UserIds);
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
                var participants = await _participantService.GetParticipantsAsync(gameId);
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
        [Authorize(Roles = "Admin, Gamemaster")]
        public async Task<IActionResult> RemoveParticipant(int gameId, int userId)
        {
            try
            {
                var updatedParticipants = await _participantService.RemoveParticipantAsync(gameId, userId);
                return Ok(updatedParticipants);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }

        }

    }
}
