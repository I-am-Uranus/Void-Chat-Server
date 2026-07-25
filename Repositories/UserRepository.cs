using VoidPart2.Database;
using VoidPart2.Models;

namespace VoidPart2.Repositories
{
    public class UserRepository
    {
        private readonly DatabaseContext _context;

        public UserRepository(DatabaseContext context)
        {
            _context = context;
        }

        public List<User> GetAll() => _context.Users.ToList();



        public User? GetById(int id) => _context.Users.FirstOrDefault(u => u.Id == id);

        public User? GetByUsername(string username) =>
            _context.Users.FirstOrDefault(u => u.UserName == username);

        public List<User> FindByName(string name) =>
            _context.Users
                .Where(u => u.DisplayName != null && u.DisplayName.ToLower().Contains(name.ToLower()))
                .ToList();

        public void Add(User user)
        {
            _context.Users.Add(user);
            _context.SaveChanges();
        }

        public void Update(User user)
        {
            _context.Users.Update(user);
            _context.SaveChanges();
        }

        public void Delete(User user)
        {
            _context.Users.Remove(user);
            _context.SaveChanges();
        }

        public bool UserExists(string username) =>
            _context.Users.Any(u => u.UserName == username);

        public bool EmailExists(string email) =>
            _context.Users.Any(u => u.Email == email);


        public async Task<User?> GetByIdAsync(int id)
        {
            return await _context.Users.FindAsync(id);
        }

        public void UpdateLastActive(int userId)
        {
            var user = _context.Users.FirstOrDefault(u => u.Id == userId);
            if (user != null)
            {
                user.LastActive = DateTime.UtcNow;
                _context.SaveChanges();
            }
        }

    }
}


