using PokerProject.DTOs;
using System.Security.Claims;

namespace PokerProject.Services.Games
{
    public interface IGameService
    {
        Task<GameDto> StartGameAsync(ClaimsPrincipal currentUser);
        Task<GameDto> CancelGameAsync(int gameId);
        Task<PlayerDto> JoinGameAsPlayerAsync(int gameId, int userId);
        Task<GameDto> EndGameAsync(int gameId);
        Task RemoveGameAsync(int gameId);
        Task<GamePanelDto?> GetActiveGameForGamePanelAsync(int userId);
        Task<GameDetailsDto?> GetActiveGameForPlayerAsync(int playerId);
        Task<List<GameListDto>> GetActiveGamesLobbyListAsync();
        //Task<List<GameDto>> GetActiveGamesAsync();
        Task<List<GameDto>> GetAllGamesAsync();
        Task<GameDto?> GetGameByIdAsync(int gameId);
        Task<GameDetailsDto?> GetGameDetailsAsync(int gameId, string? role);
        Task UpdateRulesAsync(int gameId, UpdateRulesDto dto);
        Task LeaveGameAsync(int gameId, int userId);
    }
}
