namespace PokerProject.DTOs
{
    public class KnockoutAdminDto
    {
        public int GameId { get; set; }
        public int? KillerPlayerId { get; set; }
        public int? VictimPlayerId { get; set; }
    }
}
