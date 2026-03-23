namespace PokerProject.DTOs
{
    public class HallOfFameDto
    {
        public int PlayerId { get; set; }
        public string PlayerName { get; set; } = null!;
        public int Wins { get; set; }
    }
}
