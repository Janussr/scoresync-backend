using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using PokerProject.Data;
using PokerProject.DTOs;
using PokerProject.Hubs;
using PokerProject.Models;

namespace PokerProject.Services.Rounds
{
    public class RoundService : IRoundService
    {
        private readonly PokerDbContext _context;
        private readonly IHubContext<GameHub> _hubContext;
        public const string RoundStarted = "RoundStarted";
        public const string RoundEnded = "RoundEnded";

        public RoundService(PokerDbContext context, IHubContext<GameHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
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
                // Validating scores before ending the round
                var activePlayers = game.Players
                    .Where(p => p.IsActive)
                    .Select(p => p.UserId)
                    .ToList();

                var playersWithScore = currentRound.Scores
                    .Select(s => s.PlayerId)
                    .Distinct()
                    .ToList();

                var missingPlayers = activePlayers
                    .Except(playersWithScore)
                    .ToList();

                //if (missingPlayers.Any())
                //{
                //    throw new InvalidOperationException(
                //        $"Missing scores from users: {string.Join(",", missingPlayers)}"
                //    );
                //}

                // End round
                currentRound.EndedAt = DateTime.UtcNow;
            }

            // New round number
            var roundNumber = game.Rounds.Any()
                ? game.Rounds.Max(r => r.RoundNumber) + 1
                : 1;

            var newRound = new Round
            {
                GameId = gameId,
                RoundNumber = roundNumber,
                StartedAt = DateTime.UtcNow
            };

            _context.Rounds.Add(newRound);
            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            //  SignalR (use currentRound!)
            if (currentRound != null)
            {
                await _hubContext.Clients.Group(gameId.ToString())
                    .SendAsync(RoundEnded, new
                    {
                        currentRound.Id,
                        currentRound.RoundNumber,
                        currentRound.EndedAt
                    });
            }

            await _hubContext.Clients.Group(gameId.ToString())
                .SendAsync(RoundStarted, new
                {
                    newRound.Id,
                    newRound.RoundNumber,
                    newRound.StartedAt,
                    Scores = new List<object>()
                });

            return new RoundDto
            {
                Id = newRound.Id,
                RoundNumber = newRound.RoundNumber,
                StartedAt = newRound.StartedAt
            };
        }

        public async Task<RoundDto?> GetCurrentRoundAsync(int gameId)
        {
            var round = await _context.Rounds
                .FirstOrDefaultAsync(r => r.GameId == gameId && r.EndedAt == null);

            if (round == null) return null;

            return new RoundDto
            {
                Id = round.Id,
                RoundNumber = round.RoundNumber,
                StartedAt = round.StartedAt
            };
        }

        public async Task<RoundDto> EndRoundAsync(int roundId)
        {
            var round = await _context.Rounds
                .Include(r => r.Game)
                .FirstOrDefaultAsync(r => r.Id == roundId);

            if (round == null) throw new KeyNotFoundException("Round not found");

            if (round.Game.IsFinished)
                throw new InvalidOperationException("Cannot end round of finished game");

            if (round.EndedAt != null) throw new InvalidOperationException("Round already ended");

            round.EndedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            //SIGNALR
            await _hubContext.Clients.Group($"Game-{round.GameId}")
            .SendAsync("RoundEnded", new RoundDto
            {
                Id = round.Id,
                RoundNumber = round.RoundNumber,
                StartedAt = round.StartedAt,
                EndedAt = round.EndedAt
            });

            return new RoundDto
            {
                Id = round.Id,
                RoundNumber = round.RoundNumber,
                StartedAt = round.StartedAt,
                EndedAt = round.EndedAt
            };

        }
    }
}
