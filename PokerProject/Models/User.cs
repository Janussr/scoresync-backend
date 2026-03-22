namespace PokerProject.Models
{
    public class User
    {
        public int Id { get; set; }

        public string Username { get; set; } = null!;
        public string PasswordHash { get; set; } = null!;

        public UserRole Role { get; set; } = UserRole.User;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public ICollection<HallOfFame> HallOfFames { get; set; } = new List<HallOfFame>();
        public ICollection<Player> Players { get; set; } = new List<Player>();


        public enum UserRole
        {
            User,
            Admin,
            Gamemaster
        }
    }

}
