using Microsoft.EntityFrameworkCore;
using PokerProject.Data;
using PokerProject.DTOs;

namespace PokerProject.Services.HallOfFames
{
    public class HallOfFameService : IHallOfFameService
    {
        private readonly PokerDbContext _context;
        public HallOfFameService(PokerDbContext context)
        {

            _context = context;

        }
        public async Task<List<HallOfFameDto>> GetEntireHallOfFameAsync()
        {
            return await _context.HallOfFames
                .AsNoTracking()
                .GroupBy(h => new
                {
                    h.Player.UserId,
                    h.Player.User.Username,
                    h.Game.Type
                })
                .Select(g => new HallOfFameDto
                {
                    UserId = g.Key.UserId,
                    PlayerName = g.Key.Username,
                    GameType = g.Key.Type,
                    Wins = g.Count()
                })
                .OrderByDescending(x => x.Wins)
                .ToListAsync();
        }
    }
    }
