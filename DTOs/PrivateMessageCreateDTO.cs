namespace Void.DTOs
{
    public class PrivateMessageCreateDTO
    {
        public int ReceiverId { get; set; }
        public string Content { get; set; } = string.Empty;
        public string? ImageData { get; set; }
        public string? ImageMimeType { get; set; }
    }
}
