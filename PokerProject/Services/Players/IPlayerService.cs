using PokerProject.DTOs;

namespace PokerProject.Services.Players
{
    public interface IPlayerService
    {
        //Admin adds players to game on game-panel page
        Task<List<PlayerDto>> AddPlayersToGameAsAdminAsync(int gameId, List<int> userIds);
        Task<List<PlayerDto>> GetPlayersAsync(int gameId);
        //TODO måske remove dne nedenunder, find ud af hvad pointen er.
        //Task<bool> IsUserAPlayerAsync(int gameId, int userId);
        Task LeaveGameAsPlayerAsync(int gameId, int userId);
        Task RemovePlayerAsAdminAsync(int gameId, int playerId);
    }
}
