namespace PokerProject.Models
{
    public class Round
    {
        public int Id { get; set; }

        public int GameId { get; set; }
        public Game Game { get; set; } = null!;

        public int RoundNumber { get; set; } 

        public DateTime StartedAt { get; set; }
        public DateTime? EndedAt { get; set; }

        public ICollection<Score> Scores { get; set; } = new List<Score>();
    }
}
