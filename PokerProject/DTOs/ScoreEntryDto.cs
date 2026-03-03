using PokerProject.Models;
using static Score;

namespace PokerProject.DTOs
{
    public class ScoreEntryDto
    {
        public int Id { get; set; }
        public int Points { get; set; }
        public DateTime CreatedAt { get; set; }
        public ScoreType Type { get; set; }
        public int? VictimUserId { get; set; }
        public string? VictimUserName { get; set; }
    }
}
