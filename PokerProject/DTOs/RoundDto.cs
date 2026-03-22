namespace PokerProject.DTOs
{
    public class RoundDto
    {
        public int Id { get; set; }
        public int RoundNumber { get; set; }    
        public DateTime StartedAt { get; set; }
        public DateTime? EndedAt { get; set; }

        public List<ScoreDto> Scores { get; set; } = new();


    }
}
