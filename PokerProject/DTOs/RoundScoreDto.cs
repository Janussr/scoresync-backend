namespace PokerProject.DTOs
{
    public class RoundScoreDto
    {
        public int RoundId { get; set; }
        public int RoundNumber { get; set; }
        public DateTime StartedAt { get; set; }
        public int TotalPoints { get; set; }
        public List<ScoreEntryDto> Entries { get; set; } = new();
    }
}
