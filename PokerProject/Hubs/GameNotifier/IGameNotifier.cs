using PokerProject.DTOs.Rounds;

namespace PokerProject.Hubs.GameNotifier
{
    public interface IGameNotifier
    {
        Task GameUpdated(int gameId, object updatedGameDto);
        Task KnockoutUpdated(int gameId, object payload);
        Task StartNewRound(int gameId, RoundDto newDto);
    }
}
