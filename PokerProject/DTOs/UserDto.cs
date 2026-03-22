using PokerProject.Models;

namespace PokerProject.DTOs
{
    public class UserDto
    {
        public int Id { get; set; }
        public string Username { get; set; } = null!;
        public User.UserRole Role { get; set; }

    }
}
