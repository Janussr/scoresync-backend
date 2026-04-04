using PokerProject.DTOs;
using System.Security.Claims;

namespace PokerProject.Services.Games
{
    public interface IGameService
    {
        Task<GameDto> StartGameAsync(ClaimsPrincipal currentUser, StartGameRequestDto request);
        Task OpenGameForPlayers(int gameId);
        Task<GameDto> CancelGameAsync(int gameId);
        Task<PlayerDto> JoinGameAsPlayerAsync(int gameId, int userId);
        Task<GameDto> EndGameAsync(int gameId);
        Task RemoveGameAsync(int gameId);
        Task<List<GameHistoryListItemDto>> GetGameHistoryAsync();
        Task<List<GamePanelDto>> GetAllActiveGamesForGamePanelAsync(int userId);
        Task<ActiveGamePlayerPageDto?> GetActiveGameForPlayerAsync(int userId);
        Task<List<GameListDto>> GetActiveGamesLobbyListAsync();
        Task<List<GameDto>> GetAllGamesAsync();
        Task<GameDetailsDto?> GetGameDetailsAsync(int gameId, string? role);
        Task UpdateRulesAsync(int gameId, UpdateRulesDto dto);
        Task LeaveGameAsync(int gameId, int userId);
    }
}
