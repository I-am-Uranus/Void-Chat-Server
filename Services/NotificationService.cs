using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Void.Database;
using Void.DTOs;
using Void.Hubs;
using Void.Models;

namespace Void.Services
{
    public class NotificationService
    {
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly DatabaseContext _context;

        public NotificationService(IHubContext<NotificationHub> hubContext, DatabaseContext context)
        {
            _hubContext = hubContext;
            _context = context;
        }

        public Task SendToUser(int userId, object notification)
        {
            return _hubContext.Clients.User(userId.ToString()).SendAsync("ReceiveNotification", notification);
        }

        public Task Broadcast(object notification)
        {
            return _hubContext.Clients.All.SendAsync("ReceiveNotification", notification);
        }

        public async Task<NotificationDTO> CreateAsync(
            int recipientUserId,
            int? senderUserId,
            string type,
            string title,
            string message,
            int? relatedEntityId = null)
        {
            var normalizedType = NotificationTypes.Normalize(type);
            if (normalizedType == NotificationTypes.All)
            {
                throw new ArgumentException("All cannot be used as a stored notification type.");
            }

            var notification = new Notification
            {
                RecipientUserId = recipientUserId,
                SenderUserId = senderUserId,
                Type = normalizedType,
                Title = title,
                Message = message,
                RelatedEntityId = relatedEntityId,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            var dto = ToDTO(notification);
            await SendToUser(recipientUserId, dto);

            return dto;
        }

        public async Task<List<NotificationDTO>> GetForUserAsync(int recipientUserId, string? type)
        {
            var normalizedType = NotificationTypes.Normalize(type);
            var query = _context.Notifications
                .Where(n => n.RecipientUserId == recipientUserId);

            if (normalizedType != NotificationTypes.All)
            {
                query = query.Where(n => n.Type == normalizedType);
            }

            return await query
                .OrderByDescending(n => n.CreatedAt)
                .Select(n => ToDTO(n))
                .ToListAsync();
        }

        public async Task<int> GetUnreadCountAsync(int recipientUserId, string? type = null)
        {
            var normalizedType = NotificationTypes.Normalize(type);
            var query = _context.Notifications
                .Where(n => n.RecipientUserId == recipientUserId && !n.IsRead);

            if (normalizedType != NotificationTypes.All)
            {
                query = query.Where(n => n.Type == normalizedType);
            }

            return await query.CountAsync();
        }

        public async Task<bool> MarkAsReadAsync(int id, int recipientUserId)
        {
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == id && n.RecipientUserId == recipientUserId);

            if (notification == null)
            {
                return false;
            }

            notification.IsRead = true;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<int> MarkAllAsReadAsync(int recipientUserId, string? type = null)
        {
            var normalizedType = NotificationTypes.Normalize(type);
            var query = _context.Notifications
                .Where(n => n.RecipientUserId == recipientUserId && !n.IsRead);

            if (normalizedType != NotificationTypes.All)
            {
                query = query.Where(n => n.Type == normalizedType);
            }

            var notifications = await query.ToListAsync();
            foreach (var notification in notifications)
            {
                notification.IsRead = true;
            }

            await _context.SaveChangesAsync();
            return notifications.Count;
        }

        public async Task<int> MarkRelatedAsReadAsync(int recipientUserId, string type, int relatedEntityId)
        {
            var normalizedType = NotificationTypes.Normalize(type);
            var notifications = await _context.Notifications
                .Where(n =>
                    n.RecipientUserId == recipientUserId &&
                    n.Type == normalizedType &&
                    n.RelatedEntityId == relatedEntityId &&
                    !n.IsRead)
                .ToListAsync();

            foreach (var notification in notifications)
            {
                notification.IsRead = true;
            }

            await _context.SaveChangesAsync();
            return notifications.Count;
        }

        public async Task<bool> DeleteAsync(int id, int recipientUserId)
        {
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == id && n.RecipientUserId == recipientUserId);

            if (notification == null)
            {
                return false;
            }

            _context.Notifications.Remove(notification);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<int> DeleteAllAsync(int recipientUserId, string? type = null)
        {
            var normalizedType = NotificationTypes.Normalize(type);
            var query = _context.Notifications
                .Where(n => n.RecipientUserId == recipientUserId);

            if (normalizedType != NotificationTypes.All)
            {
                query = query.Where(n => n.Type == normalizedType);
            }

            var notifications = await query.ToListAsync();
            _context.Notifications.RemoveRange(notifications);
            await _context.SaveChangesAsync();
            return notifications.Count;
        }

        private static NotificationDTO ToDTO(Notification notification)
        {
            return new NotificationDTO
            {
                Id = notification.Id,
                RecipientUserId = notification.RecipientUserId,
                SenderUserId = notification.SenderUserId,
                Type = notification.Type,
                Title = notification.Title,
                Message = notification.Message,
                IsRead = notification.IsRead,
                CreatedAt = notification.CreatedAt,
                RelatedEntityId = notification.RelatedEntityId
            };
        }
    }
}
