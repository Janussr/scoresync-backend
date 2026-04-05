using Microsoft.EntityFrameworkCore;
using PokerProject.Data;
using PokerProject.DTOs.Bounties;
using PokerProject.DTOs.Rounds;
using PokerProject.DTOs.Scores;
using PokerProject.Hubs.GameNotifier;

namespace PokerProject.Services.Bounties
{
    public class BountyService : IBountyService
    {
        private readonly PokerDbContext _context;
        private readonly IGameNotifier _gameNotifier;

        public BountyService(PokerDbContext context, IGameNotifier gameNotifier)
        {
            _context = context;
            _gameNotifier = gameNotifier;
        }

        public async Task<ScoreDto> PlayerKnockoutAsync(int gameId, int killerUserId, int? victimPlayerId)
        {
            var killer = await _context.Players
                .FirstOrDefaultAsync(p => p.GameId == gameId && p.UserId == killerUserId);

            if (killer == null)
                throw new InvalidOperationException("You are not a player in this game");

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
                CreatedAt = DateTimeOffset.UtcNow
            };

            _context.Scores.Add(score);

            victim.ActiveBounties = 0;
            killer.ActiveBounties += 1;

            await _context.SaveChangesAsync();

            await _gameNotifier.KnockoutUpdated(gameId, new KnockoutUpdatedDto
            {
                GameId = gameId,
                KillerPlayerId = killer.Id,
                VictimPlayerId = victim.Id,
                KillerActiveBounties = killer.ActiveBounties,
                VictimActiveBounties = victim.ActiveBounties,
                Score = new ScoreDto
                {
                    Id = score.Id,
                    PlayerId = killer.Id,
                    UserId = killer.UserId,
                    Points = score.Value,
                    Type = score.Type,
                    Rounds = new RoundDto
                    {
                        Id = currentRound.Id,
                        RoundNumber = currentRound.RoundNumber,
                        StartedAt = currentRound.StartedAt,
                        EndedAt = currentRound.EndedAt
                    }
                }
            });

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
            var knockoutsQuery = from s in _context.Scores
                                 where s.Type == Score.ScoreType.Bounty
                                 join p in _context.Players on s.PlayerId equals p.Id
                                 select new { p.UserId, s.Value };

            var knockoutsGrouped = await knockoutsQuery
                .GroupBy(k => k.UserId)
                .Select(g => new
                {
                    UserId = g.Key,
                    Knockouts = g.Count(),
                    TotalBountyPoints = g.Sum(x => x.Value)
                })
                .ToListAsync();

            var timesKnockedOutQuery = from s in _context.Scores
                                       where s.Type == Score.ScoreType.Bounty && s.VictimPlayerId.HasValue
                                       join p in _context.Players on s.VictimPlayerId equals p.Id
                                       select p.UserId;

            var timesKnockedOutGrouped = await timesKnockedOutQuery
                .GroupBy(uId => uId)
                .Select(g => new
                {
                    VictimUserId = g.Key,
                    TimesKnockedOut = g.Count()
                })
                .ToListAsync();

            var knockoutsDict = knockoutsGrouped.ToDictionary(k => k.UserId, k => new { k.Knockouts, k.TotalBountyPoints });
            var timesKnockedOutDict = timesKnockedOutGrouped.ToDictionary(t => t.VictimUserId, t => t.TimesKnockedOut);

            var allUserIds = knockoutsGrouped.Select(k => k.UserId)
                .Union(timesKnockedOutGrouped.Select(t => t.VictimUserId))
                .ToList();

            var users = await _context.Users
                .AsNoTracking()
                .Where(u => allUserIds.Contains(u.Id))
                .ToListAsync();

            var leaderboard = users.Select(u => new BountyLeaderboardDto
            {
                UserId = u.Id,
                UserName = u.Username,
                Knockouts = knockoutsDict.ContainsKey(u.Id) ? knockoutsDict[u.Id].Knockouts : 0,
                TotalBountyPoints = knockoutsDict.ContainsKey(u.Id) ? knockoutsDict[u.Id].TotalBountyPoints : 0,
                TimesKnockedOut = timesKnockedOutDict.ContainsKey(u.Id) ? timesKnockedOutDict[u.Id] : 0
            })
            .OrderByDescending(x => x.Knockouts)
            .ThenByDescending(x => x.TotalBountyPoints)
            .ToList();

            return leaderboard;
        }


    }
}
