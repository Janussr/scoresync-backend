using Microsoft.EntityFrameworkCore;
using PokerProject.Data;
using PokerProject.DTOs;
using PokerProject.Models;

namespace PokerProject.Services.Games
{
    public class GameService : IGameService
    {
        private readonly PokerDbContext _context;

        public GameService(PokerDbContext context)
        {
            _context = context;
        }

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

        public async Task<GameDto> EndGameAsync(int gameId)
        {
            var game = await _context.Games
                .Include(g => g.Scores)
                .FirstOrDefaultAsync(g => g.Id == gameId);

            if (game == null)
                throw new KeyNotFoundException("Game not found");

            if (game.IsFinished)
                throw new KeyNotFoundException("Game already finished");

            if (!game.Scores.Any())
                throw new InvalidOperationException("No scores registered");

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

            game.IsFinished = true;
            game.EndedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

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
        public async Task<GameDto?> GetActiveGameAsync()
        {
            var game = await _context.Games
                .Where(g => !g.IsFinished)
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
                .FirstOrDefaultAsync(); 

            return game;
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

            //Admin and gamemaster can see active scoreboard but user cannot
            if (!game.IsFinished && role != "Admin" && role != "Gamemaster")
                throw new UnauthorizedAccessException();


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

        public async Task RemoveGameAsync(int gameId)
        {
            var game = await _context.Games
                .Include(g => g.Scores)
                .Include(g => g.Participants)
                .Include(g => g.Winner)
                .FirstOrDefaultAsync(g => g.Id == gameId);

            if (game == null)
                throw new KeyNotFoundException("Game not found");

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

        public async Task UpdateRulesAsync(int gameId, UpdateRulesDto dto)
        {
            var game = await _context.Games.FindAsync(gameId);
            if (game == null) 
                throw new KeyNotFoundException("Game not found");

            game.RebuyValue = dto.RebuyValue;
            game.BountyValue = dto.BountyValue;

            await _context.SaveChangesAsync();
        }

    }
}
