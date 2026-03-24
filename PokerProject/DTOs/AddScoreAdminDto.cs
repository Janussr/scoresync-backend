namespace PokerProject.DTOs
{
    public class AddScoreAdminDto
    {
        public int GameId { get; set; }
        public int TargetPlayerId { get; set; } 
        public int Value { get; set; }
    }
}
