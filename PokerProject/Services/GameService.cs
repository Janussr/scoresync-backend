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
        Task<GameDto?> GetActiveGameAsync();
        Task<List<GameDto>> GetAllGamesAsync();
        Task<GameDto?> GetGameByIdAsync(int gameId);
        Task<GameDetailsDto?> GetGameDetailsAsync(int gameId, string? role);
        Task AddParticipantsAsync(int gameId, List<int> userIds);
        Task<List<ParticipantDto>> GetParticipantsAsync(int gameId);
        Task<bool> IsUserParticipantAsync(int gameId, int userId);
        Task<List<ParticipantDto>> RemoveParticipantAsync(int gameId, int userId);
        Task<PlayerScoreDetailsDto> GetPlayerScoreEntries(int gameId, int userId);
        Task RegisterKnockoutAsync(int gameId, int killerUserId, int victimUserId, bool isAdmin);
        //Task<ScoreDto> RegisterRebuyAsync(int gameId, int userId, bool isAdmin);
        Task<ScoreDto> RegisterRebuyAsync(int gameId, int actorUserId, int targetUserId, bool isAdmin);
        Task UpdateRulesAsync(int gameId, UpdateRulesDto dto);
        Task<List<BountyLeaderboardDto>> GetBountyLeaderboardAsync();
    }


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



        public async Task<ScoreDto> AddScoreAsync(int gameId, int userId, int points)
        {
            var game = await _context.Games.FindAsync(gameId);
            if (game == null)
                throw new KeyNotFoundException("Game not found");

            if (game.IsFinished)
                throw new InvalidOperationException("Game has ended – Cant add points.");

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
                throw new KeyNotFoundException("Game not found");

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
                throw new KeyNotFoundException("Score not found");

            var game = await _context.Games.FindAsync(score.GameId);
            if (game == null)
                throw new KeyNotFoundException("Game not found");

            if (game.IsFinished)
                throw new InvalidOperationException("Game has ended - can't remove points.");

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

            //Admin can see scoreboard but normal user cant
            if (!game.IsFinished && role != "Admin")
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

            var game = await _context.Games.FindAsync(gameId);
            if (game == null)
                throw new KeyNotFoundException("Game not found");

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


        private async Task HandleKnockoutAsync(int gameId, int killerUserId, int victimUserId, bool isAdmin)
        {
            var game = await _context.Games
                .Include(g => g.Scores) 
                .FirstOrDefaultAsync(g => g.Id == gameId);

            if (game == null)
                throw new KeyNotFoundException("Game not found");

            if (!game.BountyValue.HasValue)
                throw new InvalidOperationException("Bounty value not set for this game");

            var participants = await _context.GameParticipants
                .Where(p => p.GameId == gameId && (p.UserId == killerUserId || p.UserId == victimUserId))
                .ToListAsync();

            if (participants.Count != 2)
            {
                if (!participants.Any(p => p.UserId == killerUserId))
                    throw new InvalidOperationException("Killer not found in game");

                if (!participants.Any(p => p.UserId == victimUserId))
                    throw new InvalidOperationException("Victim not found in game");
            }

            var killer = participants.First(p => p.UserId == killerUserId);
            var victim = participants.First(p => p.UserId == victimUserId);

            if (killer.UserId == victim.UserId)
                throw new InvalidOperationException("Cannot knock yourself out");

            var bountyValue = game.BountyValue.Value;
            var points = victim.ActiveBounties > 0
                ? victim.ActiveBounties * bountyValue
                : 0;

            game.Scores.Add(new Score
            {
                GameId = game.Id,
                UserId = killerUserId,
                Points = points,
                Type = Score.ScoreType.Bounty,
                VictimUserId = victimUserId,
                CreatedAt = DateTime.UtcNow
            });

            victim.ActiveBounties = 0;
            killer.ActiveBounties += 1;

            await _context.SaveChangesAsync();
        }

        public async Task RegisterKnockoutAsync(int gameId, int killerUserId, int victimUserId, bool isAdmin)
        {
            await HandleKnockoutAsync(gameId, killerUserId, victimUserId, isAdmin);
        }


        //public async Task<ScoreDto> RegisterRebuyAsync(int gameId, int userId, bool isAdmin)
        //{
        //    var game = await _context.Games
        //        .Include(g => g.Participants)
        //        .Include(g => g.Scores)
        //        .FirstOrDefaultAsync(g => g.Id == gameId);

        //    if (game == null)
        //        throw new KeyNotFoundException("Game not found");

        //    if (game.RebuyValue == null)
        //        throw new InvalidOperationException("Rebuy value not set by admin");

        //    var participant = game.Participants.FirstOrDefault(p => p.UserId == userId);
        //    if (participant == null)
        //        if(!isAdmin)
        //            throw new UnauthorizedAccessException("User not participant in game");

        //    participant.RebuyCount++;

        //    var score = new Score
        //    {
        //        GameId = gameId,
        //        UserId = userId,
        //        Points = -game.RebuyValue.Value,
        //        CreatedAt = DateTime.UtcNow,
        //        Type = Score.ScoreType.Rebuy,
        //    };

        //    _context.Scores.Add(score);
        //    await _context.SaveChangesAsync();

        //    return new ScoreDto
        //    {
        //        UserId = userId,
        //        Points = score.Points,
        //        Type = score.Type
        //    };
        //}

        public async Task<ScoreDto> RegisterRebuyAsync(int gameId, int actorUserId, int targetUserId, bool isAdmin)
        {
            var game = await _context.Games
                .Include(g => g.Participants)
                .FirstOrDefaultAsync(g => g.Id == gameId);

            if (game == null)
                throw new KeyNotFoundException("Game not found");

            if (game.RebuyValue == null)
                throw new InvalidOperationException("Rebuy value not set by admin");

            if (!isAdmin && actorUserId != targetUserId)
                throw new UnauthorizedAccessException("Players can only rebuy themselves");

            var participant = game.Participants.FirstOrDefault(p => p.UserId == targetUserId);

            if (participant == null)
                throw new InvalidOperationException("Target user is not participant in game");

            participant.RebuyCount++;

            var score = new Score
            {
                GameId = gameId,
                UserId = targetUserId,
                Points = -game.RebuyValue.Value,
                CreatedAt = DateTime.UtcNow,
                Type = Score.ScoreType.Rebuy,
            };

            _context.Scores.Add(score);
            await _context.SaveChangesAsync();

            return new ScoreDto
            {
                UserId = targetUserId,
                Points = score.Points,
                Type = score.Type
            };
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

        public async Task<List<BountyLeaderboardDto>> GetBountyLeaderboardAsync()
        {
            var knockoutsQuery = _context.Scores
                .Where(s => s.Type == Score.ScoreType.Bounty)
                .GroupBy(s => s.UserId)
                .Select(g => new
                {
                    UserId = g.Key,
                    Knockouts = g.Count(),
                    TotalBountyPoints = g.Sum(s => s.Points)
                });

            var timesKnockedOutQuery = _context.Scores
                .Where(s => s.Type == Score.ScoreType.Bounty && s.VictimUserId.HasValue)
                .GroupBy(s => s.VictimUserId.Value)
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
