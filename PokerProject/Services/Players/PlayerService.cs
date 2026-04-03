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

            var players = new List<Player>();

            foreach (var userId in userIds)
            {
                var existingPlayer = await _context.Players
                    .FirstOrDefaultAsync(p => p.GameId == gameId && p.UserId == userId);

                if (existingPlayer != null)
                {
                    if (!existingPlayer.IsActive)
                    {
                        existingPlayer.IsActive = true;
                        players.Add(existingPlayer);
                    }

                    continue;
                }

                var player = new Player
                {
                    GameId = gameId,
                    UserId = userId,
                    IsActive = true,
                };

                _context.Players.Add(player);
                players.Add(player);
            }

            await _context.SaveChangesAsync();

            var result = new List<PlayerDto>();

            foreach (var player in players)
            {
                var user = await _context.Users.FindAsync(player.UserId);

                result.Add(new PlayerDto
                {
                    PlayerId = player.Id,
                    UserId = player.UserId,
                    Username = user?.Username ?? "Unknown",
                    RebuyCount = 0,
                    ActiveBounties = 0,
                    IsActive = true
                });
            }

            return result;
        }

        public async Task<List<PlayerDto>> GetPlayersAsync(int gameId)
        {
            return await _context.Players
                .AsNoTracking()
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
                .AsNoTracking()
                .AnyAsync(gp => gp.GameId == gameId && gp.UserId == userId);
        }


        public async Task LeaveGameAsPlayerAsync(int gameId, int userId)
        {
            var game = await _context.Games.FindAsync(gameId);

            if (game.IsFinished)
                throw new Exception("Game already finished");

            var player = await _context.Players
                .FirstOrDefaultAsync(p => p.GameId == gameId && p.UserId == userId);

            if (player == null)
                throw new Exception("Player not found");

            if (!player.IsActive)
                return;

            player.IsActive = false;
            player.LeftAt = DateTimeOffset.UtcNow;

            await _context.SaveChangesAsync();
        }

        public async Task RemovePlayerAsAdminAsync(int gameId, int playerId)
        {
            var player = await _context.Players
        .FirstOrDefaultAsync(p => p.GameId == gameId && p.Id == playerId);

            if (player == null)
                throw new Exception("Player not found");

            if (!player.IsActive)
                return;

            player.IsActive = false;
            player.LeftAt = DateTimeOffset.UtcNow;

            await _context.SaveChangesAsync();

        }


    }
}