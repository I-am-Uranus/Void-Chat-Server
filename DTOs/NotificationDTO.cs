namespace Void.DTOs
{
    public class NotificationDTO
    {
        public int Id { get; set; }
        public int RecipientUserId { get; set; }
        public int? SenderUserId { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
        public int? RelatedEntityId { get; set; }
    }
}
