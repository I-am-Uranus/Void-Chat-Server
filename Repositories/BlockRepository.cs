using Microsoft.EntityFrameworkCore;
using Void.Database;
using Void.Models;

namespace Void.Repositories
{
    public class BlockRepository
    {
        private readonly DatabaseContext _context;

        public BlockRepository(DatabaseContext context)
        {
            _context = context;
        }

        public async Task<UserBlock?> GetByUsersAsync(int blockerId, int blockedId)
        {
            return await _context.UserBlocks
                .Include(ub => ub.Blocker)
                .Include(ub => ub.Blocked)
                .FirstOrDefaultAsync(ub => ub.BlockerId == blockerId && ub.BlockedId == blockedId);
        }

        public async Task<List<UserBlock>> GetBlockedUsersForUserAsync(int userId)
        {
            return await _context.UserBlocks
                .Include(ub => ub.Blocked)
                .Where(ub => ub.BlockerId == userId)
                .ToListAsync();
        }

        public async Task<bool> IsBlockedBetweenUsersAsync(int userAId, int userBId)
        {
            return await _context.UserBlocks.AnyAsync(ub =>
                (ub.BlockerId == userAId && ub.BlockedId == userBId) ||
                (ub.BlockerId == userBId && ub.BlockedId == userAId));
        }

        public async Task AddAsync(UserBlock block)
        {
            _context.UserBlocks.Add(block);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var block = await _context.UserBlocks.FindAsync(id);
            if (block == null) return false;

            _context.UserBlocks.Remove(block);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteByUsersAsync(int blockerId, int blockedId)
        {
            var block = await _context.UserBlocks.FirstOrDefaultAsync(ub => ub.BlockerId == blockerId && ub.BlockedId == blockedId);
            if (block == null) return false;

            _context.UserBlocks.Remove(block);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
