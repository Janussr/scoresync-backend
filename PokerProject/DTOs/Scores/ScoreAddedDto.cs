namespace PokerProject.DTOs.Scores
{
    public class ScoreAddedDto
    {
        public int GameId { get; set; }
        public ScoreDto Score { get; set; } = null!;
    }
}
