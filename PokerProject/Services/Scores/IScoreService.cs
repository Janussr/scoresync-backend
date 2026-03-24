using PokerProject.DTOs;

namespace PokerProject.Services.Scores
{
    public interface IScoreService
    {
        Task<ScoreDto> AddScoreAsync(int gameId, int currentUserId, int points, int? targetPlayerId = null);
        Task<ScoreDto> RemoveScoreAsync(int scoreId);
        Task<List<ScoreDto>> AddScoresBulkAsync(BulkAddScoresDto dto);
        Task<PlayerScoreDetailsDto> GetPlayerScoreEntries(int gameId, int userId);
        Task<ScoreDto> RegisterRebuyAsync(int gameId, int actorUserId, int? targetPlayerId = null);

    }
}
