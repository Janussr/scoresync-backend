using PokerProject.Models;

public class Score
{
    public int Id { get; set; }

    public int GameId { get; set; }
    public Game Game { get; set; } = null!;

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public int Points { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public int? VictimUserId { get; set; }   
    public User? VictimUser { get; set; }

    public ScoreType Type { get; set; }
    public enum ScoreType
    {
        Chips,
        Rebuy,
        Bounty
    }
}
