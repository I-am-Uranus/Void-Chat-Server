namespace Void.DTOs
{
    public class GroupMessageDTO
    {
        public int Id { get; set; }
        public string Content { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public int SenderId { get; set; }
        public string SenderName { get; set; } = string.Empty;
        public string? SenderProfilePicture { get; set; }
        public int GroupId { get; set; }
        public string? ImageData { get; set; }
        public string? ImageMimeType { get; set; }
    }
}
