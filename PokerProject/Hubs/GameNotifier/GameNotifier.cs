using Microsoft.AspNetCore.SignalR;
using PokerProject.DTOs.Bounties;
using PokerProject.DTOs.Games;
using PokerProject.DTOs.Players;
using PokerProject.DTOs.Rounds;
using PokerProject.DTOs.Scores;

namespace PokerProject.Hubs.GameNotifier
{
    public class GameNotifier : IGameNotifier
    {
        private readonly IHubContext<GameHub> _hubContext;

        public GameNotifier(IHubContext<GameHub> hubContext)
        {
            _hubContext = hubContext;
        }

        //public Task GameUpdated(int gameId, object updatedGameDto)
        //    => _hubContext.Clients.Group($"Game-{gameId}")
        //        .SendAsync("GameUpdated", updatedGameDto);

        public Task KnockoutUpdated(int gameId, object payload)
        => _hubContext.Clients.Group($"Game-{gameId}")
            .SendAsync("KnockoutUpdated", payload);



        public Task GameEnded(int gameId)
            => _hubContext.Clients.Group($"Game-{gameId}")
                .SendAsync("GameFinished", gameId);


        public Task StartNewRound(int gameId, RoundDto newDto)
            => _hubContext.Clients.Group($"Game-{gameId}")
                .SendAsync("RoundStarted", newDto);

        public Task KnockoutTargetsUpdated(
            int gameId,
            IEnumerable<KnockoutTargetDto> knockoutTargets)
            => _hubContext.Clients.Group($"Game-{gameId}")
                .SendAsync("KnockoutTargetsUpdated", new KnockoutTargetsUpdatedDto
                {
                    GameId = gameId,
                    KnockoutTargets = knockoutTargets.ToList()
                });

        public Task PlayerRemoved(int gameId, PlayerRemovedDto payload)
             => _hubContext.Clients.Group($"Game-{gameId}")
                .SendAsync("PlayerRemoved", payload);

        public Task RulesUpdated(int gameId, RulesUpdatedDto payload)
             => _hubContext.Clients.Group($"Game-{gameId}")
                .SendAsync("RulesUpdated", payload);

        public Task PlayerJoined(int gameId, PlayerJoinedDto payload)
            => _hubContext.Clients.Group($"Game-{gameId}")
                .SendAsync("PlayerJoined", payload);


        public Task PlayerLeft(int gameId, PlayerLeftDto payload)
            => _hubContext.Clients.Group($"Game-{gameId}")
                .SendAsync("PlayerLeft", payload);

        public Task RebuyUpdated(int gameId, RebuyUpdatedDto payload)
            => _hubContext.Clients.Group($"Game-{gameId}")
                .SendAsync("RebuyUpdated", payload);

        public Task ScoreAdded(int gameId, ScoreAddedDto payload)
            => _hubContext.Clients.Group($"Game-{gameId}")
                .SendAsync("ScoreAdded", payload);

    }
}
