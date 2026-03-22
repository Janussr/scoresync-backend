using PokerProject.Models;

public class Score
{
    public int Id { get; set; }

    public int? RoundId { get; set; }
    public Round? Round { get; set; }

    public int PlayerId { get; set; }
    public Player Player { get; set; } = null!;

    public int Value { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;


     public int? VictimPlayerId { get; set; }
    public Player? VictimPlayer { get; set; }


    public ScoreType Type { get; set; }
    public enum ScoreType
    {
        Chips,
        Rebuy,
        Bounty
    }
}
