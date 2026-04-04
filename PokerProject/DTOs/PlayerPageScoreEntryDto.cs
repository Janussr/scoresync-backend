using static Score;

namespace PokerProject.DTOs
{
    public class PlayerPageScoreEntryDto
    {
        public int Id { get; set; }
        public int PlayerId { get; set; }
        public int Points { get; set; }
        public ScoreType Type { get; set; }
    }
}
