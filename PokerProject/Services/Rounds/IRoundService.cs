using PokerProject.DTOs;

namespace PokerProject.Services.Rounds
{
    public interface IRoundService
    {
        Task<RoundDto> StartNewRoundAsync(int gameId);
        Task<RoundDto> EndRoundAsync(int roundId);
        Task<RoundDto?> GetCurrentRoundAsync(int gameId);
    }
}
