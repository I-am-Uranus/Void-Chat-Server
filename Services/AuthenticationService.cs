using System.Text.RegularExpressions;
using VoidPart2.Models;

namespace VoidPart2.Services
{
    public class AuthenticationService
    {
        private readonly UserService _userService;

        public AuthenticationService(UserService userService)
        {
            _userService = userService;
        }

        public void Register(
            string username,
            string displayName,
            string password,
            string confirmPassword,
            string email,
            string? profilePicture
        )
        {
            var errors = new List<string>();

            ValidateUsername(username, errors);
            ValidateDisplayName(displayName, errors);
            ValidatePassword(password, confirmPassword, errors);
            ValidateEmail(email, errors);
            ValidateProfilePicture(profilePicture, errors);

            if (_userService.UserExists(username))
                errors.Add("Username already exists");

            if (_userService.EmailExists(email))
                errors.Add("Email already registered");

            if (errors.Any())
                throw new ArgumentException(string.Join(Environment.NewLine, errors));

            var user = new User
            {
                UserName = username,
                DisplayName = displayName,
                Password = BCrypt.Net.BCrypt.EnhancedHashPassword(password, workFactor: 11),
                Email = email,
                ProfilePicture = profilePicture
            };

            _userService.Add(user);
        }

        public User? SignIn(string username, string password)
        {
            var user = _userService.GetByUsername(username);
            if (user == null) return null;

            return BCrypt.Net.BCrypt.EnhancedVerify(password, user.Password) ? user : null;
        }

        private void ValidateUsername(string username, List<string> errors)
        {
            if (string.IsNullOrWhiteSpace(username))
                errors.Add("Username is required");
        }

        private void ValidateDisplayName(string displayName, List<string> errors)
        {
            if (string.IsNullOrWhiteSpace(displayName))
                errors.Add("Display name is required");
        }

        private void ValidatePassword(string password, string confirmPassword, List<string> errors)
        {
            if (password.Length < 6)
                errors.Add("Password must be at least 6 characters");
            if (!Regex.IsMatch(password, @"[A-Z]"))
                errors.Add("Password must contain at least one uppercase letter");
            if (!Regex.IsMatch(password, @"[a-z]"))
                errors.Add("Password must contain at least one lowercase letter");
            if (!Regex.IsMatch(password, @"[0-9]"))
                errors.Add("Password must contain at least one digit");
            if (password != confirmPassword)
                errors.Add("Passwords don't match");
        }

        private void ValidateEmail(string email, List<string> errors)
        {
            const string pattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
            if (string.IsNullOrEmpty(email) || !Regex.IsMatch(email, pattern))
                errors.Add("Invalid email format");
        }

        private void ValidateProfilePicture(string? profilePicture, List<string> errors)
        {
            ProfilePictureValidator.Validate(profilePicture, errors);
        }


        public User? GetUserById(int id)
        {
            return _userService.GetById(id);
        }

    }


}
