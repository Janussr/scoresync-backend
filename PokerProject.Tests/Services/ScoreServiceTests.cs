using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using PokerProject.Data;
using PokerProject.Models;
using PokerProject.Services.Scores;

namespace PokerProject.Tests.Services
{
    public class ScoreServiceTests
    {
        private PokerDbContext GetDbContext()
        {
            var options = new DbContextOptionsBuilder<PokerDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            return new PokerDbContext(options);
        }

        private ScoreService CreateService(PokerDbContext context)
        {
            return new ScoreService(context);
        }

        // -------------------------------
        // AddScoreAsync
        // -------------------------------

        [Fact]
        public async Task AddScoreAsync_ShouldThrow_WhenGameDoesNotExist()
        {
            var context = GetDbContext();
            var service = CreateService(context);

            Func<Task> act = async () =>
                await service.AddScoreAsync(999, 1, 100);

            await act.Should().ThrowAsync<KeyNotFoundException>();
        }


        [Fact]
        public async Task AddScoreAsync_ShouldThrow_WhenPlayerNotInGame()
        {
            var context = GetDbContext();
            var service = CreateService(context);

            var game = new Game { GameNumber = 1, StartedAt = DateTime.UtcNow };
            context.Games.Add(game);

            var round = new Round
            {
                GameId = game.Id,
                RoundNumber = 1,
                StartedAt = DateTime.UtcNow
            };
            context.Rounds.Add(round);

            await context.SaveChangesAsync();

            Func<Task> act = async () =>
                await service.AddScoreAsync(game.Id, 999, 100);

            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        // -------------------------------
        // RemoveScoreAsync
        // -------------------------------

        [Fact]
        public async Task RemoveScoreAsync_ShouldSetScoreToZero()
        {
            var context = GetDbContext();
            var service = CreateService(context);

            var game = new Game { GameNumber = 1, StartedAt = DateTime.UtcNow };
            context.Games.Add(game);
            await context.SaveChangesAsync();

            var round = new Round
            {
                GameId = game.Id,
                RoundNumber = 1
            };
            context.Rounds.Add(round);
            await context.SaveChangesAsync();

            var score = new Score
            {
                RoundId = round.Id,
                PlayerId = 1,
                Value = 100
            };
            context.Scores.Add(score);
            await context.SaveChangesAsync();

            // Act
            var result = await service.RemoveScoreAsync(score.Id);

            // Assert
            result.Points.Should().Be(0);

            var scoreInDb = await context.Scores.FirstAsync();
            scoreInDb.Value.Should().Be(0);
        }

        [Fact]
        public async Task RemoveScoreAsync_ShouldThrow_WhenGameFinished()
        {
            var context = GetDbContext();
            var service = CreateService(context);

            var game = new Game
            {
                GameNumber = 1,
                StartedAt = DateTime.UtcNow,
                IsFinished = true
            };
            context.Games.Add(game);
            await context.SaveChangesAsync();

            var round = new Round
            {
                GameId = game.Id,
                RoundNumber = 1
            };
            context.Rounds.Add(round);
            await context.SaveChangesAsync();

            var score = new Score
            {
                RoundId = round.Id,
                PlayerId = 1,
                Value = 100
            };
            context.Scores.Add(score);
            await context.SaveChangesAsync();

            Func<Task> act = async () =>
                await service.RemoveScoreAsync(score.Id);

            await act.Should().ThrowAsync<InvalidOperationException>();
        }
    }
}