using Microsoft.EntityFrameworkCore;
using Void.Database;
using Void.Models;

namespace Void.Repositories
{
    public class FriendRequestRepository
    {
        private readonly DatabaseContext _context;

        public FriendRequestRepository(DatabaseContext context)
        {
            _context = context;
        }

        public async Task<FriendRequest?> GetByIdAsync(int id)
        {
            return await _context.FriendRequests
                .Include(fr => fr.Requester)
                .Include(fr => fr.Recipient)
                .FirstOrDefaultAsync(fr => fr.Id == id);
        }

        public async Task<FriendRequest?> GetByUsersAsync(int requesterId, int recipientId)
        {
            return await _context.FriendRequests
                .Include(fr => fr.Requester)
                .Include(fr => fr.Recipient)
                .FirstOrDefaultAsync(fr =>
                    (fr.RequesterId == requesterId && fr.RecipientId == recipientId) ||
                    (fr.RequesterId == recipientId && fr.RecipientId == requesterId));
        }

        public async Task<List<FriendRequest>> GetPendingRequestsForRecipientAsync(int recipientId)
        {
            return await _context.FriendRequests
                .Include(fr => fr.Requester)
                .Where(fr => fr.RecipientId == recipientId && fr.Status == FriendshipStatus.Pending)
                .ToListAsync();
        }

        public async Task AddAsync(FriendRequest request)
        {
            _context.FriendRequests.Add(request);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(FriendRequest request)
        {
            _context.FriendRequests.Update(request);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var request = await GetByIdAsync(id);
            if (request == null) return false;

            _context.FriendRequests.Remove(request);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
