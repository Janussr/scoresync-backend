using Microsoft.EntityFrameworkCore;
using PokerProject.Data;
using PokerProject.DTOs;
using PokerProject.Models;
using System.Data;

namespace PokerProject.Services.Users
{
    public class UserService : IUserService
    {
        private readonly PokerDbContext _context;

        public UserService(PokerDbContext context)
        {
            _context = context;
        }

        public async Task<int?> GetActiveGameIdByUserAsync(int userId)
        {
            var activeGameId = await _context.Players
                .Include(p => p.Game)
                .Where(p => p.UserId == userId && p.IsActive && !p.Game.IsFinished)
                .Select(p => (int?)p.GameId)
                .FirstOrDefaultAsync();

            return activeGameId;
        }

        public async Task<IEnumerable<UserDto>> GetAllUsersAsync()
        {
            return await _context.Users.Select(u => new UserDto {
                    Id = u.Id,
                    Username = u.Username,
                }).ToListAsync();
        }


        public async Task<UserDto?> GetUserByIdAsync(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return null;

            return new UserDto
            {
                Id = user.Id,
                Username = user.Username,
                Role = user.Role,
            };
        }


        public async Task<UserDto> RegisterAsync(RegisterUserDto dto)
        {
            var normalizedUsername = NormalizeUsername(dto.Username);

            var existingUser = await _context.Users
             .FirstOrDefaultAsync(u => u.Username.ToLower() == normalizedUsername.ToLower());

            if (existingUser != null)
                throw new Exception("Username already exists");

            var user = new User
            {
                Username = normalizedUsername,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                Role = dto.Role
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return new UserDto
            {
                Id = user.Id,
                Username = user.Username,
            };
        }

        private static string NormalizeUsername(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
                return username;

            return char.ToUpper(username[0]) + username.Substring(1);
        }

        public async Task<User?> ValidateUserAsync(string username, string password)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (user == null) return null;

            bool verified = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
            if (!verified) return null;

            return user;
        }

        public async Task<UserDto?> AdminResetPasswordAsync(int userId, string newPassword)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return null;

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);

            await _context.SaveChangesAsync();

            return new UserDto
            {
                Id = user.Id,
                Username = user.Username,
            };
        }

        public async Task<UserDto?> PlayerUpdatePasswordAsync(int userId, string currentPassword, string newPassword)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return null;

            bool verified = BCrypt.Net.BCrypt.Verify(currentPassword, user.PasswordHash);
            if (!verified)
                throw new Exception("Current password is incorrect");

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);

            await _context.SaveChangesAsync();

            return new UserDto
            {
                Id = user.Id,
                Username = user.Username,
                Role = user.Role
            };
        }

        public async Task<UserDto?> SetUserRoleAsync(SetUserRoleDto dto)
        {
            var user = await _context.Users.FindAsync(dto.UserId);
            if (user == null) return null;

            user.Role = dto.Role;

            await _context.SaveChangesAsync();

            return new UserDto
            {
                Id = user.Id,
                Username = user.Username,
            };
        }

    public async Task<UserDto?> PlayerUpdateUsernameAsync(int userId, string newUsername)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return null;

            var existingUser = await _context.Users
                .AnyAsync(u => u.Username == newUsername && u.Id != userId);
            if (existingUser)
                throw new Exception("Username already exists");

            user.Username = newUsername;
            await _context.SaveChangesAsync();

            return new UserDto
            {
                Id = user.Id,
                Username = user.Username,
                Role = user.Role
            };
        }


    }
}
