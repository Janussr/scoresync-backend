namespace PokerProject.DTOs
{
    public class PlayerDto
    {
        public int PlayerId { get; set; }
        public int UserId { get; set; }
        public string  Username { get; set; } = null!;
        public int RebuyCount { get; set; }
        public int ActiveBounties { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
