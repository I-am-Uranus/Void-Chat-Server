namespace VoidPart2.DTOs
{
    public class BlockedUserDTO
    {
        public int Id { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string? ProfilePicture { get; set; }
    }
}
