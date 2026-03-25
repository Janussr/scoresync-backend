namespace PokerProject.DTOs
{
    public class GameDto
    {
        public int Id { get; set; }
        public int PlayerId { get; set; }
        public int GameNumber { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? EndedAt { get; set; }
        public bool IsFinished { get; set; }
        public Game.GameType Type { get; set; }

        public int? RebuyValue { get; set; }
        public int? BountyValue { get; set; }
        public List<PlayerDto> Players { get; set; } = new();
        public List<ScoreDto> Scores { get; set; } = new();
        public List<RoundDto> Rounds { get; set; } = [];

        public int? WinnerPlayerId { get; set; }
        public WinnerDto? Winner { get; set; }
    }
}
