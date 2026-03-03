using Microsoft.EntityFrameworkCore;
using PokerProject.Data;
using PokerProject.DTOs;
using PokerProject.Models;
using static Score;

namespace PokerProject.Services
{
    public interface IGameService
    {
        Task<GameDto> StartGameAsync();
        Task<ScoreDto> AddScoreAsync(int gameId, int userId, int value);
        Task<List<ScoreDto>> AddScoresBulkAsync(BulkAddScoresDto dto);
        Task<ScoreDto> RemoveScoreAsync(int scoreId);
        Task<GameDto> EndGameAsync(int gameId);
        Task<GameDto> CancelGameAsync(int gameId);
        Task RemoveGameAsync(int gameId);
        Task<List<GameDto>> GetAllGamesAsync();
        Task<GameDto?> GetGameByIdAsync(int gameId);
        Task<GameDetailsDto?> GetGameDetailsAsync(int gameId, string? role);
        Task AddParticipantsAsync(int gameId, List<int> userIds);
        Task<List<ParticipantDto>> GetParticipantsAsync(int gameId);
        Task<bool> IsUserParticipantAsync(int gameId, int userId);
        Task<List<ParticipantDto>> RemoveParticipantAsync(int gameId, int userId);
        Task<PlayerScoreDetailsDto> GetPlayerScoreEntries(int gameId, int userId);
        //Task RegisterKnockoutAsync(int gameId, int victimUserId, int killerUserId);

        //Task AdminRegisterKnockoutAsync(int gameId, int killerUserId, int victimUserId);
        Task RegisterKnockoutAsync(int gameId, int killerUserId, int victimUserId);
        //Task HandleKnockoutAsync(int gameId, int killerUserId, int victimUserId);
        Task<ScoreDto> RegisterRebuyAsync(int gameId, int userId);
        Task UpdateRulesAsync(int gameId, UpdateRulesDto dto);
    }


    public class GameService : IGameService
    {
        private readonly PokerDbContext _context;

        public GameService(PokerDbContext context)
        {
            _context = context;
        }

        // Start a new game
        public async Task<GameDto> StartGameAsync()
        {
            var game = new Game
            {
                GameNumber = await GetNextGameNumber(),
                StartedAt = DateTime.UtcNow,
                IsFinished = false
            };

            _context.Games.Add(game);
            await _context.SaveChangesAsync();

            return new GameDto
            {
                Id = game.Id,
                GameNumber = game.GameNumber,
                StartedAt = game.StartedAt,
                IsFinished = game.IsFinished
            };
        }

        private async Task<int> GetNextGameNumber()
        {
            if (!await _context.Games.AnyAsync())
                return 1;

            return await _context.Games.MaxAsync(g => g.GameNumber) + 1;
        }



        // Add points for a player in a game
        public async Task<ScoreDto> AddScoreAsync(int gameId, int userId, int points)
        {
            var game = await _context.Games.FindAsync(gameId);
            if (game == null)
                throw new Exception("Game not found");

            if (game.IsFinished)
                throw new InvalidOperationException("Spillet er slut – kan ikke tilføje points.");

            var score = new Score
            {
                GameId = gameId,
                UserId = userId,
                Points = points,
                CreatedAt = DateTime.UtcNow,
                Type = Score.ScoreType.Chips,
            };

            _context.Scores.Add(score);
            await _context.SaveChangesAsync();

            return new ScoreDto
            {
                UserId = score.UserId,
                Points = score.Points,
                GameId = score.GameId,
                Type = score.Type,
            };
        }

        public async Task<List<ScoreDto>> AddScoresBulkAsync(BulkAddScoresDto dto)
        {
            var game = await _context.Games
                .Include(g => g.Scores)
                .FirstOrDefaultAsync(g => g.Id == dto.GameId);

            if (game == null)
                throw new Exception("Game not found");

            if (game.IsFinished)
                throw new InvalidOperationException("Game has ended - cant add points.");

            var addedScores = new List<Score>();

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                foreach (var s in dto.Scores)
                {
                    var score = new Score
                    {
                        GameId = game.Id,
                        UserId = s.UserId,
                        Points = s.Points,
                        CreatedAt = DateTime.UtcNow,
                        Type = Score.ScoreType.Chips,
                    };
                    _context.Scores.Add(score);
                    addedScores.Add(score);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }

            return addedScores.Select(s => new ScoreDto
            {
                Id = s.Id,
                UserId = s.UserId,
                Points = s.Points,
                Type = s.Type
            }).ToList();
        }

