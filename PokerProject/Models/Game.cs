using PokerProject.Models;

public class Game
{
    public int Id { get; set; }

    public int GameNumber { get; set; }
    public int GamemasterId { get; set; }
    public User Gamemaster { get; set; } = null;
    public int? RebuyValue { get; set; }
    public int? BountyValue { get; set; }
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? EndedAt { get; set; }

    public bool IsFinished { get; set; }
    public int? WinnerPlayerId { get; set; }
    public Player? WinnerPlayer { get; set; }
    public ICollection<Player> Players { get; set; } = new List<Player>();
    public ICollection<Round> Rounds { get; set; } = new List<Round>();

    public GameType Type { get; set; }
    public enum GameType
    {
        BlackJack,
        Poker,
        Roulette
    }

} 
