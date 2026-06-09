using Void.DTOs;
using Void.Models;
using Void.Repositories;

namespace Void.Services
{
    public class SettingsService
    {
        private readonly UserRepository _userRepository;

        public SettingsService(UserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public UserSettingsDTO UpdateProfilePicture(int userId, string? profilePicture)
        {
            var errors = new List<string>();
            ProfilePictureValidator.Validate(profilePicture, errors);

            if (errors.Any())
                throw new ArgumentException(string.Join(Environment.NewLine, errors));

            var user = _userRepository.GetById(userId);
            if (user == null)
                throw new KeyNotFoundException("User not found");

            user.ProfilePicture = string.IsNullOrWhiteSpace(profilePicture)
                ? null
                : profilePicture;

            _userRepository.Update(user);

            return ToUserSettingsDTO(user);
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