        public async Task<ScoreDto> RemoveScoreAsync(int scoreId)
        {
            var score = await _context.Scores.FindAsync(scoreId);
            if (score == null)
                throw new Exception("Score not found");

            var game = await _context.Games.FindAsync(score.GameId);
            if (game == null)
                throw new Exception("Game not found");

            if (game.IsFinished)
                throw new InvalidOperationException("Game has ended - can't remove points.");

            // Sæt score til 0
            score.Points = 0;
            await _context.SaveChangesAsync();

            return new ScoreDto
            {
                Id = score.Id,
                GameId = score.GameId,
                UserId = score.UserId,
                Points = score.Points
            };
        }


        // End game, calculate winner and update HallOfFame
        public async Task<GameDto> EndGameAsync(int gameId)
        {
            var game = await _context.Games
                .Include(g => g.Scores)
                .FirstOrDefaultAsync(g => g.Id == gameId);

            if (game == null)
                throw new Exception("Game not found");

            if (game.IsFinished)
                throw new Exception("Game already finished");

            if (!game.Scores.Any())
                throw new InvalidOperationException("No scores registered");

            // Beregn totals pr spiller
            var totals = game.Scores
                .GroupBy(s => s.UserId)
                .Select(g => new
                {
                    UserId = g.Key,
                    Total = g.Sum(x => x.Points)
                })
                .OrderByDescending(x => x.Total)
                .ToList();

            var winnerData = totals.First();

            var hallOfFame = new HallOfFame
            {
                GameId = game.Id,
                UserId = winnerData.UserId,
                WinningScore = winnerData.Total,
                WinDate = DateTime.UtcNow
            };

            _context.HallOfFames.Add(hallOfFame);

            // Mark game as finished
            game.IsFinished = true;
            game.EndedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Return DTO (så vi undgår JSON cycle)
            return new GameDto
            {
                Id = game.Id,
                GameNumber = game.GameNumber,
                StartedAt = game.StartedAt,
                EndedAt = game.EndedAt,
                IsFinished = game.IsFinished,
            };
        }

        public async Task<GameDto> CancelGameAsync(int gameId)
        {
            var game = await _context.Games
                .Include(g => g.Scores)
                .Include(g => g.Participants)
                .FirstOrDefaultAsync(g => g.Id == gameId);

            if (game == null)
                throw new KeyNotFoundException("Game not found");

            if (game.IsFinished)
                throw new InvalidOperationException("Game already finished");

            if (game.Scores.Any())
                throw new InvalidOperationException("Cannot cancel a game with scores");

            _context.Games.Remove(game);

            await _context.SaveChangesAsync();

            return new GameDto
            {
                Id = game.Id,
                GameNumber = game.GameNumber,
                StartedAt = game.StartedAt,
                EndedAt = DateTime.UtcNow,
                IsFinished = true
            };
        }

        public async Task<List<GameDto>> GetAllGamesAsync()
        {
            var games = await _context.Games
                .Select(g => new GameDto
                {
                    Id = g.Id,
                    GameNumber = g.GameNumber,
                    StartedAt = g.StartedAt,
                    EndedAt = g.EndedAt,
                    IsFinished = g.IsFinished,
                    RebuyValue = g.RebuyValue,
                    BountyValue = g.BountyValue,

                    Participants = g.Participants.Select(p => new ParticipantDto
                    {
                        UserId = p.UserId,
                        UserName = p.User.Name,
                        RebuyCount = p.RebuyCount,
                        ActiveBounties = p.ActiveBounties,
                    }).ToList(),

                    Scores = g.Scores.Select(s => new ScoreDto
                    {
                        Id = s.Id,
                        UserId = s.UserId,
                        UserName = s.User.Username,
                        Points = s.Points
                    }).ToList(),

                    Winner = g.Winner == null ? null : new WinnerDto
                    {
                        UserId = g.Winner.UserId,
                        UserName = g.Winner.User.Name,
                        WinningScore = g.Winner.WinningScore,
                        WinDate = g.Winner.WinDate
                    }
                })
                .ToListAsync();

            return games;
        }



