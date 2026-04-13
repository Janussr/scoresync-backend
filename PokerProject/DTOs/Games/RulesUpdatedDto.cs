namespace PokerProject.DTOs.Games
{
    public class RulesUpdatedDto
    {
        public int GameId { get; set; }
        public int? RebuyValue { get; set; }
        public int? BountyValue { get; set; }
    }
}
