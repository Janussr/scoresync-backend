namespace PokerProject.DTOs
{
    public class HallOfFameDto
    {
        public int UserId { get; set; }
        public string PlayerName { get; set; } = null!;
        public int Wins { get; set; }
        public  Game.GameType GameType { get; set; }
    }
}
