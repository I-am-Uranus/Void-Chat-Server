using Microsoft.EntityFrameworkCore;
using Void.Database;
using Void.Models;

namespace Void.Repositories
{
    public class FriendshipRepository
    {
        private readonly DatabaseContext _context;

        public FriendshipRepository(DatabaseContext context)
        {
            _context = context;
        }

        public async Task<Friendship?> GetByIdAsync(int id)
        {
            return await _context.Friendships
                .Include(f => f.UserA)
                .Include(f => f.UserB)
                .FirstOrDefaultAsync(f => f.Id == id);
        }

        public async Task<Friendship?> GetByUsersAsync(int userAId, int userBId)
        {
            return await _context.Friendships
                .Include(f => f.UserA)
                .Include(f => f.UserB)
                .FirstOrDefaultAsync(f =>
                    (f.UserAId == userAId && f.UserBId == userBId) ||
                    (f.UserAId == userBId && f.UserBId == userAId));
        }

        public async Task<List<Friendship>> GetFriendsForUserAsync(int userId)
        {
            return await _context.Friendships
                .Include(f => f.UserA)
                .Include(f => f.UserB)
                .Where(f => f.UserAId == userId || f.UserBId == userId)
                .ToListAsync();
        }

        public async Task AddAsync(Friendship friendship)
        {
            _context.Friendships.Add(friendship);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Friendship friendship)
        {
            _context.Friendships.Update(friendship);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var friendship = await GetByIdAsync(id);
            if (friendship == null) return false;

            _context.Friendships.Remove(friendship);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> AreFriends(int userAId, int userBId)
        {
            return await _context.Friendships
                .AnyAsync(f =>
                    (f.UserAId == userAId && f.UserBId == userBId) ||
                    (f.UserAId == userBId && f.UserBId == userAId));
        }
    }
}
