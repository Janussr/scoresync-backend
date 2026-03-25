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

        //USED TO ADD SCORE AS EITHER ADMIN OR PLAYER
        public async Task<ScoreDto> AddScoreAsync(int gameId, int currentUserId, int points, int? targetPlayerId = null)
        {
            var game = await _context.Games.FindAsync(gameId);
            if (game == null)
                throw new KeyNotFoundException("Game not found");

            if (game.IsFinished)
                throw new InvalidOperationException("Game has ended – can't add points.");

            var round = await GetActiveRound(gameId);
            if (round == null)
                throw new InvalidOperationException("No active round found for this game.");

            if (targetPlayerId == null)
            {
                var currentPlayer = await _context.Players
                    .FirstOrDefaultAsync(p => p.GameId == gameId && p.UserId == currentUserId);

                if (currentPlayer == null)
                    throw new InvalidOperationException("You are not a player in this game.");

                targetPlayerId = currentPlayer.Id;
            }

            var targetPlayer = await _context.Players
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.Id == targetPlayerId.Value && p.GameId == gameId);

            if (targetPlayer == null)
                throw new InvalidOperationException("Target player not found in this game.");

            var score = new Score
            {
                RoundId = round.Id,
                PlayerId = targetPlayer.Id,
                Value = points,
                CreatedAt = DateTime.UtcNow,
                Type = Score.ScoreType.Chips,
            };

            _context.Scores.Add(score);
            await _context.SaveChangesAsync();

            return new ScoreDto
            {
                PlayerId = targetPlayer.Id,
                UserId = targetPlayer.UserId,
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
                var playerIds = dto.Scores.Select(s => s.PlayerId).ToList();
                var players = await _context.Players
                    .Where(p => p.GameId == game.Id && playerIds.Contains(p.Id))
                    .ToListAsync();

                var missingPlayers = playerIds.Except(players.Select(p => p.Id)).ToList();
                if (missingPlayers.Any())
                    throw new InvalidOperationException($"Players {string.Join(", ", missingPlayers)} are not in this game.");

                var addedScores = new List<Score>();

                foreach (var s in dto.Scores)
                {
                    var player = players.First(p => p.Id == s.PlayerId);

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
                        PlayerId = player.Id, 
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

        public async Task<PlayerScoreDetailsDto> GetPlayerScoreEntries(int gameId, int playerId)
        {
            var player = await _context.Players
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.GameId == gameId && p.Id == playerId);

            if (player == null)
                throw new KeyNotFoundException("Player not found in this game");

            // Hent alle scores for denne player i spillet
            var scores = await _context.Scores
                .Include(s => s.VictimPlayer)
                    .ThenInclude(v => v.User)
                .Include(s => s.Round)
                .Where(s => s.Round.GameId == gameId && s.PlayerId == player.Id)
                .OrderBy(s => s.CreatedAt)
                .ToListAsync();

            if (!scores.Any())
                throw new KeyNotFoundException("No scores found for this player in this game");

            // Gruppér scores per round
            var roundGroups = scores
                .GroupBy(s => s.RoundId)
                .Select(g => new RoundScoreDto
                {
                    RoundId = g.Key!.Value,
                    RoundNumber = g.First().Round.RoundNumber,
                    StartedAt = g.First().Round.StartedAt,
                    TotalPoints = g.Sum(s => s.Value),
                    Entries = g.Select(s => new ScoreEntryDto
                    {
                        Id = s.Id,
                        Points = s.Value,
                        CreatedAt = s.CreatedAt,
                        Type = s.Type,
                        VictimUserId = s.VictimPlayerId != null ? s.VictimPlayer.UserId : null,
                        VictimUserName = s.VictimPlayerId != null ? s.VictimPlayer.User.Username : null
                    }).ToList()
                })
                .OrderBy(r => r.RoundNumber)
                .ToList();

            return new PlayerScoreDetailsDto
            {
                UserId = player.UserId,
                PlayerId = player.Id,
                UserName = player.User.Username,
                TotalPoints = scores.Sum(s => s.Value),
                Rounds = roundGroups
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

        public async Task<ScoreDto> RegisterRebuyAsync(int gameId, int actorUserId, int? targetPlayerId = null)
        {
            var game = await _context.Games
                .Include(g => g.Players)
                    .ThenInclude(p => p.User)
                .FirstOrDefaultAsync(g => g.Id == gameId);

            if (game == null)
                throw new KeyNotFoundException("Game not found");

            if (game.RebuyValue == null)
                throw new InvalidOperationException("Rebuy value not set");

            var round = await GetActiveRound(gameId);
            if (round == null)
                throw new InvalidOperationException("No active round");

            var player = targetPlayerId.HasValue
                ? game.Players.FirstOrDefault(p => p.Id == targetPlayerId.Value)     
                : game.Players.FirstOrDefault(p => p.UserId == actorUserId);        

            if (player == null)
                throw new InvalidOperationException("Player not found in this game");

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
                UserName = player.User?.Username ?? "Unknown",
                Points = score.Value,
                GameId = game.Id,
                Rounds = new RoundDto
                {
                    Id = round.Id,
                    RoundNumber = round.RoundNumber
                },
                Type = score.Type
            };
        }


    }
    }
