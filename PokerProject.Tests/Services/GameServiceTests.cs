using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using PokerProject.Data;
using PokerProject.Hubs;
using PokerProject.Models;
using PokerProject.Services.Games;
using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;

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

        private GameService CreateService(PokerDbContext context)
        {
            var hubMock = new Mock<IHubContext<GameHub>>();
            return new GameService(context, hubMock.Object);
        }

        private ClaimsPrincipal CreateUser(int userId)
        {
            return new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString())
            }));
        }

        // -------------------------------
        // StartGameAsync
        // -------------------------------

        [Fact]
        public async Task StartGameAsync_ShouldCreateGameWithFirstRound()
        {
            var context = GetDbContext();
            var service = CreateService(context);

            var user = CreateUser(1);

            var result = await service.StartGameAsync(user);

            result.Should().NotBeNull();
            result.GameNumber.Should().Be(1);
            result.Rounds.Should().HaveCount(1);

            var gameInDb = await context.Games.Include(g => g.Rounds).FirstAsync();
            gameInDb.Rounds.Should().HaveCount(1);
        }


        [Fact]
        public async Task JoinGameAsPlayerAsync_ShouldThrow_WhenGameNotFound()
        {
            var context = GetDbContext();
            var service = CreateService(context);

            Func<Task> act = async () =>
                await service.JoinGameAsPlayerAsync(999, 1);

            await act.Should().ThrowAsync<KeyNotFoundException>();
        }

        // -------------------------------
        // CancelGameAsync
        // -------------------------------

        [Fact]
        public async Task CancelGameAsync_ShouldDeleteGame_WhenNoScores()
        {
            var context = GetDbContext();
            var service = CreateService(context);

            var game = new Game
            {
                GameNumber = 1,
                StartedAt = DateTime.UtcNow
            };

            context.Games.Add(game);
            await context.SaveChangesAsync();

            var result = await service.CancelGameAsync(game.Id);

            result.IsFinished.Should().BeTrue();

            var exists = await context.Games.AnyAsync();
            exists.Should().BeFalse();
        }

        [Fact]
        public async Task CancelGameAsync_ShouldThrow_WhenGameHasScores()
        {
            var context = GetDbContext();
            var service = CreateService(context);

            var game = new Game
            {
                GameNumber = 1,
                StartedAt = DateTime.UtcNow
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

            context.Scores.Add(new Score
            {
                RoundId = round.Id,
                PlayerId = 1,
                Value = 100
            });

            await context.SaveChangesAsync();

            Func<Task> act = async () =>
                await service.CancelGameAsync(game.Id);

            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        // -------------------------------
        // GetGameByIdAsync
        // -------------------------------

        [Fact]
        public async Task GetGameByIdAsync_ShouldReturnGame()
        {
            var context = GetDbContext();
            var service = CreateService(context);

            var game = new Game
            {
                GameNumber = 1,
                StartedAt = DateTime.UtcNow
            };

            context.Games.Add(game);
            await context.SaveChangesAsync();

            var result = await service.GetGameByIdAsync(game.Id);

            result.Should().NotBeNull();
            result!.GameNumber.Should().Be(1);
        }
    }
}