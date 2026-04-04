namespace PokerProject.DTOs
{
    public class ActiveGamePlayerPageDto
    {
        public int Id { get; set; }
        public int GameNumber { get; set; }
        public DateTimeOffset StartedAt { get; set; }
        public DateTimeOffset? EndedAt { get; set; }
        public bool IsFinished { get; set; }
        public Game.GameType Type { get; set; }
        public int? RebuyValue { get; set; }
        public int? BountyValue { get; set; }

        public PlayerDto Me { get; set; } = null!;
        public List<KnockoutTargetDto> KnockoutTargets { get; set; } = new();
        public List<PlayerPageRoundDto> Rounds { get; set; } = new();
    }
}
