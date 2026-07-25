using VoidPart2.DTOs;
using VoidPart2.Models;
using VoidPart2.Repositories;

namespace VoidPart2.Services
{
    public class SettingsService
    {
        private readonly UserRepository _userRepository;

        public SettingsService(UserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public UserSettingsDTO UpdateProfile(int userId, string? displayName, string? profilePicture)
        {
            var errors = new List<string>();
            ValidateDisplayName(displayName, errors);
            ProfilePictureValidator.Validate(profilePicture, errors);

            if (errors.Any())
                throw new ArgumentException(string.Join(Environment.NewLine, errors));

            var user = GetExistingUser(userId);

            user.DisplayName = displayName!.Trim();
            user.ProfilePicture = NormalizeProfilePicture(profilePicture);

            _userRepository.Update(user);

            return ToUserSettingsDTO(user);
        }

        public UserSettingsDTO UpdateDisplayName(int userId, string? displayName)
        {
            var errors = new List<string>();
            ValidateDisplayName(displayName, errors);

            if (errors.Any())
                throw new ArgumentException(string.Join(Environment.NewLine, errors));

            var user = GetExistingUser(userId);

            user.DisplayName = displayName!.Trim();

            _userRepository.Update(user);

            return ToUserSettingsDTO(user);
        }

        public UserSettingsDTO UpdateProfilePicture(int userId, string? profilePicture)
        {
            var errors = new List<string>();
            ProfilePictureValidator.Validate(profilePicture, errors);

            if (errors.Any())
                throw new ArgumentException(string.Join(Environment.NewLine, errors));

            var user = GetExistingUser(userId);

            user.ProfilePicture = NormalizeProfilePicture(profilePicture);

            _userRepository.Update(user);

            return ToUserSettingsDTO(user);
        }

        private User GetExistingUser(int userId)
        {
            var user = _userRepository.GetById(userId);
            if (user == null)
                throw new KeyNotFoundException("User not found");

            return user;
        }

        private static void ValidateDisplayName(string? displayName, List<string> errors)
        {
            if (string.IsNullOrWhiteSpace(displayName))
                errors.Add("Display name is required");
        }

        private static string? NormalizeProfilePicture(string? profilePicture)
        {
            return string.IsNullOrWhiteSpace(profilePicture)
                ? null
                : profilePicture;
        }

        private static UserSettingsDTO ToUserSettingsDTO(User user)
        {
            return new UserSettingsDTO
            {
                Id = user.Id,
                UserName = user.UserName,
                DisplayName = user.DisplayName,
                Email = user.Email,
                ProfilePicture = user.ProfilePicture
            };
        }
    }
}
