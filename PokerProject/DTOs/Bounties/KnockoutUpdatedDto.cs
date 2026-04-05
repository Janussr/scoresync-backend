using PokerProject.DTOs.Scores;

namespace PokerProject.DTOs.Bounties
{
    public class KnockoutUpdatedDto
    {
        public int GameId { get; set; }
        public int KillerPlayerId { get; set; }
        public int VictimPlayerId { get; set; }
        public int KillerActiveBounties { get; set; }
        public int VictimActiveBounties { get; set; }
        public ScoreDto Score { get; set; } = null!;
    }
}
