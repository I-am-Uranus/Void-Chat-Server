namespace Void.DTOs
{
    public class ChatDTO
    {
        public int Id { get; set; }
        public string Content { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public int SenderId { get; set; }
        public string SenderName { get; set; } = string.Empty;
        public int ReceiverId { get; set; }
        public string ReceiverName { get; set; } = string.Empty;
        public string? ImageData { get; set; }
        public string? ImageMimeType { get; set; }
        public bool IsRead { get; set; }
    }
}
