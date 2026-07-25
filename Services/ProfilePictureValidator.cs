namespace VoidPart2.Services
{
    public static class ProfilePictureValidator
    {
        private const int MaxBase64Length = 400_000;

        public static void Validate(string? profilePicture, List<string> errors)
        {
            if (string.IsNullOrWhiteSpace(profilePicture))
                return;

            if (profilePicture.Length > MaxBase64Length)
            {
                errors.Add("Profile picture must be smaller than 300 KB");
                return;
            }

            var isValidImage =
                profilePicture.StartsWith("data:image/png;base64,") ||
                profilePicture.StartsWith("data:image/jpeg;base64,") ||
                profilePicture.StartsWith("data:image/webp;base64,") ||
                profilePicture.StartsWith("data:image/jfif;base64,");

            if (!isValidImage)
                errors.Add("Profile picture must be PNG, JPG, JFIF, or WEBP");
        }
    }
}
