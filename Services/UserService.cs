using Void.Models;
using Void.Repositories;

namespace Void.Services
{
    public class UserService
    {
        private readonly UserRepository _userRepository;

        public UserService(UserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public List<User> GetAll() => _userRepository.GetAll();

        public User? GetById(int id) => _userRepository.GetById(id);

        public User? GetByUsername(string username) =>
            _userRepository.GetByUsername(username);

        public List<User> FindByName(string name) =>
            _userRepository.FindByName(name);

        public bool Delete(int id)
        {
            var user = _userRepository.GetById(id);
            if (user == null) return false;

            _userRepository.Delete(user);
            return true;
        }

        public bool Update(int id, User updatedUser)
        {
            var user = _userRepository.GetById(id);
            if (user == null) return false;

            user.UserName = updatedUser.UserName;
            user.DisplayName = updatedUser.DisplayName;
            user.Password = updatedUser.Password;
            user.Email = updatedUser.Email;

            _userRepository.Update(user);
            return true;
        }


        public bool UserExists(string username) => _userRepository.UserExists(username);

        public bool EmailExists(string email) => _userRepository.EmailExists(email);

        public void Add(User user) => _userRepository.Add(user);

        public async Task<User?> GetByIdAsync(int id)
        {
            return await _userRepository.GetByIdAsync(id);
        }

        public void MarkUserActive(int userId)
        {
            _userRepository.UpdateLastActive(userId);
        }

        public DateTime? GetLastActive(int userId)
        {
            var user = _userRepository.GetById(userId);
            return user?.LastActive;
        }

    }
}
