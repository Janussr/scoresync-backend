using Microsoft.EntityFrameworkCore;
using PokerProject.Data;
using PokerProject.DTOs;

namespace PokerProject.Services.HallOfFames
{
    public class HallOfFameService : IHallOfFameService
    {
        private readonly PokerDbContext _context;
        public  HallOfFameService(PokerDbContext context) {

            _context = context;

        }
        public async Task<List<HallOfFameDto>> GetEntireHallOfFameAsync()
        {
            var hallOfFame = await _context.HallOfFames
                .GroupBy(h => new { h.PlayerId, h.Player.User.Username })
                .Select(g => new HallOfFameDto
                {
                    PlayerName = g.Key.Username,
                    Wins = g.Count()
                })
                .OrderByDescending(x => x.Wins)   
                .ToListAsync();

            return hallOfFame;
        }


    }
}
