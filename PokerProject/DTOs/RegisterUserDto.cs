using static PokerProject.Models.User;

namespace PokerProject.DTOs
{
    public class RegisterUserDto
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public UserRole Role { get; set; }
    }
}
