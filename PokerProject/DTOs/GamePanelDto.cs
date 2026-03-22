namespace PokerProject.DTOs
{
    public class GamePanelDto
    {
        public int Id { get; set; }
        public int GameNumber { get; set; }
        public DateTime StartedAt { get; set; }
        public bool IsFinished { get; set; }
        public Game.GameType Type { get; set; }

        public int? RebuyValue { get; set; }
        public int? BountyValue { get; set; }

        public List<PlayerDto> Players { get; set; } = new();
        public List<RoundDto> Rounds { get; set; } = new();
    }
}
