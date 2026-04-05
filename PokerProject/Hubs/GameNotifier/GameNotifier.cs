using Microsoft.AspNetCore.SignalR;
using PokerProject.DTOs.Rounds;

namespace PokerProject.Hubs.GameNotifier
{
    public class GameNotifier : IGameNotifier
    {
        private readonly IHubContext<GameHub> _hubContext;

        public GameNotifier(IHubContext<GameHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public Task GameUpdated(int gameId, object updatedGameDto)
            => _hubContext.Clients.Group($"Game-{gameId}")
                .SendAsync("GameUpdated", updatedGameDto);

        public Task KnockoutUpdated(int gameId, object payload)
        => _hubContext.Clients.Group($"Game-{gameId}")
            .SendAsync("KnockoutUpdated", payload);


        public Task StartNewRound(int gameId, RoundDto newDto)
            => _hubContext.Clients.Group($"Game-{gameId}")
                .SendAsync("RoundStarted", newDto);


    }
}
