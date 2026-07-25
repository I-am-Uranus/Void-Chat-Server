namespace VoidPart2.DTOs
{
    public class UpdateProfilePictureDTO
    {
        public string? ProfilePicture { get; set; }
    }

    public class UpdateDisplayNameDTO
    {
        public string DisplayName { get; set; } = string.Empty;
    }

    public class UpdateProfileDTO
    {
        public string DisplayName { get; set; } = string.Empty;
        public string? ProfilePicture { get; set; }
    }

    public class UserSettingsDTO
    {
        public int Id { get; set; }
        public string? UserName { get; set; }
        public string? DisplayName { get; set; }
        public string? Email { get; set; }
        public string? ProfilePicture { get; set; }
    }
}
