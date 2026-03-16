using PokerProject.Models;

namespace PokerProject.DTOs
{
    public class SetUserRoleDto
    {
        public int UserId { get; set; }
        public User.UserRole Role { get; set; }
    }
}
