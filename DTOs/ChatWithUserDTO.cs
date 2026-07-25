

namespace VoidPart2.DTOs
{
    public class ChatWithUserDTO
    {
        public int Id { get; set; }
        public string Content { get; set; } = "";
        public DateTime Timestamp { get; set; }
        public int SenderId { get; set; }
        public string SenderName { get; set; } = "Unknown";
        public DateTime? SenderLastActive { get; set; }
        public int ReceiverId { get; set; }
        public string ReceiverName { get; set; } = "Unknown";
        public DateTime? ReceiverLastActive { get; set; }
        public string? ImageData { get; set; }
        public string? ImageMimeType { get; set; }
        public bool IsRead { get; set; }
    }

}


