namespace Void.Models
{
    public class UserBlock
    {
        public int Id { get; set; }
        public int BlockerId { get; set; }
        public int BlockedId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public virtual User Blocker { get; set; }
        public virtual User Blocked { get; set; }
    }
}
