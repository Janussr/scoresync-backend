using Microsoft.EntityFrameworkCore;
using PokerProject.Data;
using PokerProject.DTOs;
using PokerProject.Models;

namespace PokerProject.Services.Scores
{
    public class ScoreService : IScoreService
    {
        private readonly PokerDbContext _context;
        public ScoreService(PokerDbContext context)
        {
            _context = context;
        }

        private async Task<Round> GetActiveRound(int gameId)
        {
            var round = await _context.Rounds
                .FirstOrDefaultAsync(r => r.GameId == gameId && r.EndedAt == null);

            if (round == null)
                throw new InvalidOperationException("No active round");

            return round;
        }

        public async Task<ScoreDto> AddScoreAsync(int gameId, int userId, int points)
        {
            var game = await _context.Games.FindAsync(gameId);
            if (game == null)
                throw new KeyNotFoundException("Game not found");

            if (game.IsFinished)
                throw new InvalidOperationException("Game has ended – can't add points.");

            var round = await GetActiveRound(gameId);
            if (round == null)
                throw new InvalidOperationException("No active round found for this game.");

            var player = await _context.Players
                .Include(p => p.User) 
                .FirstOrDefaultAsync(p => p.GameId == gameId && p.UserId == userId);

            if (player == null)
                throw new InvalidOperationException("Player not found in this game.");

            var score = new Score
            {
                RoundId = round.Id,
                PlayerId = player.Id,      
                Value = points,
                CreatedAt = DateTime.UtcNow,
                Type = Score.ScoreType.Chips,
            };

            _context.Scores.Add(score);
            await _context.SaveChangesAsync();

            return new ScoreDto
            {
                PlayerId = player.Id,         
                UserId = player.UserId,
                Points = score.Value,
                Type = score.Type,
                Rounds = new RoundDto
                {
                    Id = round.Id,
                    RoundNumber = round.RoundNumber,
                    StartedAt = round.StartedAt
                }
            };
        }

        public async Task<List<ScoreDto>> AddScoresBulkAsync(BulkAddScoresDto dto)
        {
            var game = await _context.Games.FindAsync(dto.GameId);
            if (game == null)
                throw new KeyNotFoundException("Game not found");

            if (game.IsFinished)
                throw new InvalidOperationException("Game has ended – can't add points.");

            var round = await GetActiveRound(dto.GameId);
            if (round == null)
                throw new InvalidOperationException("No active round found for this game.");

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var userIds = dto.Scores.Select(s => s.UserId).ToList();
                var players = await _context.Players
                    .Where(p => p.GameId == game.Id && userIds.Contains(p.UserId))
                    .ToListAsync();

                var missingUsers = userIds.Except(players.Select(p => p.UserId)).ToList();
                if (missingUsers.Any())
                    throw new InvalidOperationException($"Users {string.Join(", ", missingUsers)} are not players in this game.");

                var addedScores = new List<Score>();

                foreach (var s in dto.Scores)
                {
                    var player = players.First(p => p.UserId == s.UserId);

                    var score = new Score
                    {
                        RoundId = round.Id,
                        PlayerId = player.Id,
                        Value = s.Points,
                        CreatedAt = DateTime.UtcNow,
                        Type = Score.ScoreType.Chips,
                    };

                    _context.Scores.Add(score);
                    addedScores.Add(score);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return addedScores.Select(s =>
                {
                    var player = players.First(p => p.Id == s.PlayerId);
                    return new ScoreDto
                    {
                        Id = s.Id,
                        PlayerId = player.UserId,
                        Points = s.Value,
                        Type = s.Type,
                        Rounds = new RoundDto
                        {
                            Id = round.Id,
                            RoundNumber = round.RoundNumber,
                            StartedAt = round.StartedAt
                        }
                    };
                }).ToList();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<PlayerScoreDetailsDto> GetPlayerScoreEntries(int gameId, int userId)
        {
            var scores = await _context.Scores
                .Include(s => s.VictimPlayer)
                .Include(s => s.Round)
                .Where(s => s.Round.GameId == gameId && s.PlayerId == userId)
                .OrderBy(s => s.CreatedAt)
                .Select(s => new ScoreEntryDto
                {
                    Id = s.Id,
                    Points = s.Value,
                    CreatedAt = s.CreatedAt,
                    Type = s.Type,
                    VictimUserId = s.VictimPlayerId,
                    VictimUserName = s.VictimPlayerId != null ? s.VictimPlayer.User.Username : null
                })
                .ToListAsync();

            if (!scores.Any())
                throw new KeyNotFoundException("No scores found for this player in this game");

            var user = await _context.Users.FindAsync(userId);

            return new PlayerScoreDetailsDto
            {
                UserId = userId,
                UserName = user!.Username,
                TotalPoints = scores.Sum(s => s.Points),
                Entries = scores
            };
        }

        public async Task<ScoreDto> RemoveScoreAsync(int scoreId)
        {
            var score = await _context.Scores
                .Include(s => s.Round)
                .FirstOrDefaultAsync(s => s.Id == scoreId);

            if (score == null)
                throw new KeyNotFoundException("Score not found");

            var game = await _context.Games.FindAsync(score.Round.GameId);

            if (game == null)
                throw new KeyNotFoundException("Game not found");

            if (game.IsFinished)
                throw new InvalidOperationException("Game has ended - can't remove points.");

            score.Value = 0;
            await _context.SaveChangesAsync();

            return new ScoreDto
            {
                Id = score.Id,
                PlayerId = score.PlayerId,
                Points = score.Value
            };
        }

        public async Task<ScoreDto> RegisterRebuyForAdminAsync(int gameId, int actorUserId, int targetUserId, bool isAdmin)
        {
            var game = await _context.Games
                .Include(g => g.Players)
                .FirstOrDefaultAsync(g => g.Id == gameId);

            if (game == null) throw new KeyNotFoundException("Game not found");
            if (game.RebuyValue == null) throw new InvalidOperationException("Rebuy value not set");

            if (!isAdmin && actorUserId != targetUserId)
                throw new UnauthorizedAccessException("Players can only rebuy themselves");

            var player = game.Players.FirstOrDefault(p => p.UserId == targetUserId);
            if (player == null) throw new InvalidOperationException("Target user is not player");

            var round = await GetActiveRound(gameId);

            player.RebuyCount++;

            var score = new Score
            {
                RoundId = round.Id,
                PlayerId = player.Id,            
                Value = -game.RebuyValue.Value,
                CreatedAt = DateTime.UtcNow,
                Type = Score.ScoreType.Rebuy,
            };

            _context.Scores.Add(score);
            await _context.SaveChangesAsync();

            return new ScoreDto
            {
                Id = score.Id,
                PlayerId = player.Id,
                UserName = player.User.Username,
                Points = score.Value,
                GameId = game.Id,
                Rounds = new RoundDto { Id = round.Id },
                Type = score.Type
            };
        }


    }
    }
