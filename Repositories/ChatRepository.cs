using Microsoft.EntityFrameworkCore;
using Void.Database;
using Void.Models;

namespace Void.Repositories
{
    public class ChatRepository
    {
        private readonly DatabaseContext _context;

        public ChatRepository(DatabaseContext context)
        {
            _context = context;
        }

        public async Task<List<Chat>> GetConversationAsync(int user1, int user2)
        {
            return await _context.Chats
                .Include(c => c.Sender)
                .Include(c => c.Receiver)
                .Where(c =>
                    (c.SenderId == user1 && c.ReceiverId == user2) ||
                    (c.SenderId == user2 && c.ReceiverId == user1))
                .OrderBy(c => c.Timestamp)
                .ToListAsync();
        }

        public async Task<Chat?> GetByIdAsync(int id)
        {
            return await _context.Chats
                .Include(c => c.Sender)
                .Include(c => c.Receiver)
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task AddAsync(Chat chat)
        {
            _context.Chats.Add(chat);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Chat chat)
        {
            _context.Chats.Update(chat);
            await _context.SaveChangesAsync();
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
