namespace VoidPart2.DTOs
{
    public class ChatCreateDTO
    {
        public int SenderId { get; set; }
        public int ReceiverId { get; set; }
        public string Content { get; set; } = string.Empty;
        public string? ImageData { get; set; }
        public string? ImageMimeType { get; set; }
    }

}
