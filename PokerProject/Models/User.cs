namespace PokerProject.Models
{
    public class User
    {
        public int Id { get; set; }

        public string Username { get; set; } = null!;
        public string PasswordHash { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string Role { get; set; } = null!;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<Score> Scores { get; set; } = new List<Score>();
        public ICollection<HallOfFame> HallOfFames { get; set; } = new List<HallOfFame>();
        public ICollection<GameParticipant> GameParticipants { get; set; } = new List<GameParticipant>();
    }

}
