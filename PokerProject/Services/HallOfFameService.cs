using Microsoft.EntityFrameworkCore;
using PokerProject.Data;
using PokerProject.DTOs;

namespace PokerProject.Services
{

    public interface IHallOfFameService
    {
        Task<List<HallOfFameDto>> GetEntireHallOfFameAsync();
    }

    public class HallOfFameService : IHallOfFameService
    {
        private readonly PokerDbContext _context;
        public  HallOfFameService(PokerDbContext context) {

            _context = context;

        }
        public async Task<List<HallOfFameDto>> GetEntireHallOfFameAsync()
        {
            var hallOfFame = await _context.HallOfFames
                .GroupBy(h => new { h.UserId, h.User.Name })
                .Select(g => new HallOfFameDto
                {
                    PlayerName = g.Key.Name,
                    Wins = g.Count()
                })
                .OrderByDescending(x => x.Wins)   
                .ToListAsync();

            return hallOfFame;
        }


    }
}
