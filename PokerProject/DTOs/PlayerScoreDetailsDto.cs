namespace PokerProject.DTOs
{
    public class PlayerScoreDetailsDto
    {
        public int UserId { get; set; }
        public int PlayerId { get; set; }
        public string UserName { get; set; } = "";
        public int TotalPoints { get; set; }
        public List<ScoreEntryDto> Entries { get; set; } = new();
    }
}
