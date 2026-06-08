namespace Void.Models
{
    public class GroupMessage
    {
        public int Id { get; set; }
        public int GroupId { get; set; }
        public int SenderId { get; set; }
        public string Content { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string? ImageData { get; set; }
        public string? ImageMimeType { get; set; }

        public virtual Group Group { get; set; }
        public virtual User Sender { get; set; }
    }
}
