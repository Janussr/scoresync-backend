namespace PokerProject.DTOs
{
    public class PlayerPageRoundDto
    {
        public int Id { get; set; }
        public int RoundNumber { get; set; }
        public DateTimeOffset StartedAt { get; set; }
        public DateTimeOffset? EndedAt { get; set; }
        public List<PlayerPageScoreEntryDto> Scores { get; set; } = new();
    }
}
