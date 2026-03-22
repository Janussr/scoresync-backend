using PokerProject.DTOs;

namespace PokerProject.Services.Scores
{
    public interface IScoreService
    {
        Task<ScoreDto> AddScoreAsync(int gameId, int userId, int value);
        Task<ScoreDto> RemoveScoreAsync(int scoreId);
        Task<List<ScoreDto>> AddScoresBulkAsync(BulkAddScoresDto dto);
        Task<PlayerScoreDetailsDto> GetPlayerScoreEntries(int gameId, int userId);

        Task<ScoreDto> RegisterRebuyForAdminAsync(int gameId, int actorUserId, int targetUserId, bool isAdmin);

    }
}