        // Get a single game
        public async Task<GameDto?> GetGameByIdAsync(int id)
        {
            var game = await _context.Games
                .Include(g => g.Scores)
                .FirstOrDefaultAsync(g => g.Id == id);

            if (game == null) return null;

            return new GameDto
            {
                Id = game.Id,
                GameNumber = game.GameNumber,
                StartedAt = game.StartedAt,
                EndedAt = game.EndedAt,
                IsFinished = game.IsFinished,
                Scores = game.Scores.Select(s => new ScoreDto
                {
                    UserId = s.UserId,
                    Points = s.Points
                }).ToList()
            };
        }

        public async Task<GameDetailsDto?> GetGameDetailsAsync(int gameId, string? role)
        {
            var game = await _context.Games
                .Include(g => g.Scores)
                    .ThenInclude(s => s.User)
                .Include(g => g.Winner)
                    .ThenInclude(w => w.User)
                .FirstOrDefaultAsync(g => g.Id == gameId);

            if (game == null) return null;

            //Admin kan se scoreboard men User kan ikke.
            if (!game.IsFinished && role != "Admin")
                throw new UnauthorizedAccessException();


            // Summer points pr spiller
            var scores = game.Scores
                .GroupBy(s => new { s.UserId, s.User.Name })
                .Select(g => new GameScoreboardDto
                {
                    UserId = g.Key.UserId,
                    UserName = g.Key.Name,
                    TotalPoints = g.Sum(s => s.Points)
                })
                .ToList();

            WinnerDto? winnerDto = null;
            if (game.Winner != null)
            {
                winnerDto = new WinnerDto
                {
                    UserId = game.Winner.UserId,
                    UserName = game.Winner.User.Name,
                    WinningScore = game.Winner.WinningScore,
                    WinDate = game.Winner.WinDate
                };
            }

            return new GameDetailsDto
            {
                Id = game.Id,
                GameNumber = game.GameNumber,
                StartedAt = game.StartedAt,
                EndedAt = game.EndedAt,
                IsFinished = game.IsFinished,
                Scores = scores,
                Winner = winnerDto
            };
        }

        public async Task<PlayerScoreDetailsDto> GetPlayerScoreEntries(int gameId, int userId)
        {
            var scores = await _context.Scores.Include(s => s.VictimUser)
                .Where(s => s.GameId == gameId && s.UserId == userId)
                .OrderBy(s => s.CreatedAt)
                .Select(s => new ScoreEntryDto
                {
                    Id = s.Id,
                    Points = s.Points,
                    CreatedAt = s.CreatedAt,
                    Type = s.Type,
                    VictimUserId = s.VictimUserId,
                    VictimUserName = s.VictimUser != null ? s.VictimUser.Name : null
                })
                .ToListAsync();

            if (!scores.Any())
                throw new KeyNotFoundException("No scores found for this player in this game");

            var user = await _context.Users.FindAsync(userId);

            return new PlayerScoreDetailsDto
            {
                UserId = userId,
                UserName = user!.Name,
                TotalPoints = scores.Sum(s => s.Points),
                Entries = scores
            };
        }

        public async Task AddParticipantsAsync(int gameId, List<int> userIds)
        {
            foreach (var userId in userIds)
            {
                var exists = await _context.GameParticipants
                    .AnyAsync(gp => gp.GameId == gameId && gp.UserId == userId);

                if (!exists)
                {
                    _context.GameParticipants.Add(new GameParticipant
                    {
                        GameId = gameId,
                        UserId = userId
                    });
                }
            }

            await _context.SaveChangesAsync();
        }

        public async Task<List<ParticipantDto>> GetParticipantsAsync(int gameId)
        {
            return await _context.GameParticipants
                .Where(gp => gp.GameId == gameId)
                .Include(gp => gp.User)
                .Select(gp => new ParticipantDto
                {
                    UserId = gp.UserId,
                    UserName = gp.User.Name
                })
                .ToListAsync();
        }

        public async Task<bool> IsUserParticipantAsync(int gameId, int userId)
        {
            return await _context.GameParticipants
                .AnyAsync(gp => gp.GameId == gameId && gp.UserId == userId);
        }

