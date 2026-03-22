namespace PokerProject.DTOs
{
    public class WinnerDto
    {
        public int PlayerId { get; set; }
        public string UserName { get; set; } = null!;
        public int WinningScore { get; set; }
        public DateTime WinDate { get; set; }
    }

}
