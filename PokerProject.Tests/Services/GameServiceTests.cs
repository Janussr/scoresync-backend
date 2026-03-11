using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using PokerProject.Data;
using PokerProject.Services.Games;
using PokerProject.Services.Scores;

namespace PokerProject.Tests.Services
{
    public class GameServiceTests
    {
        private PokerDbContext GetDbContext()
        {
            var options = new DbContextOptionsBuilder<PokerDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()) 
                .Options;

            return new PokerDbContext(options);
        }

        [Fact]
        public async Task StartGame_ShouldCreateGame()
        {
            // Arrange
            var context = GetDbContext();
            var service = new GameService(context);

            // Act
            var result = await service.StartGameAsync();

            // Assert
            result.Should().NotBeNull();
            result.GameNumber.Should().Be(1);

            var gameInDb = await context.Games.FirstOrDefaultAsync();
            gameInDb.Should().NotBeNull();
        }

        [Fact]
        public async Task AddScore_ShouldThrow_WhenGameDoesNotExist()
        {
            var context = GetDbContext();
            var service = new ScoreService(context);

            Func<Task> act = async () =>
                await service.AddScoreAsync(999, 1, 100);

            await act.Should().ThrowAsync<KeyNotFoundException>();
        }

        [Fact]
        public async Task AddScore_ShouldAddScore_WhenGameExists()
        {
            // Arrange
            var context = GetDbContext();
            var service = new ScoreService(context);

            var game = new Game
            {
                GameNumber = 1,
                StartedAt = DateTime.UtcNow
            };
            context.Games.Add(game);
            await context.SaveChangesAsync(); 

            // Act
            var result = await service.AddScoreAsync(game.Id, 1, 100);

            // Assert
            result.Should().NotBeNull();
            result.Points.Should().Be(100);

            var scoreInDb = await context.Scores.FirstOrDefaultAsync();
            scoreInDb.Should().NotBeNull();
            scoreInDb.GameId.Should().Be(game.Id);
            scoreInDb.UserId.Should().Be(1);
            scoreInDb.Points.Should().Be(100);
        }

        [Fact]
        public async Task EndGame_ShouldSetWinner()
        {
            var context = GetDbContext();
            var service = new GameService(context);

            var game = new Game { GameNumber = 1, StartedAt = DateTime.UtcNow };
            context.Games.Add(game);
            await context.SaveChangesAsync();

            context.Scores.Add(new Score { GameId = game.Id, UserId = 1, Points = 100 });
            context.Scores.Add(new Score { GameId = game.Id, UserId = 2, Points = 50 });
            await context.SaveChangesAsync();

            var result = await service.EndGameAsync(game.Id);

            result.IsFinished.Should().BeTrue();
            var winner = await context.HallOfFames.FirstOrDefaultAsync();
            winner.UserId.Should().Be(1);
        }

    }
}