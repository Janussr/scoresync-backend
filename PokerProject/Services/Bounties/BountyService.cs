using Microsoft.EntityFrameworkCore;
using PokerProject.Data;
using PokerProject.DTOs;

namespace PokerProject.Services.Bounties
{
    public class BountyService : IBountyService
    {
        private readonly PokerDbContext _context;

        public BountyService(PokerDbContext context)
        {
            _context = context;
        }

        public async Task<ScoreDto> PlayerKnockoutAsync(int gameId, int killerUserId, int? victimPlayerId)
        {
            // Find killer's PlayerId i game
            var killer = await _context.Players
                .FirstOrDefaultAsync(p => p.GameId == gameId && p.UserId == killerUserId);

            if (killer == null)
                throw new InvalidOperationException("You are not a player in this game");

            // Nu kalder vi eksisterende HandleKnockoutAsync med PlayerId'er
            return await HandleKnockoutAsync(gameId, killer.Id, victimPlayerId);
        }

        public async Task<ScoreDto> HandleKnockoutAsync(int gameId, int killerPlayerId, int? victimPlayerId)
        {
            var game = await _context.Games
                .Include(g => g.Rounds)
                .Include(g => g.Players)
                .FirstOrDefaultAsync(g => g.Id == gameId);

            if (game == null)
                throw new KeyNotFoundException("Game not found");

            if (!game.BountyValue.HasValue)
                throw new InvalidOperationException("Bounty value not set for this game");

            var currentRound = game.Rounds.FirstOrDefault(r => r.EndedAt == null);
            if (currentRound == null)
                throw new InvalidOperationException("No active round found");

            // Find spillerne via PlayerId
            var killer = game.Players.FirstOrDefault(p => p.Id == killerPlayerId);
            var victim = game.Players.FirstOrDefault(p => p.Id == victimPlayerId);

            if (killer == null) throw new InvalidOperationException("Killer not found in game");
            if (victim == null) throw new InvalidOperationException("Victim not found in game");
            if (killer.Id == victim.Id) throw new InvalidOperationException("Cannot knock yourself out");

            var bountyValue = game.BountyValue.Value;
            var points = victim.ActiveBounties > 0 ? victim.ActiveBounties * bountyValue : 0;

            var score = new Score
            {
                RoundId = currentRound.Id,
                PlayerId = killer.Id,
                VictimPlayerId = victim.Id,
                Value = points,
                Type = Score.ScoreType.Bounty,
                CreatedAt = DateTime.UtcNow
            };

            _context.Scores.Add(score);

            victim.ActiveBounties = 0;
            killer.ActiveBounties += 1;

            await _context.SaveChangesAsync();

            return new ScoreDto
            {
                PlayerId = killer.Id,
                UserId = killer.UserId,
                Points = score.Value,
                Type = score.Type,
                Rounds = new RoundDto
                {
                    Id = currentRound.Id,
                    RoundNumber = currentRound.RoundNumber,
                    StartedAt = currentRound.StartedAt
                }
            };
        }



        public async Task<List<BountyLeaderboardDto>> GetBountyLeaderboardAsync()
        {
            var knockoutsQuery = _context.Scores
                .Where(s => s.Type == Score.ScoreType.Bounty)
                .GroupBy(s => s.PlayerId)
                .Select(g => new
                {
                    UserId = g.Key,
                    Knockouts = g.Count(),
                    TotalBountyPoints = g.Sum(s => s.Value)
                });

            var timesKnockedOutQuery = _context.Scores
                .Where(s => s.Type == Score.ScoreType.Bounty && s.VictimPlayerId.HasValue)
                .GroupBy(s => s.VictimPlayerId.Value)
                .Select(g => new
                {
                    VictimUserId = g.Key,
                    TimesKnockedOut = g.Count()
                });

            var knockouts = await knockoutsQuery.ToListAsync();
            var timesKnockedOut = await timesKnockedOutQuery.ToListAsync();

            var userIds = knockouts.Select(k => k.UserId)
                .Union(timesKnockedOut.Select(t => t.VictimUserId))
                .ToList();

            var users = await _context.Users
                .Where(u => userIds.Contains(u.Id))
                .ToListAsync();

            var leaderboard = users.Select(u => new BountyLeaderboardDto
            {
                UserId = u.Id,
                UserName = u.Username,
                Knockouts = knockouts.FirstOrDefault(k => k.UserId == u.Id)?.Knockouts ?? 0,
                TimesKnockedOut = timesKnockedOut.FirstOrDefault(t => t.VictimUserId == u.Id)?.TimesKnockedOut ?? 0,
                TotalBountyPoints = knockouts.FirstOrDefault(k => k.UserId == u.Id)?.TotalBountyPoints ?? 0
            })
            .OrderByDescending(x => x.Knockouts)
            .ToList();

            return leaderboard;
        }



    }
}
