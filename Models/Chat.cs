namespace Void.Models
{
    public class Chat
    {
        public int Id { get; set; }
        public string? Content { get; set; } = null!;
        public DateTime? Timestamp { get; set; } = DateTime.UtcNow;
        public int? SenderId { get; set; }
        public int? ReceiverId { get; set; }
        public bool? IsRead { get; set; } = false;
        public string? ImageData { get; set; }
        public string? ImageMimeType { get; set; }

        public virtual User? Sender { get; set; }
        public virtual User? Receiver { get; set; }
    }
}