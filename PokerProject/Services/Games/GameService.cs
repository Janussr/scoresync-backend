using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using PokerProject.Data;
using PokerProject.DTOs;
using PokerProject.Hubs;
using PokerProject.Models;
using System.Security.Claims;

namespace PokerProject.Services.Games
{
    public class GameService : IGameService
    {
        private readonly PokerDbContext _context;
        private readonly IHubContext<GameHub> _hubContext;

        public GameService(PokerDbContext context, IHubContext<GameHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        public async Task<GameDto> StartGameAsync(ClaimsPrincipal currentUser)
        {
            if (currentUser == null)
                throw new ArgumentNullException(nameof(currentUser));

            int userId = currentUser.GetUserId();

            var game = new Game
            {
                GameNumber = await GetNextGameNumber(),
                StartedAt = DateTime.UtcNow,
                IsFinished = false,
                Type = Game.GameType.Poker, 
                GamemasterId = userId 
            };

            _context.Games.Add(game);
            await _context.SaveChangesAsync();

            var firstRound = new Round
            {
                GameId = game.Id,
                RoundNumber = 1,
                StartedAt = DateTime.UtcNow
            };

            _context.Rounds.Add(firstRound);
            await _context.SaveChangesAsync();

            return new GameDto
            {
                Id = game.Id,
                GameNumber = game.GameNumber,
                StartedAt = game.StartedAt,
                IsFinished = game.IsFinished,
                Type = game.Type,
                Players = new List<PlayerDto>(), 
                Rounds = new List<RoundDto>
        {
            new RoundDto
            {
                Id = firstRound.Id,
                RoundNumber = firstRound.RoundNumber,
                StartedAt = firstRound.StartedAt
            }
        }
            };
        }


        public async Task<PlayerDto> JoinGameAsPlayerAsync(int gameId, int userId)
        {
            var game = await _context.Games
                .Include(g => g.Players)
                .FirstOrDefaultAsync(g => g.Id == gameId);

            if (game == null)
                throw new KeyNotFoundException("Game not found");

            if (game.IsFinished)
                throw new InvalidOperationException("Game is finished");

            var existing = game.Players.FirstOrDefault(p => p.UserId == userId);

            if (existing != null)
            {
                if (!existing.IsActive)
                {
                    existing.IsActive = true;
                    existing.LeftAt = null;
                    await _context.SaveChangesAsync();
                }

                var existingUser = await _context.Users.FindAsync(userId);

                return new PlayerDto
                {
                    UserId = existing.UserId,
                    Username = existingUser?.Username ?? "Unknown",
                    IsActive = existing.IsActive,
                    RebuyCount = existing.RebuyCount,
                    ActiveBounties = existing.ActiveBounties
                };
            }

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                throw new KeyNotFoundException("User not found");

            var newPlayer = new Player
            {
                GameId = gameId,
                UserId = userId,
                IsActive = true,
                RebuyCount = 0,
                ActiveBounties = 0
            };

            game.Players.Add(newPlayer);
            await _context.SaveChangesAsync();

            return new PlayerDto
            {
                UserId = newPlayer.UserId,
                Username = user.Username,
                IsActive = newPlayer.IsActive,
                RebuyCount = newPlayer.RebuyCount,
                ActiveBounties = newPlayer.ActiveBounties
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
                .Include(g => g.Rounds)
                    .ThenInclude(r => r.Scores)
                .Include(g => g.Players)
                    .ThenInclude(p => p.User)
                .FirstOrDefaultAsync(g => g.Id == gameId);

            if (game == null)
                throw new KeyNotFoundException("Game not found");

            if (game.IsFinished)
                throw new InvalidOperationException("Game already finished");

            // Afslut aktiv runde, hvis den findes
            var activeRound = game.Rounds.FirstOrDefault(r => r.EndedAt == null);
            if (activeRound != null)
                activeRound.EndedAt = DateTime.UtcNow;

            var allScores = game.Rounds.SelectMany(r => r.Scores).ToList();
            if (!allScores.Any())
                throw new InvalidOperationException("No scores registered");

            var activePlayerIds = game.Players
    .Where(p => p.IsActive)
    .Select(p => p.Id)
    .ToList();


            // Summer scores pr. Player.Id
            var totals = allScores
    .Where(s => activePlayerIds.Contains(s.PlayerId))
    .GroupBy(s => s.PlayerId)
    .Select(g => new
    {
        PlayerId = g.Key,
        TotalScore = g.Sum(s => s.Value)
    })
    .OrderByDescending(x => x.TotalScore)
    .ToList();

            if (!totals.Any())
                throw new InvalidOperationException("No active players to determine winner");

            var winnerData = totals.First();

            // Find vinderen i Player-listen baseret på Player.Id
            var winnerPlayer = game.Players.FirstOrDefault(p => p.Id == winnerData.PlayerId);
            if (winnerPlayer == null)
                throw new InvalidOperationException("Winner player not found in game");

            // Sæt winner i Game
            game.WinnerPlayerId = winnerPlayer.Id;
            game.WinnerPlayer = winnerPlayer;

            // Tilføj til HallOfFame
            var hallOfFame = new HallOfFame
            {
                GameId = game.Id,
                PlayerId = winnerPlayer.Id,
                WinDate = DateTime.UtcNow
            };
            _context.HallOfFames.Add(hallOfFame);

            // Afslut spillet
            game.IsFinished = true;
            game.EndedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // SIGNALR notification
            await _hubContext.Clients
                .Group($"Game-{game.Id}")
                .SendAsync("GameFinished", game.Id);

            // Returnér GameDto inkl. vinderen
            return new GameDto
            {
                Id = game.Id,
                GameNumber = game.GameNumber,
                StartedAt = game.StartedAt,
                EndedAt = game.EndedAt,
                IsFinished = game.IsFinished,
                Winner = new WinnerDto
                {
                    PlayerId = winnerPlayer.Id,
                    UserName = winnerPlayer.User.Username,
                    WinningScore = winnerData.TotalScore,
                    WinDate = game.EndedAt.Value
                }
            };
        }

        public async Task<GameDto> CancelGameAsync(int gameId)
        {
            var game = await _context.Games
                .Include(g => g.Rounds)
    .ThenInclude(r => r.Scores)
                .Include(g => g.Players)
                .FirstOrDefaultAsync(g => g.Id == gameId);

            if (game == null)
                throw new KeyNotFoundException("Game not found");

            if (game.IsFinished)
                throw new InvalidOperationException("Game already finished");

            if (game.Rounds.Any(r => r.Scores.Any()))
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


        public async Task<GamePanelDto?> GetActiveGameForGamePanelAsync(int userId)
        {
            var game = await _context.Games
                .Where(g => !g.IsFinished && g.GamemasterId == userId)
                .Include(g => g.Players)
                    .ThenInclude(p => p.User)
                .Include(g => g.Rounds)
                    .ThenInclude(r => r.Scores)
                        .ThenInclude(s => s.Player)
                            .ThenInclude(p => p.User)
                .OrderByDescending(g => g.StartedAt)
                .FirstOrDefaultAsync();

            if (game == null) return null;

            var activePlayers = game.Players.Where(p => p.IsActive).ToList();

            return new GamePanelDto
            {
                Id = game.Id,
                GameNumber = game.GameNumber,
                StartedAt = game.StartedAt,
                IsFinished = game.IsFinished,
                Type = game.Type,
                RebuyValue = game.RebuyValue,
                BountyValue = game.BountyValue,
                Players = game.Players.Select(p => new PlayerDto
                {
                    PlayerId = p.Id,
                    UserId = p.UserId,
                    Username = p.User.Username,
                    RebuyCount = p.RebuyCount,
                    ActiveBounties = p.ActiveBounties,
                    IsActive = p.IsActive
                }).ToList(),
                Rounds = game.Rounds.Select(r => new RoundDto
                {
                    Id = r.Id,
                    RoundNumber = r.RoundNumber,
                    StartedAt = r.StartedAt,
                    Scores = r.Scores.Select(s => new ScoreDto
                    {
                        Id = s.Id,
                        PlayerId = s.Player.UserId,
                        UserName = s.Player.User.Username,
                        Points = s.Value,
                        Type = s.Type
                    }).ToList()
                }).ToList()
            };
        }

        public async Task<GameDetailsDto?> GetActiveGameForPlayerAsync(int userId)
        {
            var game = await _context.Games
                .Include(g => g.Rounds)
                    .ThenInclude(r => r.Scores)
                        .ThenInclude(s => s.Player)
                            .ThenInclude(p => p.User)
                .Include(g => g.WinnerPlayer)
                    .ThenInclude(w => w.User)
                .Include(g => g.Players)
                    .ThenInclude(p => p.User)
                .Where(g => !g.IsFinished && g.Players.Any(p => p.UserId == userId && p.IsActive))
                .FirstOrDefaultAsync();

            if (game == null) return null;

            // --- Scores grouped per Player ---
            var scores = game.Rounds
                .SelectMany(r => r.Scores)
                .GroupBy(s => new { s.PlayerId, s.Player.User.Username })
                .Select(g => new GameScoreboardDto
                {
                    PlayerId = g.First().PlayerId,
                    UserName = g.Key.Username,
                    TotalPoints = g.Sum(s => s.Value)
                })
                .ToList();

            // --- Players ---
            var players = game.Players.Select(p => new PlayerDto
            {
                PlayerId = p.Id,
                UserId = p.UserId,
                Username = p.User.Username,
                RebuyCount = p.RebuyCount,
                ActiveBounties = p.ActiveBounties,
                IsActive = p.IsActive
            }).ToList();

            // --- Winner ---
            WinnerDto? winnerDto = null;
            if (game.WinnerPlayer != null)
            {
                var winningScore = game.Rounds
                    .SelectMany(r => r.Scores)
                    .Where(s => s.PlayerId == game.WinnerPlayerId)
                    .Sum(s => s.Value);

                winnerDto = new WinnerDto
                {
                    PlayerId = game.WinnerPlayer.Id,
                    UserName = game.WinnerPlayer.User.Username,
                    WinningScore = winningScore,
                    WinDate = game.EndedAt ?? DateTime.UtcNow
                };
            }

            return new GameDetailsDto
            {
                Id = game.Id,
                GameNumber = game.GameNumber,
                StartedAt = game.StartedAt,
                EndedAt = game.EndedAt,
                IsFinished = game.IsFinished,
                RebuyValue = game.RebuyValue,       
                BountyValue = game.BountyValue,
                Scores = scores,
                Players = players,
                Winner = winnerDto,
                Rounds = game.Rounds.Select(r => new RoundDto
                {
                    Id = r.Id,
                    RoundNumber = r.RoundNumber,
                    StartedAt = r.StartedAt,
                    EndedAt = r.EndedAt,
                    Scores = r.Scores.Select(s => new ScoreDto
                    {
                        Id = s.Id,
                        PlayerId = s.PlayerId,
                        UserName = s.Player.User.Username,
                        Points = s.Value,
                        GameId = game.Id,
                        Type = s.Type
                    }).ToList()
                }).ToList()
            };
        }


        public async Task<List<GameDto>> GetActiveGamesAsync()
        {
            return await _context.Games
                .Where(g => !g.IsFinished)

                .Include(g => g.Players)
                    .ThenInclude(p => p.User)

                .Include(g => g.Rounds)
                    .ThenInclude(r => r.Scores)
                        .ThenInclude(s => s.Player)
                            .ThenInclude(p => p.User)

                .Include(g => g.WinnerPlayer)
                    .ThenInclude(w => w.User)

                .Select(g => new GameDto
                {
                    Id = g.Id,
                    GameNumber = g.GameNumber,
                    StartedAt = g.StartedAt,
                    IsFinished = g.IsFinished,
                    Type = g.Type,
                    RebuyValue = g.RebuyValue,
                    BountyValue = g.BountyValue,

                    Players = g.Players.Select(p => new PlayerDto
                    {
                        UserId = p.UserId,
                        Username = p.User.Username,
                        ActiveBounties = p.ActiveBounties,
                        RebuyCount = p.RebuyCount
                    }).ToList(),

                    Scores = g.Rounds
                        .SelectMany(r => r.Scores)
                        .Select(s => new ScoreDto
                        {
                            Id = s.Id,
                            PlayerId = s.Player.UserId,
                            UserName = s.Player.User.Username,
                            Points = s.Value,
                            Type = s.Type
                        }).ToList(),

                    Rounds = g.Rounds.Select(r => new RoundDto
                    {
                        Id = r.Id,
                        RoundNumber = r.RoundNumber,
                        StartedAt = r.StartedAt,
                        EndedAt = r.EndedAt,

                        Scores = r.Scores.Select(s => new ScoreDto
                        {
                            Id = s.Id,
                            PlayerId = s.Player.UserId,
                            UserName = s.Player.User.Username,
                            Points = s.Value,
                            Type = s.Type
                        }).ToList()
                    }).ToList(),

                    Winner = g.WinnerPlayer == null ? null : new WinnerDto
                    {
                        PlayerId = g.WinnerPlayer.UserId,
                        UserName = g.WinnerPlayer.User.Username,
                        WinningScore = g.Rounds
                            .SelectMany(r => r.Scores)
                            .Where(s => s.PlayerId == g.WinnerPlayerId)
                            .Sum(s => s.Value),
                        WinDate = g.EndedAt ?? DateTime.UtcNow
                    }
                })
                .ToListAsync();
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

                    Players = g.Players.Select(p => new PlayerDto
                    {
                        UserId = p.UserId,
                        Username = p.User.Username,
                        RebuyCount = p.RebuyCount,
                        ActiveBounties = p.ActiveBounties,
                    }).ToList(),

                    Scores = g.Rounds
                        .SelectMany(r => r.Scores)
                        .Select(s => new ScoreDto
                        {
                            Id = s.Id,
                            PlayerId = s.Player.UserId,
                            UserName = s.Player.User.Username,
                            Points = s.Value,
                            Type = s.Type
                        }).ToList(),

                    Winner = g.WinnerPlayer == null ? null : new WinnerDto
                    {
                        PlayerId = g.WinnerPlayer.UserId,
                        UserName = g.WinnerPlayer.User.Username,
                        WinningScore = g.Rounds
        .SelectMany(r => r.Scores)
        .Where(s => s.PlayerId == g.WinnerPlayerId)
        .Sum(s => s.Value),
                        WinDate = g.EndedAt ?? DateTime.UtcNow
                    }
                })
                .ToListAsync();

            return games;
        }

        public async Task<GameDto?> GetGameByIdAsync(int id)
        {
            var game = await _context.Games
                .Include(g => g.Rounds)
    .ThenInclude(r => r.Scores)
                .FirstOrDefaultAsync(g => g.Id == id);

            if (game == null) return null;

            return new GameDto
            {
                Id = game.Id,
                GameNumber = game.GameNumber,
                StartedAt = game.StartedAt,
                EndedAt = game.EndedAt,
                IsFinished = game.IsFinished,
                Scores = game.Rounds
    .SelectMany(r => r.Scores)
    .Select(s => new ScoreDto
    {
        PlayerId = s.PlayerId,
        Points = s.Value
    }).ToList()
            };
        }

        public async Task<GameDetailsDto?> GetGameDetailsAsync(int gameId, string? role)
        {
            var game = await _context.Games
                .Include(g => g.Rounds)
                    .ThenInclude(r => r.Scores)
                        .ThenInclude(s => s.Player)
                            .ThenInclude(p => p.User)
                .Include(g => g.WinnerPlayer)
                    .ThenInclude(w => w.User)
                .Include(g => g.Players)
                    .ThenInclude(p => p.User)
                .FirstOrDefaultAsync(g => g.Id == gameId);

            if (game == null) return null;

            if (!game.IsFinished && role != "Admin" && role != "Gamemaster")
                throw new UnauthorizedAccessException();

            // --- Players ---
            var players = game.Players.Select(p => new PlayerDto
            {
                PlayerId = p.Id,
                UserId = p.UserId,
                Username = p.User.Username,
                RebuyCount = p.RebuyCount,
                ActiveBounties = p.ActiveBounties,
                IsActive = p.IsActive
            }).ToList();

            // --- Scores (grouped per player) ---
            var scores = game.Rounds
                .SelectMany(r => r.Scores)
                .GroupBy(s => new { s.PlayerId, s.Player.User.Username })
                .Select(g => new GameScoreboardDto
                {
                    PlayerId = g.First().PlayerId,
                    UserName = g.Key.Username,
                    TotalPoints = g.Sum(s => s.Value)
                }).ToList();

            // --- Rounds ---
            var rounds = game.Rounds.Select(r => new RoundDto
            {
                Id = r.Id,
                RoundNumber = r.RoundNumber,
                StartedAt = r.StartedAt,
                EndedAt = r.EndedAt,
                Scores = r.Scores.Select(s => new ScoreDto
                {
                    Id = s.Id,
                    PlayerId = s.PlayerId,
                    UserName = s.Player.User.Username,
                    Points = s.Value,
                    Type = s.Type,
                    GameId = game.Id
                }).ToList()
            }).ToList();

            // --- Winner ---
            WinnerDto? winnerDto = null;
            if (game.WinnerPlayer != null)
            {
                var winningScore = game.Rounds
                    .SelectMany(r => r.Scores)
                    .Where(s => s.PlayerId == game.WinnerPlayerId)
                    .Sum(s => s.Value);

                winnerDto = new WinnerDto
                {
                    PlayerId = game.WinnerPlayer.Id,
                    UserName = game.WinnerPlayer.User.Username,
                    WinningScore = winningScore,
                    WinDate = game.EndedAt ?? DateTime.UtcNow
                };
            }

            return new GameDetailsDto
            {
                Id = game.Id,
                GameNumber = game.GameNumber,
                StartedAt = game.StartedAt,
                EndedAt = game.EndedAt,
                IsFinished = game.IsFinished,
                Players = players,
                Scores = scores,
                Rounds = rounds,
                Winner = winnerDto,
                RebuyValue = game.RebuyValue,
                BountyValue = game.BountyValue
            };
        }

        public async Task RemoveGameAsync(int gameId)
        {
            // Hent spillet inkl. Players og Rounds
            var game = await _context.Games
                .Include(g => g.Players)
                .Include(g => g.Rounds)
                    .ThenInclude(r => r.Scores)
                .FirstOrDefaultAsync(g => g.Id == gameId);

            if (game == null)
                throw new KeyNotFoundException("Game not found");

            // 1️⃣ Slet HallOfFame entries for spillere i spillet
            var playerIds = game.Players.Select(p => p.Id).ToList();
            // 1️⃣ Sæt WinnerPlayerId = null, hvis vinderen er i spillet
            if (game.WinnerPlayerId.HasValue && playerIds.Contains(game.WinnerPlayerId.Value))
            {
                game.WinnerPlayerId = null;
                await _context.SaveChangesAsync(); // <--- Gem her før sletning
            }

            // 2️⃣ Slet HallOfFame
            var hallOfFameEntries = await _context.HallOfFames
                .Where(h => playerIds.Contains(h.PlayerId))
                .ToListAsync();
            _context.HallOfFames.RemoveRange(hallOfFameEntries);
            await _context.SaveChangesAsync(); // valgfrit, men kan gøres

            // 3️⃣ Slet Scores
            var scoresToDelete = await _context.Scores
                .Where(s => playerIds.Contains(s.PlayerId) ||
                            (s.VictimPlayerId.HasValue && playerIds.Contains(s.VictimPlayerId.Value)))
                .ToListAsync();
            _context.Scores.RemoveRange(scoresToDelete);
            await _context.SaveChangesAsync();

            // 4️⃣ Slet selve spillet (Players og Rounds slettes via cascade)
            _context.Games.Remove(game);
            await _context.SaveChangesAsync();
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

       


        public async Task LeaveGameAsync(int gameId, int userId)
        {
            var player = await _context.Players
                .FirstOrDefaultAsync(p => p.GameId == gameId && p.UserId == userId);

            if (player == null)
                throw new KeyNotFoundException("Player not found");

            if (!player.IsActive)
                throw new InvalidOperationException("Player already inactive");

            player.IsActive = false;
            player.LeftAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
        }


        public async Task<List<GameListDto>> GetActiveGamesLobbyListAsync()
        {
            return await _context.Games
                .Where(g => !g.IsFinished)
                .Select(g => new GameListDto
                {
                    Id = g.Id,
                    GameNumber = g.GameNumber,
                    Type = g.Type,
                    StartedAt = g.StartedAt,
                    IsFinished = g.IsFinished,

                    PlayerCount = g.Players.Count(p => p.IsActive)
                })
                .OrderByDescending(g => g.StartedAt)
                .ToListAsync();
        }
    }
}
