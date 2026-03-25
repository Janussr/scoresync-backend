using PokerProject.DTOs;

namespace PokerProject.Services.Bounties
{
    public interface IBountyService
    {
        Task<ScoreDto> HandleKnockoutAsync(int gameId, int killerPlayerId, int? victimPlayerId);
        Task<ScoreDto> PlayerKnockoutAsync(int gameId, int killerUserId, int? victimPlayerId);
        Task<List<BountyLeaderboardDto>> GetBountyLeaderboardAsync();
    }
}