        public async Task<List<ParticipantDto>> RemoveParticipantAsync(int gameId, int userId)
        {
            var game = await _context.Games
                .Include(g => g.Participants)
                .FirstOrDefaultAsync(g => g.Id == gameId);

            //Cannot be deleted if has score.
            //var hasScores = await _context.Scores.AnyAsync(s => s.GameId == gameId && s.UserId == userId);
            //if (hasScores)
            //    throw new InvalidOperationException("Cannot remove player who already has scores");

            if (game == null)
                throw new KeyNotFoundException("Game not found");

            if (game.IsFinished)
                throw new InvalidOperationException("Cannot remove participants from a finished game");

            var participant = await _context.GameParticipants
                .FirstOrDefaultAsync(p => p.GameId == gameId && p.UserId == userId);

            if (participant == null)
                throw new InvalidOperationException("User is not a participant in this game");

            _context.GameParticipants.Remove(participant);
            await _context.SaveChangesAsync();

            return await _context.GameParticipants
                .Where(p => p.GameId == gameId)
                .Include(p => p.User)
                .Select(p => new ParticipantDto
                {
                    UserId = p.UserId,
                    UserName = p.User.Name
                })
                .ToListAsync();
        }

        public async Task RemoveGameAsync(int gameId)
        {
            var game = await _context.Games
                .Include(g => g.Scores)
                .Include(g => g.Participants)
                .Include(g => g.Winner)
                .FirstOrDefaultAsync(g => g.Id == gameId);

            if (game == null)
                throw new KeyNotFoundException("Game not found");

            //if (game.IsFinished)
            //    throw new InvalidOperationException("Cannot delete a finished game");

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                _context.Scores.RemoveRange(game.Scores);

                _context.GameParticipants.RemoveRange(game.Participants);

                if (game.Winner != null)
                {
                    _context.HallOfFames.Remove(game.Winner);
                }

                _context.Games.Remove(game);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }


        private async Task HandleKnockoutAsync(int gameId, int killerUserId, int victimUserId)
        {
            var game = await _context.Games
                .Include(g => g.Participants)
                .Include(g => g.Scores)
                .FirstOrDefaultAsync(g => g.Id == gameId)
                ?? throw new Exception("Game not found");

            if (!game.BountyValue.HasValue)
                throw new Exception("Bounty value not set for this game");

            var killer = game.Participants
                .FirstOrDefault(p => p.UserId == killerUserId)
                ?? throw new Exception("Killer not found in game");

            var victim = game.Participants
                .FirstOrDefault(p => p.UserId == victimUserId)
                ?? throw new Exception("Victim not found in game");

            if (killer.UserId == victim.UserId)
                throw new Exception("Cannot knock yourself out");

            var bountyValue = game.BountyValue.Value;

            // 1️⃣ Calculate payout first
            var points = victim.ActiveBounties > 0
                ? victim.ActiveBounties * bountyValue
                : 0;

            // 2️⃣ Register knockout (ALWAYS)
            game.Scores.Add(new Score
            {
                GameId = game.Id,
                UserId = killerUserId,
                Points = points,
                Type = Score.ScoreType.Bounty,
                VictimUserId = victimUserId
            });

            // 3️⃣ Reset victim
            victim.ActiveBounties = 0;

            // 4️⃣ Killer gains bounty
            killer.ActiveBounties += 1;

            await _context.SaveChangesAsync();
        }

        public async Task RegisterKnockoutAsync(int gameId, int killerUserId, int victimUserId)
        {
            await HandleKnockoutAsync(gameId, killerUserId, victimUserId);
        }


        public async Task<ScoreDto> RegisterRebuyAsync(int gameId, int userId)
        {
            var game = await _context.Games
                .Include(g => g.Participants)
                .Include(g => g.Scores)
                .FirstOrDefaultAsync(g => g.Id == gameId);

            if (game == null)
                throw new KeyNotFoundException("Game not found");

            if (game.RebuyValue == null)
                throw new InvalidOperationException("Rebuy value not set by admin");

            var participant = game.Participants.FirstOrDefault(p => p.UserId == userId);
            if (participant == null)
                throw new InvalidOperationException("User not participant");

            participant.RebuyCount++;

            var score = new Score
            {
                GameId = gameId,
                UserId = userId,
                Points = -game.RebuyValue.Value,
                CreatedAt = DateTime.UtcNow,
                Type = Score.ScoreType.Rebuy,
            };

            _context.Scores.Add(score);
            await _context.SaveChangesAsync();

            return new ScoreDto
            {
                UserId = userId,
                Points = score.Points,
                Type = score.Type
            };
        }

        public async Task UpdateRulesAsync(int gameId, UpdateRulesDto dto)
        {
            var game = await _context.Games.FindAsync(gameId);
            if (game == null) throw new KeyNotFoundException("Game not found");

            game.RebuyValue = dto.RebuyValue;
            game.BountyValue = dto.BountyValue;

            await _context.SaveChangesAsync();
        }

    }
}
