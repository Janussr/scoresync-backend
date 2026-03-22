namespace PokerProject.DTOs
{
    public class RebuyRequestDto
    {
        public int GameId { get; set; }
        public int ActorUserId { get; set; }   
        public int TargetUserId { get; set; } 
        public bool IsAdmin { get; set; }
    }
}
