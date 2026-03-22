using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using PokerProject.Data;
using PokerProject.Models;
using PokerProject.Services.Players;

namespace PokerProject.Tests.Services
{
    public class PlayerServiceTests
    {

        private PokerDbContext GetDbContext()
        {
            var options = new DbContextOptionsBuilder<PokerDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            return new PokerDbContext(options);
        }


        [Fact]
        public async Task AddParticipants_ShouldAddNewParticipants()
        {
            var context = GetDbContext();
            var service = new PlayerService(context);

            var game = new Game { GameNumber = 1, StartedAt = DateTime.UtcNow };
            context.Games.Add(game);
            await context.SaveChangesAsync();

            await service.AddPlayersToGameAsAdminAsync(game.Id, new List<int> { 1, 2 });

            var participants = await context.Players
                .Where(p => p.GameId == game.Id)
                .ToListAsync();

            participants.Count.Should().Be(2);
        }

        [Fact]
        public async Task RemoveParticipant_ShouldRemoveParticipant()
        {
            var context = GetDbContext();
            var service = new PlayerService(context);

            var game = new Game { GameNumber = 1, StartedAt = DateTime.UtcNow };
            context.Games.Add(game);
            context.Players.Add(new Player { GameId = game.Id, UserId = 1 });
            await context.SaveChangesAsync();

            var remaining = await service.RemovePlayerAsync(game.Id, 1);

            remaining.Should().BeEmpty();
            (await context.Players.CountAsync()).Should().Be(0);
        }


    }
}
