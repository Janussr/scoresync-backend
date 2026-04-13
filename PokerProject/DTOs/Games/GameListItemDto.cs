namespace PokerProject.DTOs.Games
{
    public class GameListItemDto
    {
        public int Id { get; set; }
        public int GameNumber { get; set; }
        public DateTimeOffset StartedAt { get; set; }
        public DateTimeOffset? EndedAt { get; set; }
        public bool IsFinished { get; set; }
        public Game.GameType Type { get; set; } 
    }
}
