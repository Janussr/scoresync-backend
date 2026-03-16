using PokerProject.DTOs;

namespace PokerProject.Services.Users
{
    public interface IUserService
    {
        Task<IEnumerable<UserDto>> GetAllUsersAsync();
        Task<UserDto?> GetUserByIdAsync(int id);
        Task<UserDto> RegisterAsync(RegisterUserDto dto);
        Task<string?> LoginAndGenerateTokenAsync(string username, string password);
        Task<UserDto?> AdminResetPasswordAsync(int userId, string newPassword);
        Task<UserDto?> SetUserRoleAsync(SetUserRoleDto dto);
    }
}
