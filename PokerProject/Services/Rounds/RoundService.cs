using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using PokerProject.Data;
using PokerProject.DTOs.Rounds;
using PokerProject.DTOs.Scores;
using PokerProject.Hubs;
using PokerProject.Hubs.GameNotifier;
using PokerProject.Models;

namespace PokerProject.Services.Rounds
{
    public class RoundService : IRoundService
    {
        private readonly PokerDbContext _context;
        private readonly IHubContext<GameHub> _hubContext;
        private readonly IGameNotifier _gameNotifier;
        public const string RoundStarted = "RoundStarted";
        public const string RoundEnded = "RoundEnded";

        public RoundService(PokerDbContext context, IHubContext<GameHub> hubContext, IGameNotifier gameNotifier)
        {
            _context = context;
            _hubContext = hubContext;
            _gameNotifier = gameNotifier;
        }

        public async Task<RoundDto> StartNewRoundAsync(int gameId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            var game = await _context.Games
                .Include(g => g.Players)
                .Include(g => g.Rounds)
                    .ThenInclude(r => r.Scores)
                .FirstOrDefaultAsync(g => g.Id == gameId);

            if (game == null)
                throw new KeyNotFoundException("Game not found");

            if (game.IsFinished)
                throw new InvalidOperationException("Game is finished");

            var currentRound = game.Rounds.FirstOrDefault(r => r.EndedAt == null);

            if (currentRound != null)
            {
                currentRound.EndedAt = DateTimeOffset.UtcNow;
            }

            var newRound = new Round
            {
                GameId = gameId,
                RoundNumber = game.Rounds.Any() ? game.Rounds.Max(r => r.RoundNumber) + 1 : 1,
                StartedAt = DateTimeOffset.UtcNow
            };

            _context.Rounds.Add(newRound);
            await _context.SaveChangesAsync();


            if (currentRound != null)
            {
                var endedDto = new RoundDto
                {
                    Id = currentRound.Id,
                    RoundNumber = currentRound.RoundNumber,
                    EndedAt = currentRound.EndedAt,
                    Scores = currentRound.Scores.Select(s => new ScoreDto
                    {
                        Id = s.Id,
                        PlayerId = s.PlayerId,
                        Points = s.Value,
                        Type = s.Type
                    }).ToList()
                };

            }

            var newDto = new RoundDto
            {
                Id = newRound.Id,
                RoundNumber = newRound.RoundNumber,
                StartedAt = newRound.StartedAt,
                Scores = new List<ScoreDto>()
            };

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            // Send RoundStarted to clients
            await _gameNotifier.StartNewRound(gameId, newDto);


            return newDto;


        }
    }
}
