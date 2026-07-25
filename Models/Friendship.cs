namespace VoidPart2.Models
{
    public class Friendship
    {
        public int Id { get; set; }
        public int UserAId { get; set; }
        public int UserBId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public virtual User UserA { get; set; } = null!;
        public virtual User UserB { get; set; } = null!;
    }

    public enum FriendshipStatus
    {
        Pending,
        Accepted,
        Rejected,
        Blocked
    }
}
