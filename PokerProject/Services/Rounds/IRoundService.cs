using PokerProject.DTOs;

namespace PokerProject.Services.Rounds
{
    public interface IRoundService
    {
        Task<RoundDto> StartNewRoundAsync(int gameId);
    }
}
