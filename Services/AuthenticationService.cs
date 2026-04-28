using System.Text.RegularExpressions;
using Void.Models;

namespace Void.Services
{
    public class AuthenticationService
    {
        private readonly UserService _userService;

        public AuthenticationService(UserService userService)
        {
            _userService = userService;
        }

        public void Register(string username, string password, string confirmPassword, string email)
        {
            var errors = new List<string>();

            ValidatePassword(password, confirmPassword, errors);
            ValidateEmail(email, errors);

            if (_userService.UserExists(username))
                errors.Add("Username already exists");

            if (_userService.EmailExists(email))
                errors.Add("Email already registered");

            if (errors.Any())
                throw new ArgumentException(string.Join(Environment.NewLine, errors));

            var user = new User
            {
                UserName = username,
                Password = BCrypt.Net.BCrypt.EnhancedHashPassword(password, workFactor: 11),
                Email = email
            };

            _userService.Add(user);
        }

        public User? SignIn(string username, string password)
        {
            var user = _userService.GetByUsername(username);
            if (user == null) return null;

            return BCrypt.Net.BCrypt.EnhancedVerify(password, user.Password) ? user : null;
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
    }
}
