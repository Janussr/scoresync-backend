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

        [Fact]
        public async Task AddScore_ShouldAddScore()
        {
            var context = GetDbContext();
            var service = new ScoreService(context);

            var game = new Game { GameNumber = 1, StartedAt = DateTime.UtcNow };
            context.Games.Add(game);
            await context.SaveChangesAsync();

            var round = new Round
            {
                GameId = game.Id,
                RoundNumber = 1,
                StartedAt = DateTime.UtcNow
            };
            context.Rounds.Add(round);
            await context.SaveChangesAsync();

            var score = await service.AddScoreAsync(game.Id, 1, 100);

            score.Points.Should().Be(100);
            var scoreInDb = await context.Scores.FirstOrDefaultAsync();
            scoreInDb.Should().NotBeNull();
            scoreInDb.RoundId.Should().Be(round.Id); 
        }

        [Fact]
        public async Task AddScore_ShouldThrow_WhenGameDoesNotExist()
        {
            var context = GetDbContext();
            var service = new ScoreService(context);

            Func<Task> act = async () => await service.AddScoreAsync(999, 1, 100);

            await act.Should().ThrowAsync<KeyNotFoundException>();
        }


        [Fact]
        public async Task RegisterRebuy_ShouldFail_WhenNoRebuyValue()
        {
            var context = GetDbContext();
            var service = new ScoreService(context);

            var game = new Game { GameNumber = 1, StartedAt = DateTime.UtcNow };
            context.Games.Add(game);
            await context.SaveChangesAsync();

            Func<Task> act = async () => await service.RegisterRebuyForAdminAsync(game.Id, 1, 2, true);

            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("Rebuy value not set by admin");
        }
         
    }
}
