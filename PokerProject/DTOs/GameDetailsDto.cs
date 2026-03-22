namespace PokerProject.DTOs
{
    public class GameDetailsDto
    {
        public int Id { get; set; }
        public int GameNumber { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? EndedAt { get; set; }
        public bool IsFinished { get; set; }
        public List<GameScoreboardDto> Scores { get; set; } = new();
        public List<RoundDto> Rounds { get; set; } = new();
        public List<PlayerDto> Players { get; set; }
        public WinnerDto? Winner { get; set; }
        public int? RebuyValue { get; set; }
        public int? BountyValue { get; set; }
    }

}
