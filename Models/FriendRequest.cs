namespace Void.Models
{
    public class FriendRequest
    {
        public int Id { get; set; }
        public int RequesterId { get; set; }
        public int RecipientId { get; set; }
        public FriendshipStatus Status { get; set; } = FriendshipStatus.Pending;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public virtual User Requester { get; set; } = null!;
        public virtual User Recipient { get; set; } = null!;
    }
}
