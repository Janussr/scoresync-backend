//using FluentAssertions;
//using Microsoft.EntityFrameworkCore;
//using PokerProject.Data;
//using PokerProject.Models;
//using PokerProject.Services.Games;
//using PokerProject.Services.Scores;

//namespace PokerProject.Tests.Services
//{
//    public class GameServiceTests
//    {
//        private PokerDbContext GetDbContext()
//        {
//            var options = new DbContextOptionsBuilder<PokerDbContext>()
//                .UseInMemoryDatabase(Guid.NewGuid().ToString())
//                .Options;

//            return new PokerDbContext(options);
//        }

        

//        [Fact]
//        public async Task AddScore_ShouldThrow_WhenGameDoesNotExist()
//        {
//            var context = GetDbContext();
//            var scoreService = new ScoreService(context);

//            Func<Task> act = async () =>
//                await scoreService.AddScoreAsync(999, 1, 100);

//            await act.Should().ThrowAsync<KeyNotFoundException>();
//        }

//        [Fact]
//        public async Task AddScore_ShouldAddScore_WhenGameAndRoundExist()
//        {
//            // Arrange
//            var context = GetDbContext();
//            var scoreService = new ScoreService(context);

//            var game = new Game { GameNumber = 1, StartedAt = DateTime.UtcNow };
//            context.Games.Add(game);
//            await context.SaveChangesAsync();

//            // Opret en runde for at kunne tilføje score
//            var round = new Round
//            {
//                GameId = game.Id,
//                RoundNumber = 1,
//                StartedAt = DateTime.UtcNow
//            };
//            context.Rounds.Add(round);
//            await context.SaveChangesAsync();

//            // Act
//            var result = await scoreService.AddScoreAsync(game.Id, 1, 100);

//            // Assert
//            result.Should().NotBeNull();
//            result.Points.Should().Be(100);

//            var scoreInDb = await context.Scores.FirstOrDefaultAsync();
//            scoreInDb.Should().NotBeNull();
//            scoreInDb.RoundId.Should().Be(round.Id);
//            scoreInDb.PlayerId.Should().Be(1);
//            scoreInDb.Value.Should().Be(100);
//        }

        
//    }
//}