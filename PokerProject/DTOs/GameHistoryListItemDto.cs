namespace PokerProject.DTOs
{
    public class GameHistoryListItemDto
    {
        public int Id { get; set; }
        public int GameNumber { get; set; }
        public Game.GameType Type { get; set; } 
        public DateTimeOffset? Date { get; set; }
        public string WinnerName { get; set; } = "";
        public int PlayerCount { get; set; }
        public int RoundCount { get; set; }

    }
}
