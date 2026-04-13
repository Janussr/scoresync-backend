using PokerProject.DTOs.Bounties;
using PokerProject.DTOs.Games;
using PokerProject.DTOs.Players;
using PokerProject.DTOs.Rounds;

namespace PokerProject.Hubs.GameNotifier
{
    public interface IGameNotifier
    {
        Task KnockoutUpdated(int gameId, object payload);
        Task StartNewRound(int gameId, RoundDto newDto);
        Task KnockoutTargetsUpdated( int gameId,IEnumerable<KnockoutTargetDto> knockoutTargets);
        Task PlayerRemoved(int gameId, PlayerRemovedDto payload);
        Task GameEnded(int gameId);
        Task RulesUpdated(int gameId, RulesUpdatedDto payload);
    }
}
