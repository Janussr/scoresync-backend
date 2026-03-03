using Microsoft.AspNetCore.Mvc;
using PokerProject.DTOs;
using PokerProject.Services;
using PokerProject.Models;

namespace PokerProject.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HallOfFameController : ControllerBase
    {

        private readonly IHallOfFameService _hallOfFameService;

        public HallOfFameController(IHallOfFameService hallOfFameService)
        {
            _hallOfFameService = hallOfFameService;
        }

        [HttpGet]
        public async Task<ActionResult<List<HallOfFameDto>>> GetHallOfFame()
        {
            var hof = await _hallOfFameService.GetEntireHallOfFameAsync();
            return hof;
        }

    }
    }



