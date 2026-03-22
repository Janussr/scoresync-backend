using Microsoft.EntityFrameworkCore;
using PokerProject.Data;
using PokerProject.DTOs;
using PokerProject.Models;

namespace PokerProject.Services.Players
{
    public class PlayerService : IPlayerService
    {
        private readonly PokerDbContext _context;

        public PlayerService(PokerDbContext context)
        {
            _context = context;
        }


        public async Task<List<PlayerDto>> AddPlayersToGameAsAdminAsync(int gameId, List<int> userIds)
        {
            var game = await _context.Games.FindAsync(gameId);
            if (game == null)
                throw new KeyNotFoundException("Game not found");

            var addedPlayers = new List<PlayerDto>();

            foreach (var userId in userIds)
            {
                var exists = await _context.Players
                    .AnyAsync(p => p.GameId == gameId && p.UserId == userId);

                if (!exists)
                {
                    var player = new Player
                    {
                        GameId = gameId,
                        UserId = userId
                    };
                    _context.Players.Add(player);
                    addedPlayers.Add(new PlayerDto
                    {
                        UserId = player.UserId,
                        Username = (await _context.Users.FindAsync(userId))?.Username ?? "Unknown",
                        RebuyCount = 0,
                        ActiveBounties = 0,
                        IsActive = true
                    });
                }
            }

            await _context.SaveChangesAsync();
            return addedPlayers;
        }

        public async Task<List<PlayerDto>> GetPlayersAsync(int gameId)
        {
            return await _context.Players
                .Where(gp => gp.GameId == gameId)
                .Include(gp => gp.User)
                .Select(gp => new PlayerDto
                {
                    UserId = gp.UserId,
                    Username = gp.User.Username
                })
                .ToListAsync();
        }

        public async Task<bool> IsUserAPlayerAsync(int gameId, int userId)
        {
            return await _context.Players
                .AnyAsync(gp => gp.GameId == gameId && gp.UserId == userId);
        }

        public async Task<List<PlayerDto>> RemovePlayerAsync(int gameId, int userId)
        {
            var game = await _context.Games
                .Include(g => g.Players)
                .FirstOrDefaultAsync(g => g.Id == gameId);

            if (game == null)
                throw new KeyNotFoundException("Game not found");

            if (game.IsFinished)
                throw new InvalidOperationException("Cannot remove players from a finished game");

            var player = await _context.Players
                .FirstOrDefaultAsync(p => p.GameId == gameId && p.UserId == userId);

            if (player == null)
                throw new InvalidOperationException("User is not a player in this game");

            _context.Players.Remove(player);
            await _context.SaveChangesAsync();

            return await _context.Players
                .Where(p => p.GameId == gameId)
                .Include(p => p.User)
                .Select(p => new PlayerDto
                {
                    UserId = p.UserId,
                    Username = p.User.Username
                })
                .ToListAsync();
        }

    }
}
